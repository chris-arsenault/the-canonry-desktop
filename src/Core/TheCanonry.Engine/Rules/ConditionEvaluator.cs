using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

namespace TheCanonry.Engine.Rules;

/// <summary>
/// Static evaluator for all Condition subtypes.
/// Returns true if the condition passes, false otherwise.
/// </summary>
public static class ConditionEvaluator
{
    /// <summary>
    /// Evaluate a condition against the given context.
    /// Uses the context's Self entity for per-entity conditions.
    /// </summary>
    public static bool Evaluate(Condition condition, RuleContext context) =>
        Evaluate(condition, context, context.Self);

    /// <summary>
    /// Evaluate a condition against the given context with an explicit entity.
    /// </summary>
    public static bool Evaluate(Condition condition, RuleContext context, Entity? self) => condition switch
    {
        PressureCondition c => EvaluatePressure(c, context),
        PressureCompareCondition c => EvaluatePressureCompare(c, context),
        PressureAnyAboveCondition c => EvaluatePressureAnyAbove(c, context),
        EntityCountCondition c => EvaluateEntityCount(c, context),
        RelationshipCountCondition c => EvaluateRelationshipCount(c, context, self),
        RelationshipExistsCondition c => EvaluateRelationshipExists(c, context, self),
        TagExistsCondition c => EvaluateTagExists(c, context, self),
        LacksTagCondition c => EvaluateLacksTag(c, context, self),
        StatusCondition c => EvaluateStatus(c, self),
        ProminenceCondition c => EvaluateProminence(c, self),
        TimeElapsedCondition c => EvaluateTimeElapsed(c, context, self),
        CooldownElapsedCondition => true, // Requires rate limit state not yet modeled
        CreationsPerEpochCondition => true, // Requires rate limit state not yet modeled
        GrowthPhasesCompleteCondition => true, // Requires growth phase history not yet modeled
        EraMatchCondition c => EvaluateEraMatch(c, context),
        RandomChanceCondition c => EvaluateRandomChance(c),
        GraphPathCondition => true, // Graph path evaluation is complex; stub for now
        ComponentSizeCondition c => EvaluateComponentSize(c, context, self),
        EntityExistsCondition c => EvaluateEntityExists(c, context, self),
        EntityHasRelationshipCondition c => EvaluateEntityHasRelationship(c, context),
        AndCondition c => EvaluateAnd(c, context, self),
        OrCondition c => EvaluateOr(c, context, self),
        AlwaysCondition => true,
        _ => false
    };

    // =========================================================================
    // PRESSURE
    // =========================================================================

    private static bool EvaluatePressure(PressureCondition c, RuleContext ctx)
    {
        var value = GetPressure(ctx, c.PressureId);
        return value >= c.Min && value <= c.Max;
    }

    private static bool EvaluatePressureCompare(PressureCompareCondition c, RuleContext ctx)
    {
        var valueA = GetPressure(ctx, c.PressureA);
        var valueB = GetPressure(ctx, c.PressureB);
        return c.Operator.Apply(valueA, valueB);
    }

    private static bool EvaluatePressureAnyAbove(PressureAnyAboveCondition c, RuleContext ctx)
    {
        foreach (var id in c.PressureIds)
        {
            if (GetPressure(ctx, id) > c.Threshold)
                return true;
        }
        return false;
    }

    // =========================================================================
    // ENTITY COUNT
    // =========================================================================

    private static bool EvaluateEntityCount(EntityCountCondition c, RuleContext ctx)
    {
        var entities = ctx.Graph.FindEntities(EntityCriteria.Create(
            kind: string.IsNullOrEmpty(c.Kind) ? null : new EntityKind(c.Kind),
            subtype: string.IsNullOrEmpty(c.Subtype) ? null : c.Subtype,
            status: string.IsNullOrEmpty(c.Status) ? null : new EntityStatus(c.Status)));

        var count = entities.Count;
        var effectiveMax = (int)Math.Floor(c.Max * c.OvershootFactor);
        return count >= c.Min && count < effectiveMax;
    }

    // =========================================================================
    // RELATIONSHIP
    // =========================================================================

