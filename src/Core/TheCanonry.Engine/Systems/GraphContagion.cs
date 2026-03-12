namespace TheCanonry.Engine.Systems;

using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Runtime;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.Primitives;
using TheCanonry.Schema.World;

/// <summary>
/// Spreads states/relationships through network connections using an SIR
/// (Susceptible-Infected-Recovered) epidemic model.
///
/// Flow per tick:
/// 1. Select population entities via config filters
/// 2. Categorize into infected / susceptible / immune
/// 3. For each susceptible entity, find infected contacts via vectors
/// 4. Compute transmission probability (base rate + contact multiplier, modified by susceptibility)
/// 5. Roll for infection; on success apply the infection action (tag or relationship)
/// 6. Process recovery: infected entities may gain immunity
/// 7. Return SystemResult with all modifications
/// </summary>
public sealed class GraphContagion : ISimulationSystem
{
    private readonly IReadOnlyDictionary<string, object?> _config;

    public GraphContagion(
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

        // Read config values with safe defaults
        var throttleChance = GetDouble("throttleChance", 1.0);
        var baseRate = GetDouble("baseRate", 0.1);
        var contactMultiplier = GetDouble("contactMultiplier", 0.05);
        var maxProbability = GetDouble("maxProbability", 0.8);
        var maxDepth = GetInt("maxDepth", 10);
        var spreadTag = GetString("spreadTag", "infected");
        var immunityTag = GetString("immunityTag", "");
        var recoveryRate = GetDouble("recoveryRate", 0.0);
        var vectorKind = GetString("vectorKind", "allied_with");
        var vectorDirection = GetDirection("vectorDirection", Direction.Both);
        var minStrength = GetDouble("minStrength", 0.0);
        var entityKind = GetString("entityKind", "");

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

        // Get population
        var allEntities = string.IsNullOrEmpty(entityKind)
            ? runtime.GetEntities()
            : runtime.GetEntitiesByKind(new EntityKind(entityKind));

        // Categorize into S-I-R
        var infected = new List<Entity>();
        var susceptible = new List<Entity>();
        var immune = new List<Entity>();

        foreach (var entity in allEntities)
        {
            if (entity.Tags.Contains(spreadTag))
                infected.Add(entity);
            else if (!string.IsNullOrEmpty(immunityTag) && entity.Tags.Contains(immunityTag))
                immune.Add(entity);
            else
                susceptible.Add(entity);
        }

        if (infected.Count == 0)
        {
            return Task.FromResult(new SystemResult(
                [], [], [], [],
                new Dictionary<string, double>(),
                $"{Name}: no carriers",
                new Dictionary<string, object?>(),
                new Dictionary<string, string>()));
        }

        var ctx = new ActionContext("system", Id, true);
        var modifications = new List<EntityModified>();
        var relationships = new List<RelationshipAdded>();
        var pressureChanges = new Dictionary<string, double>();
        var narrations = new Dictionary<string, string>();
        var newInfections = new List<string>();

        // Build infected ID set for fast lookup
        var infectedIds = new HashSet<EntityId>();
        foreach (var e in infected)
            infectedIds.Add(e.Id);

        // BFS-based spreading: process susceptible entities that are within contact of infected
        foreach (var entity in susceptible)
        {
            // Find infected contacts via relationship vectors
            var contacts = GetContacts(entity.Id, new RelationshipKind(vectorKind), vectorDirection, minStrength, runtime);
            var infectedContactCount = 0;

            foreach (var contactId in contacts)
            {
                if (infectedIds.Contains(contactId))
                    infectedContactCount++;
            }

            if (infectedContactCount == 0) continue;

            // Calculate transmission probability
            var susceptibilityMod = GetSusceptibilityModifier(entity);
            var prob = baseRate + (infectedContactCount * contactMultiplier);
            var modifiedProb = prob * (1.0 - susceptibilityMod);
            var infectionProb = Math.Min(maxProbability, Math.Max(0.0, modifiedProb));

            // Roll for infection
            if (runtime.NextRandomDouble() >= infectionProb * modifier) continue;

            // Depth check: BFS distance from any initially infected entity
            if (maxDepth > 0)
            {
                var depth = ComputeMinBfsDepth(entity.Id, infectedIds,
                    new RelationshipKind(vectorKind), vectorDirection, minStrength, maxDepth, runtime);
                if (depth > maxDepth) continue;
            }

            // Apply infection: set the spread tag
            var changes = new Dictionary<string, object?> { ["tag:" + spreadTag] = true };
            modifications.Add(new EntityModified(entity.Id, changes, ctx, entity.Id.Value));
            newInfections.Add(entity.Id.Value);

            // Also mutate the entity directly so subsequent iterations see it as infected
            runtime.UpdateEntity(entity.Id, e => e.Tags.Set(spreadTag, true));
            infectedIds.Add(entity.Id);
        }

        // Recovery phase: infected entities may recover and gain immunity
        if (recoveryRate > 0.0 && !string.IsNullOrEmpty(immunityTag))
        {
            foreach (var entity in infected)
            {
                if (runtime.NextRandomDouble() >= recoveryRate * modifier) continue;

                var changes = new Dictionary<string, object?>
                {
                    ["tag:" + immunityTag] = true,
                    ["tag_remove:" + spreadTag] = true
                };
                modifications.Add(new EntityModified(entity.Id, changes, ctx, entity.Id.Value));

                runtime.UpdateEntity(entity.Id, e =>
                {
                    e.Tags.Set(immunityTag, true);
                    e.Tags.Remove(spreadTag);
                });
            }
        }

        // Apply pressure changes from config
        if (newInfections.Count > 0)
        {
            ApplyConfigPressureChanges(pressureChanges);
        }

        return Task.FromResult(new SystemResult(
            relationships,
            [],
            [],
            modifications,
            pressureChanges,
            $"{Name}: {newInfections.Count} new infections, {modifications.Count} total modifications",
            new Dictionary<string, object?>(),
            narrations));
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private static HashSet<EntityId> GetContacts(
        EntityId entityId, RelationshipKind vectorKind, Direction direction,
        double minStrength, WorldRuntime runtime)
    {
        var contactIds = new HashSet<EntityId>();
        var rels = runtime.GetEntityRelationships(entityId, direction);

        foreach (var rel in rels)
        {
            if (rel.Kind != vectorKind) continue;
            if (rel.Strength < minStrength) continue;

            var otherId = rel.SourceId == entityId ? rel.TargetId : rel.SourceId;
            contactIds.Add(otherId);
        }

        return contactIds;
    }

    /// <summary>
    /// Compute minimum BFS distance from entity to any seed infected entity.
    /// Returns int.MaxValue if unreachable within maxDepth.
    /// </summary>
    private static int ComputeMinBfsDepth(
        EntityId entityId, HashSet<EntityId> infectedIds,
        RelationshipKind vectorKind, Direction direction, double minStrength,
        int maxDepth, WorldRuntime runtime)
    {
        var visited = new HashSet<EntityId> { entityId };
        var queue = new Queue<(EntityId Id, int Depth)>();
        queue.Enqueue((entityId, 0));

        while (queue.Count > 0)
        {
            var (currentId, depth) = queue.Dequeue();
            if (depth >= maxDepth) continue;

            var contacts = GetContacts(currentId, vectorKind, direction, minStrength, runtime);
            foreach (var contactId in contacts)
            {
                if (infectedIds.Contains(contactId))
                    return depth + 1;

                if (visited.Add(contactId))
                    queue.Enqueue((contactId, depth + 1));
            }
        }

        return int.MaxValue;
    }

    private double GetSusceptibilityModifier(Entity entity)
    {
        // Read susceptibility modifiers from config
        if (!_config.TryGetValue("susceptibilityModifiers", out var modsObj) || modsObj is not IReadOnlyDictionary<string, object?> mods)
            return 0.0;

        var total = 0.0;
        foreach (var (tag, modObj) in mods)
        {
            if (entity.Tags.Contains(tag) && modObj is double mod)
                total += mod;
        }
        return total;
    }

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
}
