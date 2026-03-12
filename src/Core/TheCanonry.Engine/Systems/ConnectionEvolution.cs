namespace TheCanonry.Engine.Systems;

using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Rules;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Engine.Runtime;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.Primitives;
using TheCanonry.Schema.World;

/// <summary>
/// Handles relationship strength changes over time based on shared context.
///
/// This system evaluates metrics for selected entities and applies evolution rules:
/// - Adjust prominence based on connection counts
/// - Create relationships between entities that share enough connections
/// - Change status based on graph topology
///
/// The system is configured via a dictionary with:
/// - selection: How to find entities to evaluate
/// - metric: How to calculate the metric per entity
/// - rules: Conditions and actions triggered by metric values
/// - subtypeBonuses: Bonuses added to metric for specific subtypes
/// - throttleChance: Probability of running each tick (0-1)
/// </summary>
public sealed class ConnectionEvolution : ISimulationSystem
{
    private readonly IReadOnlyDictionary<string, object?> _config;

    public ConnectionEvolution(
        string id, string name,
        IReadOnlyDictionary<string, object?> config,
        WorldRuntime runtime)
    {
        Id = id;
        Name = name;
        _config = config;
        Runtime = runtime;
    }

    public string Id { get; }
    public string Name { get; }

    /// <summary>The runtime this system operates on.</summary>
    internal WorldRuntime Runtime { get; }

    /// <summary>The raw configuration dictionary for this system.</summary>
    internal IReadOnlyDictionary<string, object?> Config => _config;

    public void Initialize() { }

    public Task<SystemResult> ApplyAsync(double modifier)
    {
        // Throttle check
        var throttleChance = GetDouble("throttleChance", 1.0);
        if (throttleChance < 1.0 && Runtime.NextRandomDouble() > throttleChance)
        {
            return Task.FromResult(SystemResult.Empty with
            {
                Description = $"{Name}: throttled"
            });
        }

        var actionCtx = new ActionContext("system", Id, true);

        // Select entities to evaluate
        var entities = SelectEntitiesFromConfig();
        if (entities.Count == 0)
        {
            return Task.FromResult(SystemResult.Empty with
            {
                Description = $"{Name}: no entities selected"
            });
        }

        // Create rule context for metric evaluation
        var graphAdapter = new RuntimeGraphAdapter(Runtime);
        var ruleCtx = RuleContext.CreateForSystem(graphAdapter,
            Runtime.CurrentEra?.Id.Value ?? "");

        // Track accumulators
        var modifications = new List<EntityModified>();
        var relationshipsAdded = new List<RelationshipAdded>();
        var relationshipsAdjusted = new List<RelationshipAdjusted>();
        var pressureChanges = new Dictionary<string, double>();
        var matchingByRule = new Dictionary<int, List<Entity>>();

        // Evaluate metric for each entity and check rules
        var rules = GetRules();
        var metric = GetMetric();

        foreach (var entity in entities)
        {
            var entityCtx = ruleCtx.WithSelf(entity);
            var metricValue = metric is not null
                ? MetricEvaluator.Evaluate(metric, entityCtx, entity)
                : 0.0;

            // Apply subtype bonuses
            metricValue = ApplySubtypeBonus(metricValue, entity);

            // Evaluate each rule
            for (var ruleIdx = 0; ruleIdx < rules.Count; ruleIdx++)
            {
                var rule = rules[ruleIdx];
                var threshold = ResolveThreshold(rule.Threshold, entity, rule.ThresholdMultiplier);

                if (!ApplyOperator(metricValue, rule.Operator, threshold))
                    continue;

                // Probability check
                if (rule.Probability < 1.0 && Runtime.NextRandomDouble() > rule.Probability * modifier)
                    continue;

                if (rule.BetweenMatching && rule.Action is CreateRelationshipMutation)
                {
                    if (!matchingByRule.ContainsKey(ruleIdx))
                        matchingByRule[ruleIdx] = [];
                    matchingByRule[ruleIdx].Add(entity);
                }
                else if (rule.Action is not null)
                {
                    // Apply individual action
                    var mutCtx = ruleCtx.WithSelf(entity);
                    if (MutationApplier.Apply(rule.Action, mutCtx))
                    {
                        modifications.Add(new EntityModified(
                            entity.Id,
                            new Dictionary<string, object?> { ["modified_by"] = Id },
                            actionCtx,
                            entity.Id.Value));
                    }
                }
            }
        }

        // Process between-matching pairs
        ProcessBetweenMatchingRules(matchingByRule, rules, ruleCtx, actionCtx,
            relationshipsAdded, modifications);

        // Add config-level pressure changes if any modifications happened
        if (modifications.Count > 0 || relationshipsAdded.Count > 0)
        {
            var configPressures = GetPressureChanges();
            foreach (var (pressureId, delta) in configPressures)
            {
                if (pressureChanges.TryGetValue(pressureId, out var current))
                    pressureChanges[pressureId] = current + delta;
                else
                    pressureChanges[pressureId] = delta;
            }
        }

        return Task.FromResult(new SystemResult(
            RelationshipsAdded: relationshipsAdded,
            RelationshipsAdjusted: relationshipsAdjusted,
            RelationshipsToArchive: [],
            EntitiesModified: modifications,
            PressureChanges: pressureChanges,
            Description: $"{Name}: {modifications.Count} modified, {relationshipsAdded.Count} relationships",
            Details: new Dictionary<string, object?>(),
            NarrationsByGroup: new Dictionary<string, string>()));
    }

