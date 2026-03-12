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
/// Handles transitions between eras based on exit/entry conditions defined per-era.
///
/// Transition model:
/// - ExitConditions: Criteria for current era to END (all must be met)
/// - EntryConditions: Criteria for next era to START (all must be met)
/// - NextEra: Optional explicit next era ID (supports divergent paths)
/// - ExitEffects: Applied when transitioning OUT of current era
/// - EntryEffects: Applied when transitioning INTO next era
///
/// Lazy spawning:
/// - Era entities are created on-demand when transitioned into
/// - Only the first era is spawned at init (by EraSpawner)
/// </summary>
public sealed class EraTransition : ISimulationSystem
{
    private readonly EraTransitionConfig _config;

    public EraTransition(EraTransitionConfig config, WorldRuntime runtime)
    {
        _config = config;
        Runtime = runtime;
    }

    public string Id => _config.Id;
    public string Name => _config.Name;

    /// <summary>The runtime this system operates on.</summary>
    internal WorldRuntime Runtime { get; }

    /// <summary>The typed configuration for this system.</summary>
    internal EraTransitionConfig Config => _config;

    public void Initialize() { }

    public Task<SystemResult> ApplyAsync(double modifier)
    {
        // Find the current era entity
        var currentEraEntity = FindCurrentEraEntity();
        if (currentEraEntity is null)
            return Task.FromResult(EmptyResult("No current era"));

        // Find the matching era config
        var currentConfig = FindEraConfig(currentEraEntity.Subtype);
        if (currentConfig is null)
        {
            Runtime.Log(LogLevel.Warn,
                $"[EraTransition] No config for \"{currentEraEntity.Name}\"");
            return Task.FromResult(EmptyResult($"{currentEraEntity.Name} persists (config not found)"));
        }

        // Check exit conditions
        var exitConditionsMet = EvaluateConditions(currentConfig.ExitConditions, currentEraEntity);
        if (!exitConditionsMet)
            return Task.FromResult(EmptyResult($"{currentEraEntity.Name} persists"));

        // Find next era
        var nextConfig = FindNextEra(currentConfig);
        if (nextConfig is null)
            return Task.FromResult(EmptyResult($"{currentEraEntity.Name} endures (final era)"));

        // Execute transition
        return Task.FromResult(ExecuteTransition(currentEraEntity, currentConfig, nextConfig));
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

    private Entity? FindCurrentEraEntity()
    {
        var eras = Runtime.FindEntities(EntityCriteria.Create(
            kind: FrameworkPrimitives.EntityKinds.Era,
            status: FrameworkPrimitives.Statuses.Current));
        return eras.Count > 0 ? eras[0] : null;
    }

    private Era? FindEraConfig(string eraSubtype)
    {
        foreach (var era in Runtime.Config.Eras)
        {
            if (era.Id.Value == eraSubtype)
                return era;
        }
        return null;
    }

    /// <summary>
    /// Evaluate all conditions. ALL must pass. Empty conditions = immediate pass.
    /// </summary>
    private bool EvaluateConditions(IReadOnlyList<Condition> conditions, Entity eraEntity)
    {
        if (conditions.Count == 0)
            return true;

        var currentEraId = Runtime.CurrentEra?.Id.Value ?? "";
        var ruleCtx = RuleContext.CreateForSystem(
            Runtime.FindEntities(EntityCriteria.Create()).Count > 0
                ? CreateGraphAccessor()
                : CreateGraphAccessor(),
            currentEraId);
        ruleCtx = ruleCtx.WithSelf(eraEntity);

        foreach (var condition in conditions)
        {
            if (!ConditionEvaluator.Evaluate(condition, ruleCtx, eraEntity))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Find the next era to transition to.
    /// 1. If currentEra.NextEra is set, try that specific era
    /// 2. Otherwise, find first candidate era whose entry conditions are met
    /// 3. Skip eras that already have entities in the graph
    /// </summary>
    private Era? FindNextEra(Era currentConfig)
    {
        var allEras = Runtime.Config.Eras;
        var existingEraIds = GetExistingEraIds();

        // If explicit nextEra is set, try that first
        if (currentConfig.NextEra is not null)
        {
            var explicitNext = FindEraConfig(currentConfig.NextEra.Value.Value);
            if (explicitNext is not null && !existingEraIds.Contains(explicitNext.Id.Value))
            {
                // For entry conditions of next era, we need to check them
                // but we use the current era entity as context
                var currentEntity = FindCurrentEraEntity();
                if (currentEntity is not null && EvaluateConditions(explicitNext.EntryConditions, currentEntity))
                    return explicitNext;
            }
        }

        // Search for first candidate era whose entry conditions are met
        var currentEraEntity = FindCurrentEraEntity();
        foreach (var candidateEra in allEras)
        {
            if (existingEraIds.Contains(candidateEra.Id.Value))
                continue;

            // Empty entry conditions = always allow
            if (currentEraEntity is not null && EvaluateConditions(candidateEra.EntryConditions, currentEraEntity))
                return candidateEra;
        }

        return null;
    }

    private HashSet<string> GetExistingEraIds()
    {
        var existingEras = Runtime.FindEntities(EntityCriteria.Create(
            kind: FrameworkPrimitives.EntityKinds.Era,
            includeHistorical: true));
        var ids = new HashSet<string>();
        foreach (var e in existingEras)
            ids.Add(e.Subtype);
        return ids;
    }

    private SystemResult ExecuteTransition(Entity currentEraEntity, Era currentConfig, Era nextConfig)
    {
        var actionCtx = new ActionContext("framework", Id, true);
        var narrativeGroupId = $"era_transition:{currentConfig.Id.Value}:{nextConfig.Id.Value}";

        // Spawn next era entity (lazy spawning)
        var nextEraEntity = EraSpawner.CreateEraEntity(
            nextConfig, Runtime.CurrentTick, FrameworkPrimitives.Statuses.Current);
        Runtime.CreateEntity(nextEraEntity);

        // End current era
        Runtime.UpdateEntity(currentEraEntity.Id, e =>
        {
            e.UpdateStatus(FrameworkPrimitives.Statuses.Historical, Runtime.CurrentTick);
            e.Temporal.EndAt(Runtime.CurrentTick);
        });
        Runtime.CurrentEra = nextConfig;

        Runtime.Log(LogLevel.Info,
            $"[EraTransition] TRANSITIONING: {currentEraEntity.Name} -> {nextEraEntity.Name}");

        // Collect pressure effects
        var pressureChanges = CollectPressureChanges(currentConfig, nextConfig);

        // Build relationships
        var relationshipsAdded = BuildTransitionRelationships(
            currentEraEntity, nextEraEntity, actionCtx, narrativeGroupId);

        // Prominence snapshot
        var (modifications, lockedCount) = SnapshotProminence(currentConfig, actionCtx, narrativeGroupId);

        var entitiesModified = new List<EntityModified>
        {
            new(currentEraEntity.Id,
                new Dictionary<string, object?> { ["status"] = "historical" },
                actionCtx, narrativeGroupId)
        };
        entitiesModified.AddRange(modifications);

        return new SystemResult(
            RelationshipsAdded: relationshipsAdded,
            RelationshipsAdjusted: [],
            RelationshipsToArchive: [],
            EntitiesModified: entitiesModified,
            PressureChanges: pressureChanges,
            Description: $"Era transition: {currentEraEntity.Name} -> {nextEraEntity.Name} ({relationshipsAdded.Count - 1} entities linked, {lockedCount} prominence locked)",
            Details: new Dictionary<string, object?>
            {
                ["eraTransition"] = new Dictionary<string, object?>
                {
                    ["fromEra"] = currentEraEntity.Name,
                    ["fromEraId"] = currentConfig.Id.Value,
                    ["toEra"] = nextEraEntity.Name,
                    ["toEraId"] = nextConfig.Id.Value,
                    ["pressureEffects"] = pressureChanges
                }
            },
            NarrationsByGroup: new Dictionary<string, string>());
    }

    private Dictionary<string, double> CollectPressureChanges(Era currentConfig, Era nextConfig)
    {
        var changes = new Dictionary<string, double>();

        // Exit effects from current era
        foreach (var mutation in currentConfig.ExitEffects.Mutations)
        {
            if (changes.TryGetValue(mutation.PressureId, out var current))
                changes[mutation.PressureId] = current + mutation.Delta;
            else
                changes[mutation.PressureId] = mutation.Delta;
        }

        // Entry effects for next era
        foreach (var mutation in nextConfig.EntryEffects.Mutations)
        {
            if (changes.TryGetValue(mutation.PressureId, out var current))
                changes[mutation.PressureId] = current + mutation.Delta;
            else
                changes[mutation.PressureId] = mutation.Delta;
        }

        return changes;
    }

    private List<RelationshipAdded> BuildTransitionRelationships(
        Entity currentEra, Entity nextEra, ActionContext ctx, string narrativeGroupId)
    {
        var rels = new List<RelationshipAdded>
        {
            // Supersedes relationship: next era supersedes current
            new(Kind: FrameworkPrimitives.RelationshipKinds.Supersedes.Value,
                Src: nextEra.Id.Value,
                Dst: currentEra.Id.Value,
                Strength: 1.0,
                Category: "",
                CatalyzedBy: "",
                CreatedAt: Runtime.CurrentTick,
                ActionContext: ctx,
                NarrativeGroupId: narrativeGroupId)
        };

        // Link prominent entities to the ending era (active_during)
        var prominent = Runtime.GetEntities()
            .Where(e =>
                e.Prominence.Value >= 2.0 &&
                e.Kind != FrameworkPrimitives.EntityKinds.Era &&
                e.CreatedAtTick >= currentEra.Temporal.StartTick &&
                e.CreatedAtTick < Runtime.CurrentTick)
            .Take(10)
            .ToList();

        foreach (var entity in prominent)
        {
            rels.Add(new RelationshipAdded(
                Kind: FrameworkPrimitives.RelationshipKinds.ActiveDuring.Value,
                Src: entity.Id.Value,
                Dst: currentEra.Id.Value,
                Strength: 1.0,
                Category: "",
                CatalyzedBy: "",
                CreatedAt: Runtime.CurrentTick,
                ActionContext: ctx,
                NarrativeGroupId: narrativeGroupId));
        }

        return rels;
    }

    private (List<EntityModified> modifications, int lockedCount) SnapshotProminence(
        Era currentConfig, ActionContext ctx, string narrativeGroupId)
    {
        var modifications = new List<EntityModified>();
        if (!_config.ProminenceSnapshotEnabled)
            return (modifications, 0);

        var minThreshold = ProminenceHelper.LabelToThreshold(_config.ProminenceSnapshotMinProminence);
        if (minThreshold < 0)
            return (modifications, 0);

        var toLock = Runtime.GetEntities()
            .Where(e =>
                e.Kind != FrameworkPrimitives.EntityKinds.Era &&
                e.Prominence.Value >= minThreshold &&
                !e.Tags.Contains(FrameworkPrimitives.Tags.ProminenceLocked))
            .ToList();

        foreach (var entity in toLock)
        {
            Runtime.UpdateEntity(entity.Id, e =>
                e.Tags.Set(FrameworkPrimitives.Tags.ProminenceLocked, currentConfig.Id.Value));

            modifications.Add(new EntityModified(
                entity.Id,
                new Dictionary<string, object?> { ["prominence_locked"] = currentConfig.Id.Value },
                ctx, narrativeGroupId));
        }

        return (modifications, toLock.Count);
    }

    private IGraph CreateGraphAccessor()
    {
        // We need the IGraph for RuleContext creation.
        // WorldRuntime doesn't expose IGraph directly, so we create a
        // lightweight adapter that wraps runtime methods.
        return new RuntimeGraphAdapter(Runtime);
    }

    private static SystemResult EmptyResult(string description)
    {
        return SystemResult.Empty with { Description = description };
    }
}

/// <summary>
/// Adapter that wraps WorldRuntime to satisfy IGraph interface for RuleContext.
/// This enables condition evaluation through the standard ConditionEvaluator path.
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
            // Reconstruct mutable dictionary from read-only pressures
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
