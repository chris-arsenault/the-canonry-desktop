namespace TheCanonry.Engine.Systems;

using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Runtime;
using TheCanonry.Schema.Config;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.Primitives;
using TheCanonry.Schema.World;

/// <summary>
/// Handles decay, reinforcement, and culling of relationships.
///
/// Lifecycle management:
/// - Decay: Reduces strength over time based on relationship kind's decay rate
/// - Reinforcement: Increases strength when entities share proximity relationships
/// - Culling: Archives weak relationships below the cull threshold
///
/// Decay rates:
/// - none: 0 (permanent, never decays)
/// - slow: 0.01 per cycle
/// - medium: 0.03 per cycle
/// - fast: 0.06 per cycle
///
/// Framework relationships (supersedes, part_of, active_during, etc.) never decay
/// and are never cullable - they are structural.
/// </summary>
public sealed class RelationshipMaintenance : ISimulationSystem
{
    private readonly RelationshipMaintenanceConfig _config;

    public RelationshipMaintenance(RelationshipMaintenanceConfig config, WorldRuntime runtime)
    {
        _config = config;
        Runtime = runtime;
    }

    public string Id => _config.Id;
    public string Name => _config.Name;

    /// <summary>The runtime this system operates on.</summary>
    internal WorldRuntime Runtime { get; }

    /// <summary>The typed configuration for this system.</summary>
    internal RelationshipMaintenanceConfig Config => _config;

    public void Initialize() { }

