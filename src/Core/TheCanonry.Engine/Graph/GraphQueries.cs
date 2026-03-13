using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

namespace TheCanonry.Engine.Graph;

/// <summary>
/// Static helper methods for common graph query patterns.
/// These wrap IGraph operations to reduce boilerplate in templates and systems.
/// </summary>
public static class GraphQueries
{
    /// <summary>
    /// Get all entities connected to a given entity by a specific relationship kind and direction.
    /// </summary>
    public static IReadOnlyList<Entity> GetEntitiesByRelationship(
        IGraph graph,
        EntityId entityId,
        RelationshipKind relationKind,
        Direction direction)
    {
        return graph.GetConnectedEntities(entityId, relationKind, direction);
    }

    /// <summary>
    /// Count relationships of a specific kind for an entity in a given direction.
    /// </summary>
    public static int CountRelationships(
        IGraph graph,
        EntityId entityId,
        RelationshipKind relationKind,
        Direction direction)
    {
        var count = 0;
        foreach (var rel in graph.GetRelationships())
        {
            if (rel.Kind != relationKind)
                continue;

            var matches = direction switch
            {
                Direction.Source => rel.SourceId == entityId,
                Direction.Target => rel.TargetId == entityId,
                Direction.Both => rel.SourceId == entityId || rel.TargetId == entityId,
                _ => false
            };

            if (matches) count++;
        }
        return count;
    }

    /// <summary>
    /// Find the first relationship matching the given entity, kind, and direction.
    /// Returns null if no match is found.
    /// </summary>
    public static Relationship? FindRelationship(
        IGraph graph,
        EntityId entityId,
        RelationshipKind relationKind,
        Direction direction)
    {
        foreach (var rel in graph.GetRelationships())
        {
            if (rel.Kind != relationKind)
                continue;

            var matches = direction switch
            {
                Direction.Source => rel.SourceId == entityId,
                Direction.Target => rel.TargetId == entityId,
                Direction.Both => rel.SourceId == entityId || rel.TargetId == entityId,
                _ => false
            };

            if (matches) return rel;
        }
        return null;
    }

    /// <summary>
    /// Given a relationship and a known entity ID, return the entity at the other end.
    /// Returns null if the relationship is null or the other entity doesn't exist.
    /// </summary>
    public static Entity? GetRelatedEntity(
        IGraph graph,
        Relationship? relationship,
        EntityId fromEntityId)
    {
        if (relationship is null) return null;

        var targetId = relationship.SourceId == fromEntityId
            ? relationship.TargetId
            : relationship.SourceId;

        return graph.GetEntity(targetId);
    }
}