    private static bool EvaluateRelationshipCount(RelationshipCountCondition c, RuleContext ctx, Entity? self)
    {
        if (self is null) return false;

        var count = 0;
        var relationships = ctx.Graph.GetRelationships();
        var hasKindFilter = !string.IsNullOrEmpty(c.RelationshipKind);
        var relKind = hasKindFilter ? new RelationshipKind(c.RelationshipKind) : default;

        foreach (var link in relationships)
        {
            if (hasKindFilter && link.Kind != relKind)
                continue;

            var involved = c.Direction switch
            {
                Direction.Both => link.SourceId == self.Id || link.TargetId == self.Id,
                Direction.Source => link.SourceId == self.Id,
                Direction.Target => link.TargetId == self.Id,
                _ => false
            };

            if (involved) count++;
        }

        return count >= c.Min && count <= c.Max;
    }

    private static bool EvaluateRelationshipExists(RelationshipExistsCondition c, RuleContext ctx, Entity? self)
    {
        if (self is null) return false;

        var withEntity = string.IsNullOrEmpty(c.With) ? null : ResolveEntityRef(c.With, ctx, self);
        var relationships = ctx.Graph.GetRelationships();

        foreach (var link in relationships)
        {
            if (link.Kind != new RelationshipKind(c.RelationshipKind))
                continue;

            var matchesDirection = c.Direction switch
            {
                Direction.Both => link.SourceId == self.Id || link.TargetId == self.Id,
                Direction.Source => link.SourceId == self.Id,
                Direction.Target => link.TargetId == self.Id,
                _ => false
            };

            if (!matchesDirection) continue;

            if (withEntity is not null)
            {
                var otherId = link.SourceId == self.Id ? link.TargetId : link.SourceId;
                if (otherId != withEntity.Id) continue;
            }

            // Check target criteria
            var otherId2 = link.SourceId == self.Id ? link.TargetId : link.SourceId;
            if (!MatchesTargetCriteria(otherId2, c, ctx))
                continue;

            return true;
        }
        return false;
    }

    private static bool MatchesTargetCriteria(EntityId otherId, RelationshipExistsCondition c, RuleContext ctx)
    {
        if (string.IsNullOrEmpty(c.TargetKind) && string.IsNullOrEmpty(c.TargetSubtype) && string.IsNullOrEmpty(c.TargetStatus))
            return true;

        var other = ctx.Graph.GetEntity(otherId);
        if (other is null) return false;

        if (!string.IsNullOrEmpty(c.TargetKind) && other.Kind != new EntityKind(c.TargetKind))
            return false;
        if (!string.IsNullOrEmpty(c.TargetSubtype) && other.Subtype != c.TargetSubtype)
            return false;
        if (!string.IsNullOrEmpty(c.TargetStatus) && other.Status != new EntityStatus(c.TargetStatus))
            return false;

        return true;
    }

    // =========================================================================
    // TAG
    // =========================================================================

    private static bool EvaluateTagExists(TagExistsCondition c, RuleContext ctx, Entity? self)
    {
        var entity = ResolveEntityRef(c.Entity, ctx, self);
        if (entity is null) return false;

        if (!entity.Tags.Contains(c.Tag)) return false;

        // If a specific value is required, check it
        if (!string.IsNullOrEmpty(c.Value))
        {
            var strVal = entity.Tags.GetString(c.Tag);
            if (strVal is not null) return strVal == c.Value;

            // Check bool value
            if (c.Value == "true") return entity.Tags.GetBool(c.Tag);
            if (c.Value == "false") return !entity.Tags.GetBool(c.Tag);
        }

        return true;
    }

    private static bool EvaluateLacksTag(LacksTagCondition c, RuleContext ctx, Entity? self)
    {
        var entity = ResolveEntityRef(c.Entity, ctx, self);
        if (entity is null) return true; // No entity means tag is trivially absent

        return !entity.Tags.Contains(c.Tag);
    }

    // =========================================================================
    // STATUS/PROMINENCE
    // =========================================================================

    private static bool EvaluateStatus(StatusCondition c, Entity? self)
    {
        if (self is null) return false;

        var matches = self.Status == new EntityStatus(c.Status);
        return c.Not ? !matches : matches;
    }

