namespace TheCanonry.Engine.Rules;

using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

/// <summary>
/// Static evaluator for all Metric subtypes. Metrics evaluate to a double value.
/// </summary>
public static class MetricEvaluator
{
    /// <summary>
    /// Evaluate a metric against the given context.
    /// Uses the context's Self entity for per-entity metrics.
    /// </summary>
    public static double Evaluate(Metric metric, RuleContext context) =>
        Evaluate(metric, context, context.Self);

    /// <summary>
    /// Evaluate a metric with an explicit entity for per-entity metrics.
    /// </summary>
    public static double Evaluate(Metric metric, RuleContext context, Entity? entity) => metric switch
    {
        EntityCountMetric m => EvaluateEntityCount(m, context),
        RelationshipCountMetric m => EvaluateRelationshipCount(m, context, entity),
        TagCountMetric m => EvaluateTagCount(m, context),
        TotalEntitiesMetric m => EvaluateTotalEntities(m, context),
        ConstantMetric m => m.Value * m.Coefficient,
        ConnectionCountMetric m => EvaluateConnectionCount(m, context, entity),
        RatioMetric m => EvaluateRatio(m, context),
        StatusRatioMetric m => EvaluateStatusRatio(m, context),
        CrossCultureRatioMetric m => EvaluateCrossCultureRatio(m, context),
        SharedRelationshipMetric m => EvaluateSharedRelationship(m, context, entity),
        ProminenceMultiplierMetric m => EvaluateProminenceMultiplier(m, entity),
        NeighborProminenceMetric m => EvaluateNeighborProminence(m, context, entity),
        NeighborKindCountMetric m => EvaluateNeighborKindCount(m, context, entity),
        ComponentSizeMetric m => EvaluateComponentSize(m, context, entity),
        DecayRateMetric m => EvaluateDecayRate(m),
        FalloffMetric m => EvaluateFalloff(m),
        _ => 0.0
    };

    // =========================================================================
    // COUNT METRICS
    // =========================================================================

    private static double EvaluateEntityCount(EntityCountMetric m, RuleContext ctx)
    {
        var entities = ctx.Graph.FindEntities(EntityCriteria.Create(
            kind: string.IsNullOrEmpty(m.Kind) ? null : new EntityKind(m.Kind),
            subtype: string.IsNullOrEmpty(m.Subtype) ? null : m.Subtype,
            status: string.IsNullOrEmpty(m.Status) ? null : new EntityStatus(m.Status)));

        var raw = entities.Count * m.Coefficient;
        return m.Cap > 0 ? Math.Min(raw, m.Cap) : raw;
    }

    private static double EvaluateRelationshipCount(RelationshipCountMetric m, RuleContext ctx, Entity? entity)
    {
        if (entity is null) return 0.0;

        var kindSet = new HashSet<string>(m.RelationshipKinds);
        var count = 0;

        foreach (var rel in ctx.Graph.GetRelationships())
        {
            if (kindSet.Count > 0 && !kindSet.Contains(rel.Kind.Value)) continue;
            if (rel.Strength < m.MinStrength) continue;

            var matches = m.Direction switch
            {
                Direction.Source => rel.SourceId == entity.Id,
                Direction.Target => rel.TargetId == entity.Id,
                Direction.Both => rel.SourceId == entity.Id || rel.TargetId == entity.Id,
                _ => false
            };

            if (matches) count++;
        }

        var raw = count * m.Coefficient;
        return m.Cap > 0 ? Math.Min(raw, m.Cap) : raw;
    }

    private static double EvaluateTagCount(TagCountMetric m, RuleContext ctx)
    {
        var count = 0;
        var tagSet = new HashSet<string>(m.Tags);

        foreach (var entity in ctx.Graph.GetEntities())
        {
            foreach (var tag in tagSet)
            {
                if (entity.Tags.Contains(tag))
                {
                    count++;
                    break;
                }
            }
        }

        var raw = count * m.Coefficient;
        return m.Cap > 0 ? Math.Min(raw, m.Cap) : raw;
    }

    private static double EvaluateTotalEntities(TotalEntitiesMetric m, RuleContext ctx)
    {
        var raw = ctx.Graph.GetEntityCount() * m.Coefficient;
        return m.Cap > 0 ? Math.Min(raw, m.Cap) : raw;
    }

