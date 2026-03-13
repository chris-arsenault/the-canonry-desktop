using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.Primitives;
using TheCanonry.Schema.World;
using ExecutionContext = TheCanonry.Schema.World.ExecutionContext;

namespace TheCanonry.Engine.Rules;

/// <summary>
/// Static applier for all Mutation subtypes.
/// Mutations directly modify graph state through the IGraph interface.
/// </summary>
public static class MutationApplier
{
    /// <summary>
    /// Apply a mutation to the graph using the given context.
    /// Returns true if the mutation was applied, false if skipped (e.g., entity not found).
    /// </summary>
    public static bool Apply(Mutation mutation, RuleContext context) => mutation switch
    {
        SetTagMutation m => ApplySetTag(m, context),
        RemoveTagMutation m => ApplyRemoveTag(m, context),
        CreateRelationshipMutation m => ApplyCreateRelationship(m, context),
        ArchiveRelationshipMutation m => ApplyArchiveRelationship(m, context),
        ArchiveAllRelationshipsMutation m => ApplyArchiveAllRelationships(m, context),
        AdjustRelationshipStrengthMutation m => ApplyAdjustRelationshipStrength(m, context),
        TransferRelationshipMutation m => ApplyTransferRelationship(m, context),
        ChangeStatusMutation m => ApplyChangeStatus(m, context),
        AdjustProminenceMutation m => ApplyAdjustProminence(m, context),
        ModifyPressureMutation m => ApplyModifyPressure(m, context),
        UpdateRateLimitMutation => true, // Rate limit state not yet modeled
        ForEachRelatedAction m => ApplyForEachRelated(m, context),
        ConditionalAction m => ApplyConditional(m, context),
        _ => false
    };

    // =========================================================================
    // TAG MUTATIONS
    // =========================================================================

    private static bool ApplySetTag(SetTagMutation m, RuleContext ctx)
    {
        if (string.IsNullOrWhiteSpace(m.Tag)) return false;

        var entity = ConditionEvaluator.ResolveEntityRef(m.Entity, ctx, ctx.Self);
        if (entity is null) return false;

        var value = m.Value;

        // If valueFrom is specified, look up the value from context
        if (!string.IsNullOrEmpty(m.ValueFrom))
        {
            if (!ctx.Values.TryGetValue(m.ValueFrom, out var sourceValue))
                return false;
            if (sourceValue is string s) value = s;
            else if (sourceValue is bool b) { entity.Tags.Set(m.Tag, b); return true; }
            else return false;
        }

        if (value == "true" || value == "false")
        {
            entity.Tags.Set(m.Tag, value == "true");
        }
        else if (string.IsNullOrEmpty(value))
        {
            entity.Tags.Set(m.Tag, true);
        }
        else
        {
            entity.Tags.Set(m.Tag, value);
        }

        return true;
    }

    private static bool ApplyRemoveTag(RemoveTagMutation m, RuleContext ctx)
    {
        var entity = ConditionEvaluator.ResolveEntityRef(m.Entity, ctx, ctx.Self);
        if (entity is null) return false;

        entity.Tags.Remove(m.Tag);
        return true;
    }

    // =========================================================================
    // RELATIONSHIP MUTATIONS
    // =========================================================================

    private static bool ApplyCreateRelationship(CreateRelationshipMutation m, RuleContext ctx)
    {
        var src = ConditionEvaluator.ResolveEntityRef(m.Src, ctx, ctx.Self);
        var dst = ConditionEvaluator.ResolveEntityRef(m.Dst, ctx, ctx.Self);
        if (src is null || dst is null) return false;

        var execCtx = new ExecutionContext(ctx.Tick, ExecutionSource.System, "mutation", true, "");

        var rel = new Relationship(
            src.Id, dst.Id, new RelationshipKind(m.Kind),
            m.Strength, 0.0, m.Category, execCtx, ctx.Tick);

        ctx.Graph.AddRelationship(rel);

        if (m.Bidirectional)
        {
            var reverseRel = new Relationship(
                dst.Id, src.Id, new RelationshipKind(m.Kind),
                m.Strength, 0.0, m.Category, execCtx, ctx.Tick);
            ctx.Graph.AddRelationship(reverseRel);
        }

        return true;
    }

