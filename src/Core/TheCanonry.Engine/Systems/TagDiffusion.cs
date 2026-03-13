using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Runtime;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;

namespace TheCanonry.Engine.Systems;

/// <summary>
/// Propagates/diverges tags based on entity connectivity.
///
/// Two modes:
/// - Convergence: Connected entities gain shared tags (cultural unification)
/// - Divergence: Isolated entities gain unique tags (cultural drift)
///
/// Flow per tick:
/// 1. Select entities via config filters
/// 2. For convergence: entities with enough connections share tags
/// 3. For divergence: isolated entities gain random tags
/// 4. Return SystemResult with tag modifications
/// </summary>
public sealed class TagDiffusion : ISimulationSystem
{
    private readonly IReadOnlyDictionary<string, object?> _config;

    public TagDiffusion(
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
        var runtime = Runtime;

        // Read config
        var throttleChance = GetDouble("throttleChance", 1.0);
        var entityKind = GetString("entityKind", "");
        var connectionKind = GetString("connectionKind", "allied_with");
        var connectionDirection = GetDirection("connectionDirection", Direction.Both);
        var maxTags = GetInt("maxTags", 10);

        // Convergence config
        var convergenceTags = GetStringList("convergenceTags");
        var convergenceMinConnections = GetInt("convergenceMinConnections", 1);
        var convergenceProbability = GetDouble("convergenceProbability", 0.3);
        var convergenceMaxShared = GetInt("convergenceMaxSharedTags", 3);

        // Divergence config
        var divergenceTags = GetStringList("divergenceTags");
        var divergenceMaxConnections = GetInt("divergenceMaxConnections", 0);
        var divergenceProbability = GetDouble("divergenceProbability", 0.2);

        // Throttle check
        if (throttleChance < 1.0 && runtime.NextRandomDouble() >= throttleChance * modifier)
        {
            return Task.FromResult(new SystemResult(
                [], [], [], [],
                new Dictionary<string, double>(),
                $"{Name}: dormant",
                new Dictionary<string, object?>(),
                new Dictionary<string, string>()));
        }

        // Get entities
        var entities = string.IsNullOrEmpty(entityKind)
            ? runtime.GetEntities()
            : runtime.GetEntitiesByKind(new EntityKind(entityKind));

        if (entities.Count < 2)
        {
            return Task.FromResult(new SystemResult(
                [], [], [], [],
                new Dictionary<string, double>(),
                $"{Name}: not enough entities",
                new Dictionary<string, object?>(),
                new Dictionary<string, string>()));
        }

        var ctx = new ActionContext("system", Id, true);
        var modifications = new List<EntityModified>();
        var narrations = new Dictionary<string, string>();
        var entityIdSet = new HashSet<EntityId>(entities.Select(e => e.Id));

        // Track tag modifications per entity
        var modifiedTags = new Dictionary<EntityId, HashSet<string>>();
        var relKind = new RelationshipKind(connectionKind);

        // ===== CONVERGENCE =====
        if (convergenceTags.Count > 0)
        {
            foreach (var entity in entities)
            {
                var connected = GetConnectedInSet(entity.Id, relKind, connectionDirection, runtime, entityIdSet);
                if (connected.Count < convergenceMinConnections) continue;

                foreach (var other in connected)
                {
                    var otherEntity = runtime.GetEntity(other);
                    if (otherEntity is null) continue;

                    // Count shared convergence tags
                    var sharedCount = 0;
                    foreach (var tag in convergenceTags)
                    {
                        if (entity.Tags.Contains(tag) && otherEntity.Tags.Contains(tag))
                            sharedCount++;
                    }
                    if (sharedCount >= convergenceMaxShared) continue;

                    // Roll for convergence
                    if (runtime.NextRandomDouble() >= convergenceProbability * modifier) continue;

                    // Pick a tag that neither has
                    var candidateTags = new List<string>();
                    foreach (var tag in convergenceTags)
                    {
                        if (!entity.Tags.Contains(tag) && !otherEntity.Tags.Contains(tag))
                            candidateTags.Add(tag);
                    }

                    if (candidateTags.Count == 0) continue;

                    // Check tag limit
                    var currentTagCount = entity.Tags.All.Count;
                    if (!modifiedTags.TryGetValue(entity.Id, out var addedTags))
                        addedTags = [];

                    if (currentTagCount + addedTags.Count >= maxTags) continue;

                    var newTag = candidateTags[runtime.NextRandomInt(candidateTags.Count)];
                    addedTags.Add(newTag);
                    modifiedTags[entity.Id] = addedTags;
                    break; // one convergence per entity per tick
                }
            }
        }

        // ===== DIVERGENCE =====
        var divergentCount = 0;
        if (divergenceTags.Count > 0)
        {
            foreach (var entity in entities)
            {
                var connectionCount = CountConnections(entity.Id, relKind, connectionDirection, runtime);
                if (connectionCount > divergenceMaxConnections) continue;

                // Roll for divergence
                if (runtime.NextRandomDouble() >= divergenceProbability * modifier) continue;

                // Pick a tag not already held
                var candidateTags = new List<string>();
                foreach (var tag in divergenceTags)
                {
                    if (!entity.Tags.Contains(tag))
                        candidateTags.Add(tag);
                }

                if (candidateTags.Count == 0) continue;

                // Check tag limit
                var currentTagCount = entity.Tags.All.Count;
                if (!modifiedTags.TryGetValue(entity.Id, out var addedTags))
                    addedTags = [];

                if (currentTagCount + addedTags.Count >= maxTags) continue;

                var newTag = candidateTags[runtime.NextRandomInt(candidateTags.Count)];
                addedTags.Add(newTag);
                modifiedTags[entity.Id] = addedTags;
                divergentCount++;
            }
        }

        // Build modifications
        foreach (var (entityId, addedTags) in modifiedTags)
        {
            var changes = new Dictionary<string, object?>();
            foreach (var tag in addedTags)
                changes["tag:" + tag] = true;

            modifications.Add(new EntityModified(entityId, changes, ctx, entityId.Value));

            // Apply directly
            runtime.UpdateEntity(entityId, e =>
            {
                foreach (var tag in addedTags)
                    e.Tags.Set(tag, true);
            });
        }

        // Pressure changes
        var pressureChanges = new Dictionary<string, double>();
        if (modifications.Count > 0)
            ApplyConfigPressureChanges(pressureChanges);

        // Divergence-specific pressure
        ApplyDivergencePressure(pressureChanges, divergentCount, modifier);

        return Task.FromResult(new SystemResult(
            [],
            [],
            [],
            modifications,
            pressureChanges,
            $"{Name}: {modifications.Count} entities affected",
            new Dictionary<string, object?>(),
            narrations));
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private static List<EntityId> GetConnectedInSet(
        EntityId entityId, RelationshipKind relKind, Direction direction,
        WorldRuntime runtime, HashSet<EntityId> entityIdSet)
    {
        var result = new List<EntityId>();
        var rels = runtime.GetEntityRelationships(entityId, direction);

        foreach (var rel in rels)
        {
            if (rel.Kind != relKind) continue;
            var otherId = rel.SourceId == entityId ? rel.TargetId : rel.SourceId;
            if (entityIdSet.Contains(otherId))
                result.Add(otherId);
        }

        return result;
    }

    private static int CountConnections(
        EntityId entityId, RelationshipKind relKind, Direction direction,
        WorldRuntime runtime)
    {
        var ids = new HashSet<EntityId>();
        var rels = runtime.GetEntityRelationships(entityId, direction);

        foreach (var rel in rels)
        {
            if (rel.Kind != relKind) continue;
            var otherId = rel.SourceId == entityId ? rel.TargetId : rel.SourceId;
            ids.Add(otherId);
        }

        return ids.Count;
    }

    // =========================================================================
    // CONFIG HELPERS
    // =========================================================================

    private void ApplyConfigPressureChanges(Dictionary<string, double> pressureChanges)
    {
        if (!_config.TryGetValue("pressureChanges", out var pcObj) || pcObj is not IReadOnlyDictionary<string, object?> pc)
            return;

        foreach (var (pressureId, deltaObj) in pc)
        {
            if (deltaObj is double delta)
            {
                pressureChanges.TryGetValue(pressureId, out var current);
                pressureChanges[pressureId] = current + delta;
            }
        }
    }

    private void ApplyDivergencePressure(Dictionary<string, double> pressureChanges, int divergentCount, double modifier)
    {
        var pressureName = GetString("divergencePressureName", "");
        var minDivergent = GetInt("divergencePressureMinDivergent", 0);
        var delta = GetDouble("divergencePressureDelta", 0.0);

        if (string.IsNullOrEmpty(pressureName) || minDivergent <= 0) return;
        if (divergentCount < minDivergent) return;

        pressureChanges.TryGetValue(pressureName, out var current);
        pressureChanges[pressureName] = current + delta * modifier;
    }

    private string GetString(string key, string defaultValue)
    {
        if (_config.TryGetValue(key, out var val) && val is string s)
            return s;
        return defaultValue;
    }

    private double GetDouble(string key, double defaultValue)
    {
        if (_config.TryGetValue(key, out var val))
        {
            if (val is double d) return d;
            if (val is int i) return i;
            if (val is float f) return f;
        }
        return defaultValue;
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

    private Direction GetDirection(string key, Direction defaultValue)
    {
        if (_config.TryGetValue(key, out var val) && val is string s)
        {
            return s.ToLowerInvariant() switch
            {
                "source" or "src" => Direction.Source,
                "target" or "dst" => Direction.Target,
                "both" => Direction.Both,
                _ => defaultValue
            };
        }
        return defaultValue;
    }

    private IReadOnlyList<string> GetStringList(string key)
    {
        if (_config.TryGetValue(key, out var val))
        {
            if (val is IReadOnlyList<string> list) return list;
            if (val is List<string> mutableList) return mutableList;
            if (val is string[] array) return array;
            if (val is IEnumerable<object?> objs)
            {
                var result = new List<string>();
                foreach (var obj in objs)
                {
                    if (obj is string s) result.Add(s);
                }
                return result;
            }
        }
        return [];
    }
}