    private static bool EvaluateProminence(ProminenceCondition c, Entity? self)
    {
        if (self is null) return false;

        var currentValue = self.Prominence.Value;
        var minValue = ProminenceHelper.LabelToThreshold(c.MinLabel);
        var maxValue = ProminenceHelper.LabelToThreshold(c.MaxLabel) + 1.0;

        return currentValue >= minValue && currentValue < maxValue;
    }

    // =========================================================================
    // TIME
    // =========================================================================

    private static bool EvaluateTimeElapsed(TimeElapsedCondition c, RuleContext ctx, Entity? self)
    {
        if (self is null) return false;

        var timestamp = c.Since == "created" ? self.CreatedAtTick : self.UpdatedAtTick;
        var elapsed = ctx.Tick - timestamp;
        return elapsed >= c.MinTicks;
    }

    // =========================================================================
    // ERA
    // =========================================================================

    private static bool EvaluateEraMatch(EraMatchCondition c, RuleContext ctx)
    {
        return c.Eras.Contains(ctx.CurrentEraId);
    }

    // =========================================================================
    // PROBABILITY
    // =========================================================================

    private static bool EvaluateRandomChance(RandomChanceCondition c)
    {
        return Random.Shared.NextDouble() < c.Chance;
    }

    // =========================================================================
    // GRAPH TOPOLOGY
    // =========================================================================

    private static bool EvaluateComponentSize(ComponentSizeCondition c, RuleContext ctx, Entity? self)
    {
        if (self is null) return false;

        var adjacency = BuildComponentAdjacency(ctx.Graph, c.RelationshipKinds, c.MinStrength);
        var size = DfsComponentSize(self.Id.Value, adjacency);

        return size >= c.Min && size <= c.Max;
    }

    // =========================================================================
    // ENTITY EXISTENCE
    // =========================================================================

    private static bool EvaluateEntityExists(EntityExistsCondition c, RuleContext ctx, Entity? self)
    {
        var entity = ResolveEntityRef(c.Entity, ctx, self);
        return entity is not null;
    }

    private static bool EvaluateEntityHasRelationship(EntityHasRelationshipCondition c, RuleContext ctx)
    {
        var entity = ResolveEntityRef(c.Entity, ctx, ctx.Self);
        if (entity is null) return false;

        var relKind = new RelationshipKind(c.RelationshipKind);

        foreach (var link in ctx.Graph.GetRelationships())
        {
            if (link.Kind != relKind) continue;

            var matches = c.Direction switch
            {
                Direction.Both => link.SourceId == entity.Id || link.TargetId == entity.Id,
                Direction.Source => link.SourceId == entity.Id,
                Direction.Target => link.TargetId == entity.Id,
                _ => false
            };

            if (matches) return true;
        }

        return false;
    }

    // =========================================================================
    // COMPOSITE
    // =========================================================================

    private static bool EvaluateAnd(AndCondition c, RuleContext ctx, Entity? self)
    {
        foreach (var condition in c.Conditions)
        {
            if (!Evaluate(condition, ctx, self))
                return false;
        }
        return true;
    }

    private static bool EvaluateOr(OrCondition c, RuleContext ctx, Entity? self)
    {
        foreach (var condition in c.Conditions)
        {
            if (Evaluate(condition, ctx, self))
                return true;
        }
        return false;
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private static double GetPressure(RuleContext ctx, string pressureId)
    {
        return ctx.Graph.Pressures.TryGetValue(pressureId, out var value) ? value : 0.0;
    }

    internal static Entity? ResolveEntityRef(string? reference, RuleContext ctx, Entity? self)
    {
        if (string.IsNullOrEmpty(reference)) return self;

        if (reference == "$self") return self;

        if (reference.StartsWith('$'))
        {
            var name = reference[1..];
            if (ctx.Entities.TryGetValue(name, out var bound))
                return bound;
        }

        return ctx.Resolver.ResolveEntity(reference);
    }

    private static Dictionary<string, HashSet<string>> BuildComponentAdjacency(
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

    private static int DfsComponentSize(string startId, Dictionary<string, HashSet<string>> adjacency)
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
