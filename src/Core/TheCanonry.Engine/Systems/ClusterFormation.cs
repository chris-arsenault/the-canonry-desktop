using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Runtime;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.Primitives;
using TheCanonry.Schema.World;
using ExecutionContext = TheCanonry.Schema.World.ExecutionContext;

namespace TheCanonry.Engine.Systems;

/// <summary>
/// Clusters similar entities into meta-entities.
///
/// Flow per tick:
/// 1. Select eligible entities (by kind, status)
/// 2. Build similarity graph based on shared tags, culture, spatial proximity
/// 3. Detect clusters using greedy agglomerative clustering
/// 4. For clusters exceeding min size, create a meta-entity
/// 5. Create part_of (subsumes) relationships from members to meta
/// 6. Archive or update status of member entities
/// 7. Return SystemResult with new entities and relationships
/// </summary>
public sealed class ClusterFormation : ISimulationSystem
{
    private readonly IReadOnlyDictionary<string, object?> _config;

    public ClusterFormation(
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
        var entityKind = GetString("entityKind", "");
        var minClusterSize = GetInt("minClusterSize", 3);
        var maxClusterSize = GetInt("maxClusterSize", 20);
        var minimumSimilarity = GetDouble("minimumSimilarity", 0.5);
        var metaEntityKind = GetString("metaEntityKind", entityKind);
        var metaEntityStatus = GetString("metaEntityStatus", "active");
        var archiveMembers = GetBool("archiveMembers", true);
        var memberStatus = GetString("memberStatus", "");
        var runAtEpochEnd = GetBool("runAtEpochEnd", false);

        // Epoch check
        if (runAtEpochEnd)
        {
            var ticksPerEpoch = runtime.Config.TicksPerEpoch;
            if (ticksPerEpoch > 0 && runtime.CurrentTick % ticksPerEpoch != 0)
            {
                return Task.FromResult(new SystemResult(
                    [], [], [], [],
                    new Dictionary<string, double>(),
                    $"{Name}: not epoch end, skipping",
                    new Dictionary<string, object?>(),
                    new Dictionary<string, string>()));
            }
        }

        // Select entities
        var allEntities = string.IsNullOrEmpty(entityKind)
            ? runtime.GetEntities()
            : runtime.GetEntitiesByKind(new EntityKind(entityKind));

        // Filter to active entities only (not already meta or subsumed)
        var candidates = new List<Entity>();
        foreach (var e in allEntities)
        {
            if (e.Status == FrameworkPrimitives.Statuses.Historical) continue;
            if (e.Status == FrameworkPrimitives.Statuses.Subsumed) continue;
            if (e.Tags.Contains(FrameworkPrimitives.Tags.MetaEntity)) continue;
            candidates.Add(e);
        }

        if (candidates.Count < minClusterSize)
        {
            return Task.FromResult(new SystemResult(
                [], [], [], [],
                new Dictionary<string, double>(),
                $"{Name}: not enough entities ({candidates.Count})",
                new Dictionary<string, object?>(),
                new Dictionary<string, string>()));
        }

        // Detect clusters via greedy agglomerative approach
        var clusters = DetectClusters(candidates, minClusterSize, maxClusterSize, minimumSimilarity);

        var ctx = new ActionContext("system", Id, true);
        var relationshipsAdded = new List<RelationshipAdded>();
        var entitiesModified = new List<EntityModified>();
        var metaEntitiesCreated = new List<string>();
        var narrations = new Dictionary<string, string>();
        var pressureChanges = new Dictionary<string, double>();

        foreach (var cluster in clusters)
        {
            // Create meta-entity
            var metaId = CreateMetaEntity(cluster, metaEntityKind, metaEntityStatus, runtime);
            metaEntitiesCreated.Add(metaId.Value);

            // Create subsumes relationships from meta to members
            foreach (var member in cluster)
            {
                var rel = new Relationship(
                    metaId, member.Id,
                    new RelationshipKind("subsumes"),
                    1.0, 0.0, "",
                    new ExecutionContext(runtime.CurrentTick, ExecutionSource.System, Id, true, ""),
                    runtime.CurrentTick);

                runtime.AddRelationship(rel);
                relationshipsAdded.Add(new RelationshipAdded(
                    "subsumes", metaId.Value, member.Id.Value,
                    1.0, "", "", runtime.CurrentTick,
                    ctx, metaId.Value));

                // Also create part_of from member to meta
                var partOfRel = new Relationship(
                    member.Id, metaId,
                    FrameworkPrimitives.RelationshipKinds.PartOf,
                    1.0, 0.0, "",
                    new ExecutionContext(runtime.CurrentTick, ExecutionSource.System, Id, true, ""),
                    runtime.CurrentTick);
                runtime.AddRelationship(partOfRel);
                relationshipsAdded.Add(new RelationshipAdded(
                    "part_of", member.Id.Value, metaId.Value,
                    1.0, "", "", runtime.CurrentTick,
                    ctx, metaId.Value));
            }

            // Archive or update status of members
            foreach (var member in cluster)
            {
                if (!string.IsNullOrEmpty(memberStatus))
                {
                    runtime.UpdateEntity(member.Id, e =>
                        e.UpdateStatus(new EntityStatus(memberStatus), runtime.CurrentTick));
                    entitiesModified.Add(new EntityModified(
                        member.Id,
                        new Dictionary<string, object?> { ["status"] = memberStatus },
                        ctx, metaId.Value));
                }
                else if (archiveMembers)
                {
                    runtime.ArchiveEntity(member.Id, archiveRelationships: false);
                    entitiesModified.Add(new EntityModified(
                        member.Id,
                        new Dictionary<string, object?> { ["status"] = "historical" },
                        ctx, metaId.Value));
                }
            }
        }

        // Apply pressure changes
        ApplyConfigPressureChanges(pressureChanges, metaEntitiesCreated.Count > 0);

        return Task.FromResult(new SystemResult(
            relationshipsAdded,
            [],
            [],
            entitiesModified,
            pressureChanges,
            $"{Name}: {metaEntitiesCreated.Count} meta-entities formed from {clusters.Sum(c => c.Count)} members",
            new Dictionary<string, object?>(),
            narrations));
    }

