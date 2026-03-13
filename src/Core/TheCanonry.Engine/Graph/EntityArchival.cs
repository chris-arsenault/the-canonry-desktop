using TheCanonry.Schema.Ids;
using TheCanonry.Schema.Primitives;
using TheCanonry.Schema.World;
using ExecutionContext = TheCanonry.Schema.World.ExecutionContext;

namespace TheCanonry.Engine.Graph;

/// <summary>
/// Static helper methods for archiving entities and transferring relationships.
/// Used by simulation systems that consolidate or supersede entities.
/// </summary>
public static class EntityArchival
{
    /// <summary>
    /// Mark an entity as historical and optionally archive its relationships.
    /// </summary>
    public static void ArchiveEntity(IGraph graph, EntityId entityId, bool archiveRelationships = true)
    {
        var entity = graph.GetEntity(entityId);
        if (entity is null) return;

        entity.UpdateStatus(FrameworkPrimitives.Statuses.Historical, graph.Tick);

        if (archiveRelationships)
        {
            var relationships = graph.GetEntityRelationships(entityId);
            foreach (var rel in relationships)
            {
                rel.Archive(graph.Tick);
            }
        }
    }

    /// <summary>
    /// Archive multiple entities at once.
    /// </summary>
    public static void ArchiveEntities(
        IGraph graph,
        IEnumerable<EntityId> entityIds,
        bool archiveRelationships = true)
    {
        foreach (var id in entityIds)
        {
            ArchiveEntity(graph, id, archiveRelationships);
        }
    }

    /// <summary>
    /// Transfer relationships from source entities to a target entity.
    /// Creates new relationships pointing to the target and archives the originals.
    /// Returns the number of relationships transferred.
    /// </summary>
    public static int TransferRelationships(
        IGraph graph,
        IEnumerable<EntityId> sourceIds,
        EntityId targetId)
    {
        var sourceIdSet = new HashSet<EntityId>(sourceIds);
        var transferred = new HashSet<string>();
        var toTransfer = new List<Relationship>();

        // Collect relationships involving source entities (active only)
        foreach (var rel in graph.GetRelationships())
        {
            if (sourceIdSet.Contains(rel.SourceId) || sourceIdSet.Contains(rel.TargetId))
                toTransfer.Add(rel);
        }

        foreach (var rel in toTransfer)
        {
            var newSrcId = sourceIdSet.Contains(rel.SourceId) ? targetId : rel.SourceId;
            var newDstId = sourceIdSet.Contains(rel.TargetId) ? targetId : rel.TargetId;

            // Skip if no change
            if (newSrcId == rel.SourceId && newDstId == rel.TargetId) continue;

            // Archive the original
            rel.Archive(graph.Tick);

            // Don't create self-referential relationships
            if (newSrcId == newDstId) continue;

            // Avoid duplicates
            var key = $"{newSrcId}:{newDstId}:{rel.Kind}";
            if (!transferred.Add(key)) continue;

            // Create new relationship pointing to the target
            var newRel = new Relationship(
                newSrcId, newDstId, rel.Kind,
                rel.Strength, rel.Distance, rel.Category,
                rel.CreatedBy, graph.Tick);

            graph.AddRelationship(newRel);
        }

        return transferred.Count;
    }

    /// <summary>
    /// Create part_of relationships from member entities to a container entity.
    /// Skips members that already have a part_of relationship to the container.
    /// Returns the number of relationships created.
    /// </summary>
    public static int CreatePartOfRelationships(
        IGraph graph,
        IEnumerable<EntityId> memberIds,
        EntityId containerId)
    {
        var created = 0;

        foreach (var memberId in memberIds)
        {
            if (graph.HasRelationship(memberId, containerId, FrameworkPrimitives.RelationshipKinds.PartOf))
                continue;

            var rel = new Relationship(
                memberId, containerId,
                FrameworkPrimitives.RelationshipKinds.PartOf,
                FrameworkPrimitives.GetDefaultRelationshipStrength(FrameworkPrimitives.RelationshipKinds.PartOf),
                0.0, "",
                new ExecutionContext(graph.Tick, ExecutionSource.Framework, "archival", true, ""),
                graph.Tick);

            if (graph.AddRelationship(rel))
                created++;
        }

        return created;
    }
}
