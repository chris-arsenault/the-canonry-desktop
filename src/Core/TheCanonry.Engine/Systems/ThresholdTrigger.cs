using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Rules;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Engine.Runtime;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

namespace TheCanonry.Engine.Systems;

/// <summary>
/// Detects conditions and sets tags/pressures for templates.
///
/// Design philosophy:
/// - Systems observe and label reality with tags describing current state
/// - Tags use state-descriptive names (e.g., "power_vacuum", "war_brewing")
/// - Templates react to these tags and create entities
/// - After creation, templates transition tags (e.g., "war_brewing" -> "at_war")
///
/// The system evaluates conditions against selected entities, clusters matches,
/// and applies configured mutations (set tags, modify pressures, change status, etc.).
///
/// Configuration comes as a dictionary with:
/// - selection: How to find entities to evaluate
/// - conditions: Conditions that must ALL be true for an entity to match
/// - actions: Mutations to apply on matching entities
/// - clusterMode: individual | all_matching | by_relationship
/// - cooldownTag: If present on entity, skip it
/// - throttleChance: Probability of running each tick (0-1)
/// - pressureChanges: Pressure modifications when trigger fires
/// </summary>
public sealed class ThresholdTrigger : ISimulationSystem
{
    private readonly IReadOnlyDictionary<string, object?> _config;