    private static double EvaluateConnectionCount(ConnectionCountMetric m, RuleContext ctx, Entity? entity)
    {
        if (entity is null) return 0.0;

        var kindSet = new HashSet<string>(m.RelationshipKinds);
        var count = 0;

        foreach (var rel in ctx.Graph.GetRelationships())
        {
            if (kindSet.Count > 0 && !kindSet.Contains(rel.Kind.Value)) continue;
            if (rel.Strength < m.MinStrength) continue;

            var matches = m.Direction switch
            {
                Direction.Source => rel.SourceId == entity.Id,
                Direction.Target => rel.TargetId == entity.Id,
                Direction.Both => rel.SourceId == entity.Id || rel.TargetId == entity.Id,
                _ => false
            };

            if (matches) count++;
        }

        var raw = count * m.Coefficient;
        return m.Cap > 0 ? Math.Min(raw, m.Cap) : raw;
    }

    // =========================================================================
    // RATIO METRICS
    // =========================================================================

    private static double EvaluateSimpleCount(SimpleCountMetric m, RuleContext ctx) => m switch
    {
        SimpleEntityCount s => ctx.Graph.FindEntities(EntityCriteria.Create(
            kind: string.IsNullOrEmpty(s.Kind) ? null : new EntityKind(s.Kind),
            subtype: string.IsNullOrEmpty(s.Subtype) ? null : s.Subtype,
            status: string.IsNullOrEmpty(s.Status) ? null : new EntityStatus(s.Status))).Count,
        SimpleRelationshipCount s => CountRelationships(ctx, s.RelationshipKinds),
        SimpleTagCount s => CountEntitiesWithTags(ctx, s.Tags),
        SimpleTotalEntities => ctx.Graph.GetEntityCount(),
        SimpleConstant s => s.Value,
        _ => 0.0
    };

    private static double EvaluateRatio(RatioMetric m, RuleContext ctx)
    {
        var numerator = EvaluateSimpleCount(m.Numerator, ctx);
        var denominator = EvaluateSimpleCount(m.Denominator, ctx);

        if (denominator == 0) return m.FallbackValue;

        var raw = (numerator / denominator) * m.Coefficient;
        return m.Cap > 0 ? Math.Min(raw, m.Cap) : raw;
    }

    private static double EvaluateStatusRatio(StatusRatioMetric m, RuleContext ctx)
    {
        var aliveStatus = new EntityStatus(m.AliveStatus);
        var kind = string.IsNullOrEmpty(m.Kind) ? (EntityKind?)null : new EntityKind(m.Kind);

        var all = ctx.Graph.FindEntities(EntityCriteria.Create(
            kind: kind,
            subtype: string.IsNullOrEmpty(m.Subtype) ? null : m.Subtype));

        if (all.Count == 0) return 0.0;

        var aliveCount = 0;
        foreach (var e in all)
        {
            if (e.Status == aliveStatus) aliveCount++;
        }

        var raw = ((double)aliveCount / all.Count) * m.Coefficient;
        return m.Cap > 0 ? Math.Min(raw, m.Cap) : raw;
    }

    private static double EvaluateCrossCultureRatio(CrossCultureRatioMetric m, RuleContext ctx)
    {
        var kindSet = new HashSet<string>(m.RelationshipKinds);
        var total = 0;
        var crossCulture = 0;

        foreach (var rel in ctx.Graph.GetRelationships())
        {
            if (kindSet.Count > 0 && !kindSet.Contains(rel.Kind.Value)) continue;

            total++;

            var src = ctx.Graph.GetEntity(rel.SourceId);
            var dst = ctx.Graph.GetEntity(rel.TargetId);
            if (src is not null && dst is not null && src.Culture != dst.Culture)
                crossCulture++;
        }

        if (total == 0) return 0.0;

        var raw = ((double)crossCulture / total) * m.Coefficient;
        return m.Cap > 0 ? Math.Min(raw, m.Cap) : raw;
    }

    // =========================================================================
    // EVOLUTION METRICS
    // =========================================================================

    private static double EvaluateSharedRelationship(SharedRelationshipMetric m, RuleContext ctx, Entity? entity)
    {
        if (entity is null) return 0.0;

        // Find entities that share a relationship target with the given entity
        var sharedKindSet = new HashSet<string>(m.SharedRelationshipKinds);
        var dir = m.SharedDirection;

        // Get targets of the entity via shared relationship kind
        var myTargets = new HashSet<EntityId>();
        foreach (var rel in ctx.Graph.GetRelationships())
        {
            if (!sharedKindSet.Contains(rel.Kind.Value)) continue;
            if (rel.Strength < m.MinStrength) continue;

            if (dir == Direction.Source && rel.SourceId == entity.Id)
                myTargets.Add(rel.TargetId);
            else if (dir == Direction.Target && rel.TargetId == entity.Id)
                myTargets.Add(rel.SourceId);
        }

        if (myTargets.Count == 0) return 0.0;

        // Count other entities that share at least one target
        var sharedCount = 0;
        foreach (var rel in ctx.Graph.GetRelationships())
        {
            if (!sharedKindSet.Contains(rel.Kind.Value)) continue;
            if (rel.Strength < m.MinStrength) continue;

            EntityId? candidateId = null;
            EntityId? targetId = null;

            if (dir == Direction.Source && rel.SourceId != entity.Id)
            {
                candidateId = rel.SourceId;
                targetId = rel.TargetId;
            }
            else if (dir == Direction.Target && rel.TargetId != entity.Id)
            {
                candidateId = rel.TargetId;
                targetId = rel.SourceId;
            }

            if (candidateId is not null && targetId is not null && myTargets.Contains(targetId.Value))
                sharedCount++;
        }

        var raw = sharedCount * m.Coefficient;
        return m.Cap > 0 ? Math.Min(raw, m.Cap) : raw;
    }

