namespace TheCanonry.Engine.Rules;

using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

/// <summary>
/// Static evaluator for all SelectionFilter subtypes.
/// Filters determine whether entities should be included in selection results.
/// </summary>
public static class FilterEvaluator
{
    /// <summary>Check if a single entity passes a filter.</summary>
    public static bool EntityPassesFilter(Entity entity, SelectionFilter filter, RuleContext context) =>
        ApplyFilter([entity], filter, context).Count > 0;

    /// <summary>Check if a single entity passes all filters.</summary>
    public static bool EntityPassesAllFilters(
        Entity entity,
        IReadOnlyList<SelectionFilter> filters,
        RuleContext context)
    {
        foreach (var filter in filters)
        {
            if (!EntityPassesFilter(entity, filter, context))
                return false;
        }
        return true;
    }

    /// <summary>Apply all filters to a list of entities (AND logic).</summary>
    public static List<Entity> ApplyFilters(
        IReadOnlyList<Entity> entities,
        IReadOnlyList<SelectionFilter> filters,
        RuleContext context)
    {
        var result = new List<Entity>(entities);

        foreach (var filter in filters)
        {
            result = ApplyFilter(result, filter, context);
            if (result.Count == 0) break;
        }

        return result;
    }

    /// <summary>Apply a single filter to a list of entities.</summary>
    public static List<Entity> ApplyFilter(
        IReadOnlyList<Entity> entities,
        SelectionFilter filter,
        RuleContext context) => filter switch
    {
        ExcludeEntitiesFilter f => FilterExclude(entities, f, context),
        HasRelationshipFilter f => FilterByRelationship(entities, f, context, negate: false),
        LacksRelationshipFilter f => FilterByLacksRelationship(entities, f, context),
        SharesRelatedFilter f => FilterBySharesRelated(entities, f, context),
        HasTagFilter f => FilterByHasTag(entities, f),
        HasTagsFilter f => FilterByHasTags(entities, f),
        HasAnyTagFilter f => FilterByHasAnyTag(entities, f),
        LacksTagFilter f => FilterByLacksTag(entities, f),
        LacksAnyTagFilter f => FilterByLacksAnyTag(entities, f),
        HasCultureFilter f => FilterByCulture(entities, f.Culture, match: true),
        NotHasCultureFilter f => FilterByCulture(entities, f.Culture, match: false),
        MatchesCultureFilter f => FilterByMatchesCulture(entities, f.With, context, negate: false),
        NotMatchesCultureFilter f => FilterByMatchesCulture(entities, f.With, context, negate: true),
        HasStatusFilter f => FilterByStatus(entities, f),
        HasProminenceFilter f => FilterByProminence(entities, f),
        GraphPathFilter => new List<Entity>(entities), // Graph path evaluation is complex; stub
        ComponentSizeFilter f => FilterByComponentSize(entities, f, context),
        _ => new List<Entity>(entities)
    };

    // =========================================================================
    // EXCLUDE
    // =========================================================================

    private static List<Entity> FilterExclude(
        IReadOnlyList<Entity> entities, ExcludeEntitiesFilter f, RuleContext ctx)
    {
        var excludeIds = new HashSet<EntityId>();
        foreach (var reference in f.Entities)
        {
            var resolved = ctx.Resolver.ResolveEntity(reference);
            if (resolved is not null)
                excludeIds.Add(resolved.Id);
        }

        var result = new List<Entity>();
        foreach (var e in entities)
        {
            if (!excludeIds.Contains(e.Id))
                result.Add(e);
        }
        return result;
    }

    // =========================================================================
    // RELATIONSHIP
    // =========================================================================