    private static bool ApplyArchiveRelationship(ArchiveRelationshipMutation m, RuleContext ctx)
    {
        var entity = ConditionEvaluator.ResolveEntityRef(m.Entity, ctx, ctx.Self);
        if (entity is null) return false;

        var withEntity = string.IsNullOrEmpty(m.With) || m.With == "any"
            ? null
            : ConditionEvaluator.ResolveEntityRef(m.With, ctx, ctx.Self);

        var relKind = new RelationshipKind(m.RelationshipKind);
        var archived = 0;

        foreach (var rel in ctx.Graph.GetRelationships())
        {
            if (rel.Kind != relKind) continue;

            var matchesDirection = m.Direction switch
            {
                Direction.Source => rel.SourceId == entity.Id,
                Direction.Target => rel.TargetId == entity.Id,
                Direction.Both => rel.SourceId == entity.Id || rel.TargetId == entity.Id,
                _ => false
            };
            if (!matchesDirection) continue;

            if (withEntity is not null)
            {
                var isPair = (rel.SourceId == entity.Id && rel.TargetId == withEntity.Id) ||
                             (rel.TargetId == entity.Id && rel.SourceId == withEntity.Id);
                if (!isPair) continue;
            }

            rel.Archive(ctx.Tick);
            archived++;
        }

        return archived > 0;
    }

    private static bool ApplyArchiveAllRelationships(ArchiveAllRelationshipsMutation m, RuleContext ctx)
    {
        // Delegate to ArchiveRelationshipMutation with no "with" constraint
        return ApplyArchiveRelationship(
            new ArchiveRelationshipMutation(m.Entity, m.RelationshipKind, "", m.Direction),
            ctx);
    }

    private static bool ApplyAdjustRelationshipStrength(AdjustRelationshipStrengthMutation m, RuleContext ctx)
    {
        var src = ConditionEvaluator.ResolveEntityRef(m.Src, ctx, ctx.Self);
        var dst = ConditionEvaluator.ResolveEntityRef(m.Dst, ctx, ctx.Self);
        if (src is null || dst is null) return false;

        var relKind = new RelationshipKind(m.Kind);
        var adjusted = false;

        foreach (var rel in ctx.Graph.GetRelationships())
        {
            if (rel.Kind != relKind) continue;

            if (rel.SourceId == src.Id && rel.TargetId == dst.Id)
            {
                rel.Reinforce(m.Delta); // Reinforce clamps to [0,1]
                adjusted = true;
            }

            if (m.Bidirectional && rel.SourceId == dst.Id && rel.TargetId == src.Id)
            {
                rel.Reinforce(m.Delta);
                adjusted = true;
            }
        }

        return adjusted;
    }

    private static bool ApplyTransferRelationship(TransferRelationshipMutation m, RuleContext ctx)
    {
        var entity = ConditionEvaluator.ResolveEntityRef(m.Entity, ctx, ctx.Self);
        var from = ConditionEvaluator.ResolveEntityRef(m.From, ctx, ctx.Self);
        var to = ConditionEvaluator.ResolveEntityRef(m.To, ctx, ctx.Self);
        if (entity is null || from is null || to is null) return false;

        // Check condition
        if (!ConditionEvaluator.Evaluate(m.Condition, ctx))
            return false;

        var relKind = new RelationshipKind(m.RelationshipKind);

        // Archive old relationship
        foreach (var rel in ctx.Graph.GetRelationships())
        {
            if (rel.Kind != relKind) continue;
            var isPair = (rel.SourceId == entity.Id && rel.TargetId == from.Id) ||
                         (rel.SourceId == from.Id && rel.TargetId == entity.Id);
            if (isPair)
            {
                rel.Archive(ctx.Tick);
                break;
            }
        }

        // Create new relationship
        var execCtx = new ExecutionContext(ctx.Tick, ExecutionSource.System, "transfer", true, "");
        var newRel = new Relationship(
            entity.Id, to.Id, relKind,
            0.8, 0.0, "", execCtx, ctx.Tick);
        ctx.Graph.AddRelationship(newRel);

        return true;
    }