    public ThresholdTrigger(
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
        if (throttleChance < 1.0 && Runtime.NextRandomDouble() > throttleChance * modifier)
        {
            return Task.FromResult(SystemResult.Empty with
            {
                Description = $"{Name}: dormant"
            });
        }

        // Select entities
        var entities = SelectEntitiesFromConfig();

        // Apply cooldown filter
        var cooldownTag = GetString("cooldownTag", "");
        if (!string.IsNullOrEmpty(cooldownTag))
        {
            entities = entities.Where(e => !e.Tags.Contains(cooldownTag)).ToList();
        }

        // Create graph adapter for condition evaluation
        var graphAdapter = new RuntimeGraphAdapter(Runtime);
        var currentEraId = Runtime.CurrentEra?.Id.Value ?? "";

        // Evaluate conditions on each entity
        var conditions = GetConditions();
        var matchingEntities = new List<Entity>();

        foreach (var entity in entities)
        {
            if (EvaluateAllConditions(entity, conditions, graphAdapter, currentEraId))
                matchingEntities.Add(entity);
        }

        if (matchingEntities.Count == 0)
        {
            return Task.FromResult(SystemResult.Empty with
            {
                Description = $"{Name}: no matches"
            });
        }

        // Cluster matching entities
        var clusters = ClusterEntities(matchingEntities);
        var minClusterSize = GetInt("minClusterSize", 1);

        // Remove clusters below minimum size
        var validClusters = new Dictionary<string, List<Entity>>();
        foreach (var (clusterId, members) in clusters)
        {
            if (members.Count >= minClusterSize)
                validClusters[clusterId] = members;
        }

        if (validClusters.Count == 0)
        {
            return Task.FromResult(SystemResult.Empty with
            {
                Description = $"{Name}: clusters too small"
            });
        }

        // Apply actions
        var actionCtx = new ActionContext("system", Id, true);
        var ruleCtx = RuleContext.CreateForSystem(graphAdapter, currentEraId);
        var modifications = new List<EntityModified>();
        var relationshipsAdded = new List<RelationshipAdded>();
        var pressureChanges = new Dictionary<string, double>();

        var actions = GetActions();
        foreach (var (clusterId, members) in validClusters)
        {
            // Apply cluster-level actions (pressure modifications)
            foreach (var action in actions)
            {
                if (action is ModifyPressureMutation pressureMut)
                {
                    if (pressureChanges.TryGetValue(pressureMut.PressureId, out var current))
                        pressureChanges[pressureMut.PressureId] = current + pressureMut.Delta;
                    else
                        pressureChanges[pressureMut.PressureId] = pressureMut.Delta;
                }
            }

            // Apply per-member actions
            foreach (var member in members)
            {
                var memberCtx = ruleCtx.WithSelf(member);

                foreach (var action in actions)
                {
                    // Skip cluster-level and between-matching actions
                    if (action is ModifyPressureMutation) continue;
                    if (action is CreateRelationshipMutation createRel && IsBetweenMatchingAction(action))
                    {
                        // Handle between-matching in pairs
                        continue;
                    }

                    if (MutationApplier.Apply(action, memberCtx))
                    {
                        modifications.Add(new EntityModified(
                            member.Id,
                            new Dictionary<string, object?> { ["triggered_by"] = Id },
                            actionCtx,
                            clusterId));
                    }
                }
            }

            // Apply between-matching relationship creation
            foreach (var action in actions)
            {
                if (action is CreateRelationshipMutation createRel && IsBetweenMatchingAction(action) &&
                    members.Count >= 2)
                {
                    var relKind = new RelationshipKind(createRel.Kind);
                    for (var i = 0; i < members.Count; i++)
                    {
                        for (var j = i + 1; j < members.Count; j++)
                        {
                            if (Runtime.HasRelationship(members[i].Id, members[j].Id, relKind))
                                continue;

                            relationshipsAdded.Add(new RelationshipAdded(
                                Kind: createRel.Kind,
                                Src: members[i].Id.Value,
                                Dst: members[j].Id.Value,
                                Strength: createRel.Strength,
                                Category: createRel.Category,
                                CatalyzedBy: "",
                                CreatedAt: Runtime.CurrentTick,
                                ActionContext: actionCtx,
                                NarrativeGroupId: clusterId));
                        }
                    }
                }
            }
        }

        // Add config-level pressure changes if any clusters fired
        if (validClusters.Count > 0)
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
            RelationshipsAdjusted: [],
            RelationshipsToArchive: [],
            EntitiesModified: modifications,
            PressureChanges: pressureChanges,
            Description: $"{Name}: {validClusters.Count} trigger(s), {modifications.Count} entities tagged",
            Details: new Dictionary<string, object?>(),
            NarrationsByGroup: new Dictionary<string, string>()));
    }

    // =========================================================================
    // ENTITY SELECTION
    // =========================================================================

    private IReadOnlyList<Entity> SelectEntitiesFromConfig()
    {
        var kind = GetString("selection.kind", "");
        if (string.IsNullOrEmpty(kind))
            return Runtime.GetEntities();

        var status = GetString("selection.status", "");
        return Runtime.FindEntities(EntityCriteria.Create(
            kind: new EntityKind(kind),
            status: string.IsNullOrEmpty(status) ? null : new EntityStatus(status)));
    }

    // =========================================================================
    // CONDITION EVALUATION
    // =========================================================================

    private IReadOnlyList<Condition> GetConditions()
    {
        if (_config.TryGetValue("conditions", out var val) && val is IReadOnlyList<Condition> conditions)
            return conditions;
        return [];
    }

    private static bool EvaluateAllConditions(
        Entity entity, IReadOnlyList<Condition> conditions,
        IGraph graph, string currentEraId)
    {
        if (conditions.Count == 0) return true;

        var ctx = RuleContext.CreateForSystem(graph, currentEraId).WithSelf(entity);

        foreach (var condition in conditions)
        {
            if (!ConditionEvaluator.Evaluate(condition, ctx, entity))
                return false;
        }
        return true;
    }

    // =========================================================================
    // CLUSTERING
    // =========================================================================

    private Dictionary<string, List<Entity>> ClusterEntities(List<Entity> entities)
    {
        var clusters = new Dictionary<string, List<Entity>>();
        var mode = GetString("clusterMode", "individual");

        switch (mode)
        {
            case "all_matching":
                if (entities.Count > 0)
                {
                    var clusterId = $"cluster_{Guid.NewGuid():N}";
                    clusters[clusterId] = entities;
                }
                break;

            case "by_relationship":
            {
                var relKind = GetString("clusterRelationshipKind", "");
                if (string.IsNullOrEmpty(relKind))
                {
                    // Fallback to individual
                    foreach (var e in entities)
                        clusters[e.Id.Value] = [e];
                    break;
                }

                var entityTargets = new Dictionary<string, HashSet<string>>();
                foreach (var e in entities)
                {
                    var targets = new HashSet<string>();
                    foreach (var rel in Runtime.GetRelationships())
                    {
                        if (rel.Kind != new RelationshipKind(relKind)) continue;
                        if (rel.SourceId == e.Id) targets.Add(rel.TargetId.Value);
                        if (rel.TargetId == e.Id) targets.Add(rel.SourceId.Value);
                    }
                    entityTargets[e.Id.Value] = targets;
                }

                var visited = new HashSet<string>();
                foreach (var e in entities)
                {
                    if (visited.Contains(e.Id.Value)) continue;

                    var cluster = new List<Entity> { e };
                    visited.Add(e.Id.Value);
                    var myTargets = entityTargets.GetValueOrDefault(e.Id.Value, []);

                    foreach (var other in entities)
                    {
                        if (visited.Contains(other.Id.Value)) continue;
                        var otherTargets = entityTargets.GetValueOrDefault(other.Id.Value, []);
                        if (myTargets.Overlaps(otherTargets))
                        {
                            cluster.Add(other);
                            visited.Add(other.Id.Value);
                        }
                    }

                    clusters[$"cluster_{Guid.NewGuid():N}"] = cluster;
                }
                break;
            }

            default: // individual
                foreach (var e in entities)
                    clusters[e.Id.Value] = [e];
                break;
        }

        return clusters;
    }

    // =========================================================================
    // ACTIONS
    // =========================================================================

    private IReadOnlyList<Mutation> GetActions()
    {
        if (_config.TryGetValue("actions", out var val) && val is IReadOnlyList<Mutation> actions)
            return actions;
        return [];
    }

    private bool IsBetweenMatchingAction(Mutation action)
    {
        // Check if this action was marked as betweenMatching in config
        if (_config.TryGetValue("actions", out var val) && val is IReadOnlyList<object?> actionsList)
        {
            foreach (var actionObj in actionsList)
            {
                if (actionObj is IReadOnlyDictionary<string, object?> actionDict &&
                    actionDict.TryGetValue("betweenMatching", out var bm) &&
                    bm is true)
                    return true;
            }
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

    private int GetInt(string key, int defaultValue)
    {
        if (_config.TryGetValue(key, out var val))
        {
            if (val is int i) return i;
            if (val is double d) return (int)d;
        }
        return defaultValue;
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
}

/// <summary>
/// Adapter that wraps WorldRuntime to satisfy IGraph interface for RuleContext.
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
            foreach (var kvp in _runtime.AllPressures)
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