    private static List<Entity> FilterByRelationship(
        IReadOnlyList<Entity> entities, HasRelationshipFilter f, RuleContext ctx, bool negate)
    {
        var withEntityId = string.IsNullOrEmpty(f.With) ? (EntityId?)null : ctx.Resolver.ResolveEntity(f.With)?.Id;
        var relKind = new RelationshipKind(f.Kind);

        var result = new List<Entity>();
        foreach (var entity in entities)
        {
            var relationships = ctx.Graph.GetEntityRelationships(entity.Id);
            var hasMatch = false;

            foreach (var link in relationships)
            {
                if (link.Kind != relKind) continue;

                var matchesDirection = f.Direction switch
                {
                    Direction.Source => link.SourceId == entity.Id,
                    Direction.Target => link.TargetId == entity.Id,
                    Direction.Both => link.SourceId == entity.Id || link.TargetId == entity.Id,
                    _ => false
                };

                if (!matchesDirection) continue;

                if (withEntityId is not null)
                {
                    var otherId = link.SourceId == entity.Id ? link.TargetId : link.SourceId;
                    if (otherId != withEntityId.Value) continue;
                }

                hasMatch = true;
                break;
            }

            if (negate ? !hasMatch : hasMatch)
                result.Add(entity);
        }
        return result;
    }

    private static List<Entity> FilterByLacksRelationship(
        IReadOnlyList<Entity> entities, LacksRelationshipFilter f, RuleContext ctx)
    {
        var withEntityId = string.IsNullOrEmpty(f.With) ? (EntityId?)null : ctx.Resolver.ResolveEntity(f.With)?.Id;
        var relKind = new RelationshipKind(f.Kind);

        var result = new List<Entity>();
        foreach (var entity in entities)
        {
            var relationships = ctx.Graph.GetEntityRelationships(entity.Id);
            var hasMatch = false;

            foreach (var link in relationships)
            {
                if (link.Kind != relKind) continue;

                if (withEntityId is not null)
                {
                    var otherId = link.SourceId == entity.Id ? link.TargetId : link.SourceId;
                    if (otherId != withEntityId.Value) continue;
                }

                hasMatch = true;
                break;
            }

            if (!hasMatch)
                result.Add(entity);
        }
        return result;
    }

    private static List<Entity> FilterBySharesRelated(
        IReadOnlyList<Entity> entities, SharesRelatedFilter f, RuleContext ctx)
    {
        var refEntity = ctx.Resolver.ResolveEntity(f.With);
        if (refEntity is null) return new List<Entity>(entities);

        var relKind = new RelationshipKind(f.RelationshipKind);
        var refRelated = ctx.Graph.GetConnectedEntities(refEntity.Id, relKind);
        if (refRelated.Count == 0) return [];

        var refRelatedIds = new HashSet<EntityId>();
        foreach (var r in refRelated)
            refRelatedIds.Add(r.Id);

        var result = new List<Entity>();
        foreach (var entity in entities)
        {
            var entityRelated = ctx.Graph.GetConnectedEntities(entity.Id, relKind);
            var shares = false;
            foreach (var r in entityRelated)
            {
                if (refRelatedIds.Contains(r.Id))
                {
                    shares = true;
                    break;
                }
            }
            if (shares) result.Add(entity);
        }
        return result;
    }

    // =========================================================================
    // TAG
    // =========================================================================

    private static List<Entity> FilterByHasTag(IReadOnlyList<Entity> entities, HasTagFilter f)
    {
        var result = new List<Entity>();
        foreach (var e in entities)
        {
            if (!e.Tags.Contains(f.Tag)) continue;

            if (!string.IsNullOrEmpty(f.Value))
            {
                var strVal = e.Tags.GetString(f.Tag);
                if (strVal is not null && strVal == f.Value) { result.Add(e); continue; }
                if (f.Value == "true" && e.Tags.GetBool(f.Tag)) { result.Add(e); continue; }
                continue;
            }

            result.Add(e);
        }
        return result;
    }

    private static List<Entity> FilterByHasTags(IReadOnlyList<Entity> entities, HasTagsFilter f)
    {
        if (f.Tags.Count == 0) return new List<Entity>(entities);

        var result = new List<Entity>();
        foreach (var e in entities)
        {
            var hasAll = true;
            foreach (var tag in f.Tags)
            {
                if (!e.Tags.Contains(tag)) { hasAll = false; break; }
            }
            if (hasAll) result.Add(e);
        }
        return result;
    }