    // =========================================================================
    // PROMINENCE METRICS
    // =========================================================================

    private static double EvaluateProminenceMultiplier(ProminenceMultiplierMetric m, Entity? entity)
    {
        if (entity is null) return 1.0;

        var value = entity.Prominence.Value;
        var index = Math.Min(4, Math.Max(0, (int)Math.Floor(value)));

        return m.Mode switch
        {
            "success_chance" => index switch
            {
                0 => 0.6,
                1 => 0.8,
                2 => 1.0,
                3 => 1.2,
                _ => 1.5,
            },
            "action_rate" => index switch
            {
                0 => 0.3,
                1 => 0.6,
                2 => 1.0,
                3 => 1.5,
                _ => 2.0,
            },
            _ => 1.0
        };
    }

    private static double EvaluateNeighborProminence(NeighborProminenceMetric m, RuleContext ctx, Entity? entity)
    {
        if (entity is null) return 0.0;

        var kindSet = m.RelationshipKinds.Count > 0 ? new HashSet<string>(m.RelationshipKinds) : null;
        var totalProminence = 0.0;
        var count = 0;

        foreach (var rel in ctx.Graph.GetRelationships())
        {
            if (kindSet is not null && !kindSet.Contains(rel.Kind.Value)) continue;
            if (rel.Strength < m.MinStrength) continue;

            EntityId? neighborId = null;
            var matches = m.Direction switch
            {
                Direction.Source => rel.SourceId == entity.Id,
                Direction.Target => rel.TargetId == entity.Id,
                Direction.Both => rel.SourceId == entity.Id || rel.TargetId == entity.Id,
                _ => false
            };

            if (!matches) continue;

            neighborId = rel.SourceId == entity.Id ? rel.TargetId : rel.SourceId;
            var neighbor = ctx.Graph.GetEntity(neighborId.Value);
            if (neighbor is null) continue;

            totalProminence += neighbor.Prominence.Value;
            count++;
        }

        if (count == 0) return 0.0;

        var raw = (totalProminence / count) * m.Coefficient;
        return m.Cap > 0 ? Math.Min(raw, m.Cap) : raw;
    }

    // =========================================================================
    // NEIGHBOR METRICS
    // =========================================================================

    private static double EvaluateNeighborKindCount(NeighborKindCountMetric m, RuleContext ctx, Entity? entity)
    {
        if (entity is null) return 0.0;

        var viaKindSet = new HashSet<string>(m.Via);

        // Step 1: Traverse "via" relationship to get intermediate entities
        var intermediates = new HashSet<EntityId>();
        foreach (var rel in ctx.Graph.GetRelationships())
        {
            if (!viaKindSet.Contains(rel.Kind.Value)) continue;
            if (rel.Strength < m.MinStrength) continue;

            var matches = m.ViaDirection switch
            {
                Direction.Source => rel.SourceId == entity.Id,
                Direction.Target => rel.TargetId == entity.Id,
                Direction.Both => rel.SourceId == entity.Id || rel.TargetId == entity.Id,
                _ => false
            };

            if (matches)
            {
                var otherId = rel.SourceId == entity.Id ? rel.TargetId : rel.SourceId;
                intermediates.Add(otherId);
            }
        }

        // Step 2: If there's a "then" relationship, traverse from intermediates
        IEnumerable<Entity> candidates;
        if (!string.IsNullOrEmpty(m.Then))
        {
            var thenKind = new RelationshipKind(m.Then);
            var finalIds = new HashSet<EntityId>();

            foreach (var intId in intermediates)
            {
                foreach (var rel in ctx.Graph.GetRelationships())
                {
                    if (rel.Kind != thenKind) continue;

                    var matches = m.ThenDirection switch
                    {
                        Direction.Source => rel.SourceId == intId,
                        Direction.Target => rel.TargetId == intId,
                        Direction.Both => rel.SourceId == intId || rel.TargetId == intId,
                        _ => false
                    };

                    if (matches)
                    {
                        var otherId = rel.SourceId == intId ? rel.TargetId : rel.SourceId;
                        finalIds.Add(otherId);
                    }
                }
            }

            candidates = finalIds
                .Select(id => ctx.Graph.GetEntity(id))
                .Where(e => e is not null)!;
        }
        else
        {
            candidates = intermediates
                .Select(id => ctx.Graph.GetEntity(id))
                .Where(e => e is not null)!;
        }

        // Step 3: Count matching entities
        var count = 0;
        var targetKind = string.IsNullOrEmpty(m.Kind) ? (EntityKind?)null : new EntityKind(m.Kind);

        foreach (var e in candidates)
        {
            if (targetKind is not null && e.Kind != targetKind.Value) continue;
            if (!string.IsNullOrEmpty(m.Subtype) && e.Subtype != m.Subtype) continue;
            if (!string.IsNullOrEmpty(m.Status) && e.Status != new EntityStatus(m.Status)) continue;
            if (!string.IsNullOrEmpty(m.HasTag) && !e.Tags.Contains(m.HasTag)) continue;
            count++;
        }

        var raw = count * m.Coefficient;
        return m.Cap > 0 ? Math.Min(raw, m.Cap) : raw;
    }