    // =========================================================================
    // ENTITY SELECTION
    // =========================================================================

    private IReadOnlyList<Entity> SelectEntitiesFromConfig()
    {
        // Select entities by kind from config
        var kind = GetString("selection.kind", "");
        if (string.IsNullOrEmpty(kind))
            return Runtime.GetEntities();

        return Runtime.FindEntities(EntityCriteria.Create(
            kind: new EntityKind(kind)));
    }

    // =========================================================================
    // RULE EVALUATION HELPERS
    // =========================================================================

    private sealed record EvolutionRuleData(
        string Operator,
        double Threshold,
        string ThresholdType,
        double ThresholdMultiplier,
        double Probability,
        bool BetweenMatching,
        Mutation? Action);

    private List<EvolutionRuleData> GetRules()
    {
        var rules = new List<EvolutionRuleData>();
        if (!_config.TryGetValue("rules", out var rulesObj) || rulesObj is not IReadOnlyList<object?> rulesList)
            return rules;

        foreach (var ruleObj in rulesList)
        {
            if (ruleObj is not IReadOnlyDictionary<string, object?> ruleDict)
                continue;

            rules.Add(new EvolutionRuleData(
                Operator: GetStringFrom(ruleDict, "condition.operator", ">="),
                Threshold: GetDoubleFrom(ruleDict, "condition.threshold", 0.0),
                ThresholdType: GetStringFrom(ruleDict, "condition.thresholdType", "number"),
                ThresholdMultiplier: GetDoubleFrom(ruleDict, "condition.multiplier", 6.0),
                Probability: GetDoubleFrom(ruleDict, "probability", 1.0),
                BetweenMatching: GetBoolFrom(ruleDict, "betweenMatching", false),
                Action: ExtractAction(ruleDict)));
        }

        return rules;
    }

    private Metric? GetMetric()
    {
        // Simplified: return a ConnectionCountMetric based on config
        if (!_config.TryGetValue("metric", out var metricObj) || metricObj is not IReadOnlyDictionary<string, object?> metricDict)
            return null;

        var type = GetStringFrom(metricDict, "type", "");
        return type switch
        {
            "connection_count" => new ConnectionCountMetric(
                RelationshipKinds: GetStringListFrom(metricDict, "relationshipKinds"),
                Direction: Direction.Both,
                MinStrength: GetDoubleFrom(metricDict, "minStrength", 0.0),
                Coefficient: GetDoubleFrom(metricDict, "coefficient", 1.0),
                Cap: GetDoubleFrom(metricDict, "cap", 100.0)),
            "shared_relationship" => new SharedRelationshipMetric(
                SharedRelationshipKinds: GetStringListFrom(metricDict, "sharedRelationshipKinds"),
                SharedDirection: Direction.Both,
                Via: null,
                MinStrength: GetDoubleFrom(metricDict, "minStrength", 0.0),
                Coefficient: GetDoubleFrom(metricDict, "coefficient", 1.0),
                Cap: GetDoubleFrom(metricDict, "cap", 100.0)),
            _ => new ConstantMetric(0.0, 1.0)
        };
    }

    private double ApplySubtypeBonus(double metricValue, Entity entity)
    {
        if (!_config.TryGetValue("subtypeBonuses", out var bonusesObj) ||
            bonusesObj is not IReadOnlyList<object?> bonusesList)
            return metricValue;

        foreach (var bonusObj in bonusesList)
        {
            if (bonusObj is not IReadOnlyDictionary<string, object?> bonusDict)
                continue;

            var subtype = GetStringFrom(bonusDict, "subtype", "");
            if (subtype == entity.Subtype)
            {
                metricValue += GetDoubleFrom(bonusDict, "bonus", 0.0);
                break;
            }
        }

        return metricValue;
    }