    // =========================================================================
    // CLUSTERING
    // =========================================================================

    private static List<List<Entity>> DetectClusters(
        List<Entity> candidates, int minSize, int maxSize, double minimumSimilarity)
    {
        // Build similarity matrix and greedily form clusters
        var clusters = new List<List<Entity>>();
        var assigned = new HashSet<EntityId>();

        for (var i = 0; i < candidates.Count; i++)
        {
            if (assigned.Contains(candidates[i].Id)) continue;

            var cluster = new List<Entity> { candidates[i] };
            assigned.Add(candidates[i].Id);

            for (var j = i + 1; j < candidates.Count && cluster.Count < maxSize; j++)
            {
                if (assigned.Contains(candidates[j].Id)) continue;

                var similarity = ComputeSimilarity(candidates[i], candidates[j]);
                if (similarity >= minimumSimilarity)
                {
                    // Check similarity with all current cluster members
                    var fitsCluster = true;
                    foreach (var member in cluster)
                    {
                        if (ComputeSimilarity(member, candidates[j]) < minimumSimilarity * 0.7)
                        {
                            fitsCluster = false;
                            break;
                        }
                    }

                    if (fitsCluster)
                    {
                        cluster.Add(candidates[j]);
                        assigned.Add(candidates[j].Id);
                    }
                }
            }

            if (cluster.Count >= minSize)
                clusters.Add(cluster);
        }

        return clusters;
    }

    private static double ComputeSimilarity(Entity a, Entity b)
    {
        var score = 0.0;
        var maxScore = 3.0;

        // Same kind: +1
        if (a.Kind == b.Kind) score += 1.0;

        // Same culture: +1
        if (a.Culture == b.Culture) score += 1.0;

        // Shared tags: +1 for any overlap
        var sharedTags = 0;
        foreach (var (key, _) in a.Tags.All)
        {
            if (key == FrameworkPrimitives.Tags.MetaEntity) continue;
            if (b.Tags.Contains(key)) sharedTags++;
        }
        if (sharedTags > 0) score += Math.Min(1.0, sharedTags * 0.25);

        return score / maxScore;
    }

