namespace TheCanonry.Engine.Systems;

using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Runtime;
using TheCanonry.Engine.Selection;
using TheCanonry.Engine.Statistics;
using TheCanonry.Engine.Templates;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.World;

// =============================================================================
// GROWTH EPOCH SUMMARY
// =============================================================================

/// <summary>
/// Summary of a completed growth epoch, capturing targets vs actuals.
/// </summary>
public sealed record GrowthEpochSummary(
    int Epoch,
    int Target,
    int EntitiesCreated,
    int TemplatesApplied,
    IReadOnlyList<string> TemplatesUsed);

// =============================================================================
// GROWTH SYSTEM
// =============================================================================

/// <summary>
/// Growth system manages entity creation via template expansion during growth phases.
///
/// This is a special system managed directly by the WorldEngine/EpochRunner,
/// not by the normal simulation tick loop. It does NOT implement ISimulationSystem.
///
/// Responsibilities:
/// - Per-epoch budgeting: calculate how many entities to create based on
///   distribution targets, scale factor, and current population
/// - Template sampling: select templates weighted by era template weights
///   + DynamicWeightCalculator adjustments
/// - Template application: for each sampled template, select a target entity
///   via TargetSelector, then execute via TemplateInterpreter
/// - Yield tracking: track how many entities each template produces vs. budget
/// - Budget enforcement: stop growth when budget is met or templates are exhausted
/// </summary>
public sealed class GrowthSystem
{
    private readonly EngineConfig _config;
    private readonly WorldRuntime _runtime;
    private readonly DynamicWeightCalculator _weightCalculator;
    private readonly int _maxTemplatesPerTick;
    private readonly int _minTemplatesPerTick;
    private readonly int _yieldWindow;
    private readonly int _maxAttemptsPerTick;

    private int _epoch = -1;
    private int _epochTarget;
    private int _entitiesCreated;
    private int _templatesApplied;
    private readonly HashSet<string> _templatesUsed = [];
    private readonly List<int> _yieldSamples = [];
    private readonly Dictionary<string, int> _templateRunCounts = [];
    private Era? _epochEra;

    /// <summary>
    /// Maximum runs allowed per template within an epoch.
    /// Prevents a single template from monopolizing the budget.
    /// </summary>
    public int MaxRunsPerTemplate { get; init; } = 50;

    public GrowthSystem(
        EngineConfig config,
        WorldRuntime runtime,
        DynamicWeightCalculator weightCalculator,
        int maxTemplatesPerTick = 5,
        int minTemplatesPerTick = 1,
        int yieldAveragingWindow = 30,
        int maxAttemptsPerTick = 40)
    {
        _config = config;
        _runtime = runtime;
        _weightCalculator = weightCalculator;
        _maxTemplatesPerTick = maxTemplatesPerTick;
        _minTemplatesPerTick = minTemplatesPerTick;
        _yieldWindow = yieldAveragingWindow;
        _maxAttemptsPerTick = maxAttemptsPerTick;
    }

    // =========================================================================
    // EPOCH LIFECYCLE
    // =========================================================================

    /// <summary>
    /// Initialize budget for a new epoch and reset yield counters.
    /// </summary>
    public void StartEpoch(Era era)
    {
        _epoch = _runtime.CurrentEpoch;
        _epochEra = era;
        _epochTarget = CalculateGrowthTarget();
        _entitiesCreated = 0;
        _templatesApplied = 0;
        _templatesUsed.Clear();
        _templateRunCounts.Clear();

        _runtime.Log(
            LogLevel.Info,
            $"[Growth] Planning epoch {_epoch} in era {era.Name} with target {_epochTarget}");
    }

    /// <summary>
    /// Record epoch statistics and return summary.
    /// </summary>
    public GrowthEpochSummary CompleteEpoch()
    {
        return new GrowthEpochSummary(
            Epoch: _epoch,
            Target: _epochTarget,
            EntitiesCreated: _entitiesCreated,
            TemplatesApplied: _templatesApplied,
            TemplatesUsed: _templatesUsed.ToList());
    }

    /// <summary>
    /// Return the current epoch's entity creation target.
    /// </summary>
    public int GetEpochTarget() => _epochTarget;

    /// <summary>
    /// Return per-template yield stats for the current epoch.
    /// Maps template ID to number of entities produced.
    /// </summary>
    public IReadOnlyDictionary<string, int> GetEpochYield()
    {
        return new Dictionary<string, int>(_templateRunCounts);
    }