    private static double ResolveThreshold(double threshold, Entity entity, double multiplier)
    {
        return threshold;
    }

    private static bool ApplyOperator(double value, string op, double threshold)
    {
        return op switch
        {
            ">=" => value >= threshold,
            "<=" => value <= threshold,
            ">" => value > threshold,
            "<" => value < threshold,
            "==" => Math.Abs(value - threshold) < 1e-9,
            "!=" => Math.Abs(value - threshold) >= 1e-9,
            _ => value >= threshold
        };
    }

    private void ProcessBetweenMatchingRules(
        Dictionary<int, List<Entity>> matchingByRule,
        List<EvolutionRuleData> rules,
        RuleContext ruleCtx,
        ActionContext actionCtx,
        List<RelationshipAdded> relationshipsAdded,
        List<EntityModified> modifications)
    {
        var pairExcludeKinds = GetStringList("pairExcludeRelationships");

        foreach (var (ruleIdx, matchingEntities) in matchingByRule)
        {
            var rule = rules[ruleIdx];
            if (rule.Action is not CreateRelationshipMutation createRel)
                continue;

            var relKind = new RelationshipKind(createRel.Kind);

            for (var i = 0; i < matchingEntities.Count; i++)
            {
                for (var j = i + 1; j < matchingEntities.Count; j++)
                {
                    var src = matchingEntities[i];
                    var dst = matchingEntities[j];

                    // Skip if relationship already exists
                    if (Runtime.HasRelationship(src.Id, dst.Id, relKind))
                        continue;

                    // Skip excluded pairs
                    if (IsPairExcluded(src.Id, dst.Id, pairExcludeKinds))
                        continue;

                    relationshipsAdded.Add(new RelationshipAdded(
                        Kind: createRel.Kind,
                        Src: src.Id.Value,
                        Dst: dst.Id.Value,
                        Strength: createRel.Strength,
                        Category: createRel.Category,
                        CatalyzedBy: "",
                        CreatedAt: Runtime.CurrentTick,
                        ActionContext: actionCtx,
                        NarrativeGroupId: src.Id.Value));
                }
            }
        }
    }

    private bool IsPairExcluded(EntityId srcId, EntityId dstId, IReadOnlyList<string> excludeKinds)
    {
        foreach (var excludeKind in excludeKinds)
        {
            var kind = new RelationshipKind(excludeKind);
            if (Runtime.HasRelationship(srcId, dstId, kind) ||
                Runtime.HasRelationship(dstId, srcId, kind))
                return true;
        }
        return false;
    }

    // =========================================================================
    // CONFIG HELPERS
    // =========================================================================

    private double GetDouble(string key, double defaultValue)
    {
        return _config.TryGetValue(key, out var val) && val is double d ? d : defaultValue;
    }

    private string GetString(string key, string defaultValue)
    {
        return _config.TryGetValue(key, out var val) && val is string s ? s : defaultValue;
    }

    private IReadOnlyList<string> GetStringList(string key)
    {
        if (_config.TryGetValue(key, out var val) && val is IReadOnlyList<object?> list)
            return list.Where(x => x is string).Cast<string>().ToList();
        return [];
    }

    private Dictionary<string, double> GetPressureChanges()
    {
        var result = new Dictionary<string, double>();
        if (_config.TryGetValue("pressureChanges", out var val) && val is IReadOnlyDictionary<string, object?> dict)
        {
            foreach (var (key, v) in dict)
            {
                if (v is double d)
                    result[key] = d;
            }
        }
        return result;
    }

    private static string GetStringFrom(IReadOnlyDictionary<string, object?> dict, string key, string defaultValue)
    {
        return dict.TryGetValue(key, out var val) && val is string s ? s : defaultValue;
    }

    private static double GetDoubleFrom(IReadOnlyDictionary<string, object?> dict, string key, double defaultValue)
    {
        return dict.TryGetValue(key, out var val) && val is double d ? d : defaultValue;
    }

    private static bool GetBoolFrom(IReadOnlyDictionary<string, object?> dict, string key, bool defaultValue)
    {
        return dict.TryGetValue(key, out var val) && val is bool b ? b : defaultValue;
    }