    // =========================================================================
    // GRAPH TOPOLOGY METRICS
    // =========================================================================

    private static double EvaluateComponentSize(ComponentSizeMetric m, RuleContext ctx, Entity? entity)
    {
        if (entity is null) return 0.0;

        var adjacency = BuildAdjacency(ctx.Graph, m.RelationshipKinds, m.MinStrength);
        var size = ComputeComponentSize(adjacency, entity.Id.Value);

        var raw = size * m.Coefficient;
        return m.Cap > 0 ? Math.Min(raw, m.Cap) : raw;
    }

    // =========================================================================
    // DECAY/FALLOFF METRICS
    // =========================================================================

    private static double EvaluateDecayRate(DecayRateMetric m) => m.Rate switch
    {
        "none" => 0.0,
        "slow" => 0.01,
        "medium" => 0.05,
        "fast" => 0.1,
        _ => 0.0
    };

    private static double EvaluateFalloff(FalloffMetric m)
    {
        if (m.MaxDistance <= 0) return 0.0;

        return m.FalloffType switch
        {
            "absolute" => m.Distance <= m.MaxDistance ? 1.0 : 0.0,
            "none" => 1.0,
            "linear" => Math.Max(0.0, 1.0 - (m.Distance / m.MaxDistance)),
            "inverse_square" => m.Distance > 0 ? 1.0 / (m.Distance * m.Distance) : 1.0,
            "sqrt" => Math.Max(0.0, 1.0 - Math.Sqrt(m.Distance / m.MaxDistance)),
            "exponential" => Math.Exp(-m.Distance / m.MaxDistance),
            _ => 1.0
        };
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private static int CountRelationships(RuleContext ctx, IReadOnlyList<string> relationshipKinds)
    {
        var kindSet = new HashSet<string>(relationshipKinds);
        var count = 0;
        foreach (var rel in ctx.Graph.GetRelationships())
        {
            if (kindSet.Count == 0 || kindSet.Contains(rel.Kind.Value))
                count++;
        }
        return count;
    }

    private static int CountEntitiesWithTags(RuleContext ctx, IReadOnlyList<string> tags)
    {
        var count = 0;
        foreach (var entity in ctx.Graph.GetEntities())
        {
            foreach (var tag in tags)
            {
                if (entity.Tags.Contains(tag))
                {
                    count++;
                    break;
                }
            }
        }
        return count;
    }

    private static Dictionary<string, HashSet<string>> BuildAdjacency(
        IGraph graph, IReadOnlyList<string> relationshipKinds, double minStrength)
    {
        var adjacency = new Dictionary<string, HashSet<string>>();
        var kindSet = new HashSet<string>(relationshipKinds);

        foreach (var link in graph.GetRelationships())
        {
            if (!kindSet.Contains(link.Kind.Value)) continue;
            if (link.Strength < minStrength) continue;

            var src = link.SourceId.Value;
            var dst = link.TargetId.Value;

            if (!adjacency.ContainsKey(src)) adjacency[src] = [];
            if (!adjacency.ContainsKey(dst)) adjacency[dst] = [];

            adjacency[src].Add(dst);
            adjacency[dst].Add(src);
        }

        return adjacency;
    }

    private static int ComputeComponentSize(Dictionary<string, HashSet<string>> adjacency, string startId)
    {
        var visited = new HashSet<string> { startId };
        var stack = new Stack<string>();
        stack.Push(startId);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!adjacency.TryGetValue(current, out var neighbors)) continue;

            foreach (var neighbor in neighbors)
            {
                if (visited.Add(neighbor))
                    stack.Push(neighbor);
            }
        }

        return visited.Count;
    }
}