    // =========================================================================
    // GROWTH TICK
    // =========================================================================

    /// <summary>
    /// Run one growth tick: sample templates, find targets, expand, add results to graph.
    /// </summary>
    /// <param name="modifier">Era-specific growth modifier (0 = disabled, 1 = normal).</param>
    /// <returns>System result describing what happened.</returns>
    public SystemResult Apply(double modifier)
    {
        if (modifier <= 0)
            return CreateResult("Growth disabled for this era");

        var remaining = Math.Max(0, _epochTarget - _entitiesCreated);
        if (remaining == 0)
            return CreateResult($"Growth target met for epoch {_epoch}");

        if (_epochEra is null)
            return CreateResult("No era set for growth");

        _runtime.UpdatePopulationMetrics();
        var metrics = _runtime.GetPopulationMetrics();
        var budget = ComputeBudget(remaining, modifier);

        var applied = 0;
        var created = 0;
        var attempts = 0;

        while (applied < budget && attempts < _maxAttemptsPerTick)
        {
            attempts++;
            var result = AttemptOneTemplate(_epochEra, metrics);

            if (result.Exhausted)
                break;

            applied += result.AppliedDelta;
            created += result.CreatedDelta;

            if (_entitiesCreated >= _epochTarget)
                break;
        }

        return CreateResult(created > 0
            ? $"Growth tick: +{created} entities (epoch {_entitiesCreated}/{_epochTarget})"
            : "Growth tick: no entities created");
    }

    // =========================================================================
    // BUDGET CALCULATION
    // =========================================================================

    /// <summary>
    /// Calculate total growth target for the epoch based on distribution targets and scale factor.
    /// Sums all subtype targets and scales by the engine's scale factor.
    /// </summary>
    public int CalculateGrowthTarget()
    {
        var totalTarget = 0;
        foreach (var kindEntry in _config.DistributionTargets.Entities)
        {
            foreach (var subtypeEntry in kindEntry.Value)
            {
                totalTarget += subtypeEntry.Value;
            }
        }

        return (int)Math.Ceiling(totalTarget * _config.ScaleFactor);
    }

    /// <summary>
    /// Compute how many templates to try this tick based on remaining budget,
    /// ticks left in epoch, average yield, and modifier.
    /// </summary>
    private int ComputeBudget(int remaining, double modifier)
    {
        var tickInEpoch = _runtime.CurrentTick % _config.TicksPerEpoch;
        var ticksLeft = Math.Max(1, _config.TicksPerEpoch - tickInEpoch);

        var avgYield = _yieldSamples.Count == 0
            ? 1.0
            : Math.Max(1.0, _yieldSamples.Sum() / (double)_yieldSamples.Count);

        var budget = (int)Math.Ceiling(
            Math.Ceiling(remaining / (double)ticksLeft / avgYield) * modifier);

        return Math.Max(
            remaining > 0 ? _minTemplatesPerTick : 0,
            Math.Min(_maxTemplatesPerTick, budget));
    }

    // =========================================================================
    // TEMPLATE SELECTION AND APPLICATION
    // =========================================================================

    private (int AppliedDelta, int CreatedDelta, bool Exhausted) AttemptOneTemplate(
        Era era, PopulationMetrics metrics)
    {
        var applicable = FilterApplicableTemplates();
        if (applicable.Count == 0)
        {
            _runtime.Log(LogLevel.Warn,
                $"No applicable templates remaining ({_entitiesCreated}/{_epochTarget})");
            return (0, 0, true);
        }

        var template = SampleTemplate(era, applicable, metrics);
        if (template is null)
            return (0, 0, false);

        var count = ApplyTemplate(template);
        if (count > 0)
        {
            RecordYield(template, count);
            return (1, count, _entitiesCreated >= _epochTarget);
        }

        return (0, 0, false);
    }

    /// <summary>
    /// Filter templates to those that are applicable in the current state:
    /// not over their run cap and whose applicability conditions pass.
    /// </summary>
    private List<DeclarativeTemplate> FilterApplicableTemplates()
    {
        var applicable = new List<DeclarativeTemplate>();
        foreach (var template in _config.Templates)
        {
            if (!template.Enabled)
                continue;

            var runs = _templateRunCounts.GetValueOrDefault(template.Id);
            if (runs >= MaxRunsPerTemplate)
                continue;

            // Check era weight: if the template has zero weight in this era, skip it
            if (_epochEra is not null &&
                _epochEra.TemplateWeights.TryGetValue(template.Id, out var weight) &&
                weight == 0)
                continue;

            applicable.Add(template);
        }
        return applicable;
    }