    public Task<SystemResult> ApplyAsync(double modifier)
    {
        // Only run on maintenance frequency ticks
        if (_config.MaintenanceFrequency > 0 && Runtime.CurrentTick % _config.MaintenanceFrequency != 0)
        {
            return Task.FromResult(SystemResult.Empty with
            {
                Description = "Relationship maintenance dormant"
            });
        }

        var allRels = Runtime.GetRelationships(includeHistorical: true);
        var activeCount = 0;
        foreach (var r in allRels)
        {
            if (r.Status != FrameworkPrimitives.Statuses.Historical)
                activeCount++;
        }

        var stats = new MaintenanceStats();
        var modifiedEntityIds = new HashSet<string>();

        foreach (var rel in allRels)
        {
            // Skip already-archived relationships
            if (rel.Status == FrameworkPrimitives.Statuses.Historical)
                continue;

            ProcessRelationship(rel, modifier, stats, modifiedEntityIds);
        }

        var actionCtx = new ActionContext("framework", Id, true);
        var entitiesModified = modifiedEntityIds
            .Select(id => new EntityModified(
                new EntityId(id),
                new Dictionary<string, object?> { ["updatedAt"] = Runtime.CurrentTick },
                actionCtx,
                ""))
            .ToList();

        return Task.FromResult(new SystemResult(
            RelationshipsAdded: [],
            RelationshipsAdjusted: [],
            RelationshipsToArchive: [],
            EntitiesModified: entitiesModified,
            PressureChanges: new Dictionary<string, double>(),
            Description: BuildDescription(stats, activeCount),
            Details: new Dictionary<string, object?>(),
            NarrationsByGroup: new Dictionary<string, string>()));
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

    private void ProcessRelationship(
        Relationship rel, double modifier,
        MaintenanceStats stats, HashSet<string> modifiedEntityIds)
    {
        var src = Runtime.GetEntity(rel.SourceId);
        var dst = Runtime.GetEntity(rel.TargetId);

        // Orphaned relationship: entities no longer exist
        if (src is null || dst is null)
        {
            stats.Removed++;
            return;
        }

        var age = Math.Min(
            Runtime.CurrentTick - src.CreatedAtTick,
            Runtime.CurrentTick - dst.CreatedAtTick);
        var isYoung = age < _config.GracePeriod;

        var decayRate = GetDecayRate(rel.Kind);
        var cullable = IsCullable(rel.Kind);

        var strength = rel.Strength;
        var decayed = false;
        var reinforced = false;

        // Apply decay (skip for young relationships and non-decaying kinds)
        if (!isYoung && decayRate != DecayRateValue.None)
        {
            var decayAmount = GetDecayAmount(decayRate) * modifier;
            strength = Math.Max(0.0, strength - decayAmount);
            decayed = true;
        }

        // Apply reinforcement if entities are in proximity
        if (_config.ProximityRelationshipKinds.Count > 0 &&
            AreInProximity(rel.SourceId, rel.TargetId))
        {
            strength = Math.Min(_config.MaxStrength, strength + _config.ReinforcementBonus * modifier);
            reinforced = true;
        }

        if (decayed) stats.Decayed++;
        if (reinforced) stats.Reinforced++;

        // Cull weak relationships (skip young and non-cullable)
        if (!isYoung && cullable && strength < _config.CullThreshold)
        {
            rel.Archive(Runtime.CurrentTick);
            stats.Archived++;
            modifiedEntityIds.Add(rel.SourceId.Value);
            modifiedEntityIds.Add(rel.TargetId.Value);
            return;
        }

        // Update strength if changed
        if (Math.Abs(rel.Strength - strength) > 1e-9)
        {
            var delta = strength - rel.Strength;
            if (delta > 0)
                rel.Reinforce(delta);
            else
                rel.Decay(-delta / rel.Strength); // Decay uses rate, not absolute
        }
    }

    /// <summary>
    /// Check if two entities are in proximity via shared related entities.
    /// Entities are in proximity if they both have relationships of the configured
    /// proximity kinds pointing to the same destination entity.
    /// </summary>
    private bool AreInProximity(EntityId srcId, EntityId dstId)
    {
        var srcDestinations = new HashSet<string>();

        foreach (var rel in Runtime.GetRelationships())
        {
            if (rel.SourceId != srcId) continue;
            foreach (var kind in _config.ProximityRelationshipKinds)
            {
                if (rel.Kind == new RelationshipKind(kind))
                {
                    srcDestinations.Add(rel.TargetId.Value);
                    break;
                }
            }
        }

        if (srcDestinations.Count == 0) return false;

        foreach (var rel in Runtime.GetRelationships())
        {
            if (rel.SourceId != dstId) continue;
            foreach (var kind in _config.ProximityRelationshipKinds)
            {
                if (rel.Kind == new RelationshipKind(kind) &&
                    srcDestinations.Contains(rel.TargetId.Value))
                    return true;
            }
        }

        return false;
    }

    private DecayRateValue GetDecayRate(RelationshipKind kind)
    {
        // Framework relationships NEVER decay
        if (FrameworkPrimitives.IsFrameworkRelationshipKind(kind))
            return DecayRateValue.None;

        var def = FindRelationshipKindDef(kind);
        if (def is null) return DecayRateValue.Medium;

        return def.DecayRate.ToLowerInvariant() switch
        {
            "none" => DecayRateValue.None,
            "slow" => DecayRateValue.Slow,
            "fast" => DecayRateValue.Fast,
            _ => DecayRateValue.Medium
        };
    }

    private bool IsCullable(RelationshipKind kind)
    {
        // Framework relationships are NEVER cullable
        if (FrameworkPrimitives.IsFrameworkRelationshipKind(kind))
            return false;

        var def = FindRelationshipKindDef(kind);
        return def?.Cullable ?? true;
    }

    private RelationshipKindDefinition? FindRelationshipKindDef(RelationshipKind kind)
    {
        foreach (var rk in Runtime.Config.Schema.RelationshipKinds)
        {
            if (rk.Kind == kind) return rk;
        }
        return null;
    }

    private static double GetDecayAmount(DecayRateValue rate)
    {
        return rate switch
        {
            DecayRateValue.None => 0.0,
            DecayRateValue.Slow => 0.01,
            DecayRateValue.Medium => 0.03,
            DecayRateValue.Fast => 0.06,
            _ => 0.03
        };
    }

    private static string BuildDescription(MaintenanceStats stats, int originalCount)
    {
        var parts = new List<string>();
        if (stats.Decayed > 0) parts.Add($"{stats.Decayed} decayed");
        if (stats.Reinforced > 0) parts.Add($"{stats.Reinforced} reinforced");
        if (stats.Archived > 0) parts.Add($"{stats.Archived} archived");
        if (stats.Removed > 0) parts.Add($"{stats.Removed} removed (orphaned)");

        return parts.Count > 0
            ? $"Relationship maintenance: {string.Join(", ", parts)} ({originalCount} total)"
            : $"Relationship maintenance: all {originalCount} relationships stable";
    }

    private enum DecayRateValue
    {
        None,
        Slow,
        Medium,
        Fast
    }

    private sealed class MaintenanceStats
    {
        public int Decayed;
        public int Reinforced;
        public int Archived;
        public int Removed;
    }
}