    private static EntityId CreateMetaEntity(
        List<Entity> cluster, string metaEntityKind, string metaEntityStatus,
        WorldRuntime runtime)
    {
        // Determine culture from majority
        var cultureCounts = new Dictionary<CultureId, int>();
        foreach (var e in cluster)
        {
            cultureCounts.TryGetValue(e.Culture, out var count);
            cultureCounts[e.Culture] = count + 1;
        }

        var majorityCulture = cluster[0].Culture;
        var maxCount = 0;
        foreach (var (culture, count) in cultureCounts)
        {
            if (count > maxCount) { maxCount = count; majorityCulture = culture; }
        }

        // Determine subtype from majority
        var subtypeCounts = new Dictionary<string, int>();
        foreach (var e in cluster)
        {
            subtypeCounts.TryGetValue(e.Subtype, out var count);
            subtypeCounts[e.Subtype] = count + 1;
        }
        var majoritySubtype = cluster[0].Subtype;
        var maxSubCount = 0;
        foreach (var (sub, count) in subtypeCounts)
        {
            if (count > maxSubCount) { maxSubCount = count; majoritySubtype = sub; }
        }

        // Aggregate coordinates (centroid)
        var sumX = 0.0;
        var sumY = 0.0;
        var sumZ = 0.0;
        foreach (var e in cluster)
        {
            sumX += e.Coordinates.X;
            sumY += e.Coordinates.Y;
            sumZ += e.Coordinates.Z;
        }
        var centroid = new SemanticCoordinates(
            sumX / cluster.Count,
            sumY / cluster.Count,
            sumZ / cluster.Count);

        // Determine prominence from cluster size
        var prominence = cluster.Count switch
        {
            >= 10 => 3.5,
            >= 6 => 2.5,
            _ => 1.5
        };

        // Build entity name from members
        var memberNames = string.Join(", ", cluster.Take(3).Select(e => e.Name));
        if (cluster.Count > 3)
            memberNames += $" (+{cluster.Count - 3} more)";

        var metaEntity = new Entity(
            id: new EntityId($"meta-{Guid.NewGuid():N}"),
            kind: new EntityKind(metaEntityKind),
            subtype: majoritySubtype,
            name: $"Cluster of {memberNames}",
            culture: majorityCulture,
            eraId: cluster[0].EraId,
            coordinates: centroid,
            createdBy: new ExecutionContext(
                runtime.CurrentTick, ExecutionSource.System, "cluster_formation", true, ""),
            tick: runtime.CurrentTick);

        metaEntity.SetProminence(new Prominence(prominence), runtime.CurrentTick);
        metaEntity.UpdateStatus(new EntityStatus(metaEntityStatus), runtime.CurrentTick);
        metaEntity.Tags.Set(FrameworkPrimitives.Tags.MetaEntity, true);

        // Aggregate member tags (top 4)
        var tagCounts = new Dictionary<string, int>();
        foreach (var e in cluster)
        {
            foreach (var (key, _) in e.Tags.All)
            {
                if (key == FrameworkPrimitives.Tags.MetaEntity) continue;
                tagCounts.TryGetValue(key, out var count);
                tagCounts[key] = count + 1;
            }
        }
        var topTags = tagCounts.OrderByDescending(kv => kv.Value).Take(4);
        foreach (var (tag, _) in topTags)
            metaEntity.Tags.Set(tag, true);

        metaEntity.SetNarrativeHint(
            $"A unified tradition encompassing {string.Join(", ", cluster.Select(e => e.Name))}. Formed from {cluster.Count} related entities.",
            runtime.CurrentTick);

        return runtime.CreateEntity(metaEntity);
    }

    // =========================================================================
    // CONFIG HELPERS
    // =========================================================================

    private void ApplyConfigPressureChanges(Dictionary<string, double> pressureChanges, bool hadChanges)
    {
        if (!hadChanges) return;
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

    private bool GetBool(string key, bool defaultValue)
    {
        if (_config.TryGetValue(key, out var val) && val is bool b)
            return b;
        return defaultValue;
    }
}