    /// <summary>
    /// Sample a template from the applicable list, weighted by era template weights
    /// adjusted by DynamicWeightCalculator.
    /// </summary>
    public DeclarativeTemplate? SampleTemplate(
        Era era, IReadOnlyList<DeclarativeTemplate> applicable, PopulationMetrics metrics)
    {
        if (applicable.Count == 0)
            return null;

        // Build weighted list using era weights + dynamic adjustment
        var weights = new double[applicable.Count];
        var totalWeight = 0.0;

        for (var i = 0; i < applicable.Count; i++)
        {
            var template = applicable[i];
            var baseWeight = era.TemplateWeights.TryGetValue(template.Id, out var w) ? w : 1.0;

            // Get creation kinds for dynamic weight adjustment
            var createdKinds = GetCreationKinds(template);
            var adjustment = _weightCalculator.CalculateWeight(
                template.Id, baseWeight, metrics, createdKinds);

            weights[i] = adjustment.AdjustedWeight;
            totalWeight += adjustment.AdjustedWeight;
        }

        if (totalWeight <= 0)
            return null;

        // Weighted random selection
        var roll = _runtime.NextRandomDouble() * totalWeight;
        var cumulative = 0.0;

        for (var i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative)
                return applicable[i];
        }

        // Fallback: return last template
        return applicable[^1];
    }

    /// <summary>
    /// Extract (kind, subtype) pairs from a template's creation rules.
    /// Used by DynamicWeightCalculator for population-based weight adjustment.
    /// </summary>
    private static IReadOnlyList<(string Kind, string Subtype)> GetCreationKinds(
        DeclarativeTemplate template)
    {
        var kinds = new List<(string Kind, string Subtype)>();
        foreach (var rule in template.Creation)
        {
            var subtype = rule.Subtype switch
            {
                FixedSubtype f => f.Value,
                _ => ""
            };
            kinds.Add((rule.Kind, subtype));
        }
        return kinds;
    }

    /// <summary>
    /// Apply a single template: find a target entity, execute template, add results to graph.
    /// </summary>
    private int ApplyTemplate(DeclarativeTemplate template)
    {
        // Find target entities using the template's selection rule
        var targets = FindTargets(template);
        if (targets.Count == 0)
        {
            _runtime.Log(LogLevel.Debug, $"{template.Id} found no targets");
            return 0;
        }

        // Pick a random target
        var target = targets[_runtime.NextRandomInt(targets.Count)];

        // Execute template
        var result = TemplateInterpreter.Execute(template, target, _runtime);
        if (result.Entities.Count == 0)
            return 0;

        // Add created entities to graph
        foreach (var entity in result.Entities)
        {
            _runtime.CreateEntity(entity);
        }

        // Add created relationships to graph
        foreach (var relationship in result.Relationships)
        {
            _runtime.AddRelationship(relationship);
        }

        return result.Entities.Count;
    }

    /// <summary>
    /// Find target entities for a template based on its selection rule.
    /// </summary>
    private IReadOnlyList<Entity> FindTargets(DeclarativeTemplate template)
    {
        var selection = template.Selection;

        if (string.IsNullOrEmpty(selection.Kind))
            return _runtime.GetEntities();

        return _runtime.GetEntitiesByKind(new EntityKind(selection.Kind));
    }

    // =========================================================================
    // YIELD TRACKING
    // =========================================================================

    /// <summary>
    /// Record entities created by a template for budget tracking.
    /// </summary>
    private void RecordYield(DeclarativeTemplate template, int count)
    {
        _entitiesCreated += count;
        _templatesApplied++;
        _templatesUsed.Add(template.Id);
        _templateRunCounts[template.Id] = _templateRunCounts.GetValueOrDefault(template.Id) + 1;

        _yieldSamples.Add(count);
        if (_yieldSamples.Count > _yieldWindow)
            _yieldSamples.RemoveAt(0);
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private static SystemResult CreateResult(string description)
    {
        return new SystemResult(
            RelationshipsAdded: [],
            RelationshipsAdjusted: [],
            RelationshipsToArchive: [],
            EntitiesModified: [],
            PressureChanges: new Dictionary<string, double>(),
            Description: description,
            Details: new Dictionary<string, object?>(),
            NarrationsByGroup: new Dictionary<string, string>());
    }
}