    // =========================================================================
    // ENTITY MUTATIONS
    // =========================================================================

    private static bool ApplyChangeStatus(ChangeStatusMutation m, RuleContext ctx)
    {
        var entity = ConditionEvaluator.ResolveEntityRef(m.Entity, ctx, ctx.Self);
        if (entity is null) return false;

        entity.UpdateStatus(new EntityStatus(m.NewStatus), ctx.Tick);
        return true;
    }

    private static bool ApplyAdjustProminence(AdjustProminenceMutation m, RuleContext ctx)
    {
        var entity = ConditionEvaluator.ResolveEntityRef(m.Entity, ctx, ctx.Self);
        if (entity is null) return false;

        // Check prominence lock
        if (entity.Tags.Contains(FrameworkPrimitives.Tags.ProminenceLocked))
            return false;

        var currentValue = entity.Prominence.Value;
        var newValue = ProminenceHelper.Clamp(currentValue + m.Delta);

        if (Math.Abs(newValue - currentValue) < 1e-9) return false;

        entity.SetProminence(new Prominence(newValue), ctx.Tick);
        return true;
    }

    // =========================================================================
    // PRESSURE MUTATIONS
    // =========================================================================

    private static bool ApplyModifyPressure(ModifyPressureMutation m, RuleContext ctx)
    {
        if (!ctx.Graph.Pressures.TryGetValue(m.PressureId, out var current))
            current = 0.0;

        ctx.Graph.Pressures[m.PressureId] = current + m.Delta;
        return true;
    }

    // =========================================================================
    // COMPOUND ACTIONS
    // =========================================================================

    private static bool ApplyForEachRelated(ForEachRelatedAction m, RuleContext ctx)
    {
        if (ctx.Self is null) return false;

        var relKind = new RelationshipKind(m.Relationship);
        var selfId = ctx.Self.Id;

        // Find related entity IDs
        var relatedIds = new HashSet<EntityId>();
        foreach (var rel in ctx.Graph.GetRelationships())
        {
            if (rel.Kind != relKind) continue;

            var matchesDirection = m.Direction switch
            {
                Direction.Source => rel.SourceId == selfId,
                Direction.Target => rel.TargetId == selfId,
                Direction.Both => rel.SourceId == selfId || rel.TargetId == selfId,
                _ => false
            };

            if (!matchesDirection) continue;

            var relatedId = rel.SourceId == selfId ? rel.TargetId : rel.SourceId;
            relatedIds.Add(relatedId);
        }

        // Filter by kind/subtype and execute actions
        var applied = false;
        foreach (var id in relatedIds)
        {
            var entity = ctx.Graph.GetEntity(id);
            if (entity is null) continue;
            if (!string.IsNullOrEmpty(m.TargetKind) && entity.Kind != new EntityKind(m.TargetKind)) continue;
            if (!string.IsNullOrEmpty(m.TargetSubtype) && entity.Subtype != m.TargetSubtype) continue;

            var nestedEntities = new Dictionary<string, Entity>(
                ctx.Entities as IDictionary<string, Entity> ?? new Dictionary<string, Entity>())
            {
                ["related"] = entity
            };
            var nestedCtx = ctx.WithEntities(nestedEntities);

            foreach (var action in m.Actions)
            {
                if (Apply(action, nestedCtx))
                    applied = true;
            }
        }

        return applied;
    }

    private static bool ApplyConditional(ConditionalAction m, RuleContext ctx)
    {
        var conditionPassed = ConditionEvaluator.Evaluate(m.Condition, ctx);
        var actions = conditionPassed ? m.ThenActions : m.ElseActions;

        var applied = false;
        foreach (var action in actions)
        {
            if (Apply(action, ctx))
                applied = true;
        }

        return applied;
    }
}