    private static List<Entity> FilterByHasAnyTag(IReadOnlyList<Entity> entities, HasAnyTagFilter f)
    {
        if (f.Tags.Count == 0) return new List<Entity>(entities);

        var result = new List<Entity>();
        foreach (var e in entities)
        {
            foreach (var tag in f.Tags)
            {
                if (e.Tags.Contains(tag)) { result.Add(e); break; }
            }
        }
        return result;
    }

    private static List<Entity> FilterByLacksTag(IReadOnlyList<Entity> entities, LacksTagFilter f)
    {
        var result = new List<Entity>();
        foreach (var e in entities)
        {
            if (!e.Tags.Contains(f.Tag)) { result.Add(e); continue; }

            // If a specific value is set, only exclude if tag matches that value
            if (!string.IsNullOrEmpty(f.Value))
            {
                var strVal = e.Tags.GetString(f.Tag);
                if (strVal is not null && strVal != f.Value) { result.Add(e); continue; }
                if (f.Value == "true" && !e.Tags.GetBool(f.Tag)) { result.Add(e); continue; }
                continue;
            }
        }
        return result;
    }

    private static List<Entity> FilterByLacksAnyTag(IReadOnlyList<Entity> entities, LacksAnyTagFilter f)
    {
        if (f.Tags.Count == 0) return new List<Entity>(entities);

        var result = new List<Entity>();
        foreach (var e in entities)
        {
            var hasAny = false;
            foreach (var tag in f.Tags)
            {
                if (e.Tags.Contains(tag)) { hasAny = true; break; }
            }
            if (!hasAny) result.Add(e);
        }
        return result;
    }

    // =========================================================================
    // CULTURE
    // =========================================================================

    private static List<Entity> FilterByCulture(IReadOnlyList<Entity> entities, string culture, bool match)
    {
        var cultureId = new CultureId(culture);
        var result = new List<Entity>();
        foreach (var e in entities)
        {
            if (match ? e.Culture == cultureId : e.Culture != cultureId)
                result.Add(e);
        }
        return result;
    }

    private static List<Entity> FilterByMatchesCulture(
        IReadOnlyList<Entity> entities, string with, RuleContext ctx, bool negate)
    {
        var refEntity = ctx.Resolver.ResolveEntity(with);
        if (refEntity is null) return new List<Entity>(entities);

        var result = new List<Entity>();
        foreach (var e in entities)
        {
            var matches = e.Culture == refEntity.Culture;
            if (negate ? !matches : matches)
                result.Add(e);
        }
        return result;
    }

    // =========================================================================
    // STATUS/PROMINENCE
    // =========================================================================

    private static List<Entity> FilterByStatus(IReadOnlyList<Entity> entities, HasStatusFilter f)
    {
        var status = new EntityStatus(f.Status);
        var result = new List<Entity>();
        foreach (var e in entities)
        {
            if (e.Status == status)
                result.Add(e);
        }
        return result;
    }

    private static List<Entity> FilterByProminence(IReadOnlyList<Entity> entities, HasProminenceFilter f)
    {
        var threshold = ProminenceHelper.LabelToThreshold(f.MinProminence);
        var result = new List<Entity>();
        foreach (var e in entities)
        {
            if (e.Prominence.Value >= threshold)
                result.Add(e);
        }
        return result;
    }

    // =========================================================================
    // COMPONENT SIZE
    // =========================================================================

    private static List<Entity> FilterByComponentSize(
        IReadOnlyList<Entity> entities, ComponentSizeFilter f, RuleContext ctx)
    {
        var adjacency = BuildAdjacency(ctx.Graph, f.RelationshipKinds, f.MinStrength);

        var result = new List<Entity>();
        foreach (var entity in entities)
        {
            var size = ComputeComponentSize(adjacency, entity.Id.Value);
            if (size >= f.Min && size <= f.Max)
                result.Add(entity);
        }
        return result;
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