    private static IReadOnlyList<string> GetStringListFrom(IReadOnlyDictionary<string, object?> dict, string key)
    {
        if (dict.TryGetValue(key, out var val) && val is IReadOnlyList<object?> list)
            return list.Where(x => x is string).Cast<string>().ToList();
        return [];
    }

    private static Mutation? ExtractAction(IReadOnlyDictionary<string, object?> ruleDict)
    {
        if (!ruleDict.TryGetValue("action", out var actionObj) ||
            actionObj is not IReadOnlyDictionary<string, object?> actionDict)
            return null;

        var type = GetStringFrom(actionDict, "type", "");
        return type switch
        {
            "adjust_prominence" => new AdjustProminenceMutation(
                Entity: "$self",
                Delta: GetDoubleFrom(actionDict, "delta", 0.0)),
            "create_relationship" => new CreateRelationshipMutation(
                Src: "$member",
                Dst: "$member2",
                Kind: GetStringFrom(actionDict, "kind", ""),
                Strength: GetDoubleFrom(actionDict, "strength", 0.5),
                Bidirectional: GetBoolFrom(actionDict, "bidirectional", false),
                Category: GetStringFrom(actionDict, "category", "")),
            "change_status" => new ChangeStatusMutation(
                Entity: "$self",
                NewStatus: GetStringFrom(actionDict, "newStatus", "")),
            "set_tag" => new SetTagMutation(
                Entity: "$self",
                Tag: GetStringFrom(actionDict, "tag", ""),
                Value: GetStringFrom(actionDict, "value", ""),
                ValueFrom: ""),
            _ => null
        };
    }
}

/// <summary>
/// Adapter that wraps WorldRuntime to satisfy IGraph interface for RuleContext.
/// Shared by ConnectionEvolution and other systems that need RuleContext evaluation.
/// </summary>
file sealed class RuntimeGraphAdapter : IGraph
{
    private readonly WorldRuntime _runtime;

    public RuntimeGraphAdapter(WorldRuntime runtime)
    {
        _runtime = runtime;
    }

    public int Tick
    {
        get => _runtime.CurrentTick;
        set => _runtime.CurrentTick = value;
    }

    public Dictionary<string, double> Pressures
    {
        get
        {
            var dict = new Dictionary<string, double>();
            foreach (var kvp in _runtime.GetAllPressures())
                dict[kvp.Key] = kvp.Value;
            return dict;
        }
    }

    public Entity? GetEntity(EntityId id) => _runtime.GetEntity(id);
    public bool HasEntity(EntityId id) => _runtime.HasEntity(id);
    public int GetEntityCount(bool includeHistorical = false) => _runtime.GetEntityCount(includeHistorical);
    public IReadOnlyList<Entity> GetEntities(bool includeHistorical = false) => _runtime.GetEntities(includeHistorical);
    public IReadOnlyList<Entity> FindEntities(EntityCriteria criteria) => _runtime.FindEntities(criteria);
    public IReadOnlyList<Entity> GetEntitiesByKind(EntityKind kind, bool includeHistorical = false) => _runtime.GetEntitiesByKind(kind, includeHistorical);
    public IReadOnlyList<Entity> GetConnectedEntities(EntityId entityId, RelationshipKind? relationKind = null, Direction direction = Direction.Both, bool includeHistorical = false) => _runtime.GetConnectedEntities(entityId, relationKind, direction, includeHistorical);
    public EntityId CreateEntity(Entity entity) => _runtime.CreateEntity(entity);
    public bool UpdateEntity(EntityId id, Action<Entity> mutator) => _runtime.UpdateEntity(id, mutator);
    public bool DeleteEntity(EntityId id) => _runtime.DeleteEntity(id);
    public IReadOnlyList<Relationship> GetRelationships(bool includeHistorical = false) => _runtime.GetRelationships(includeHistorical);
    public IReadOnlyList<Relationship> FindRelationships(RelationshipCriteria criteria) => _runtime.FindRelationships(criteria);
    public IReadOnlyList<Relationship> GetEntityRelationships(EntityId entityId, Direction direction = Direction.Both, bool includeHistorical = false) => _runtime.GetEntityRelationships(entityId, direction, includeHistorical);
    public bool HasRelationship(EntityId srcId, EntityId dstId, RelationshipKind? kind = null) => _runtime.HasRelationship(srcId, dstId, kind);
    public bool AddRelationship(Relationship relationship) => _runtime.AddRelationship(relationship);
    public bool RemoveRelationship(EntityId srcId, EntityId dstId, RelationshipKind kind) => _runtime.RemoveRelationship(srcId, dstId, kind);
}
