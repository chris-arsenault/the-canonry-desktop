namespace TheCanonry.Engine.Graph;

using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.Primitives;
using TheCanonry.Schema.World;

/// <summary>
/// In-memory implementation of IGraph.
/// Stores entities in a dictionary keyed by EntityId and relationships in a list.
/// All query methods exclude historical entities/relationships by default.
/// </summary>
public sealed class GraphStore : IGraph
{
    private readonly Dictionary<EntityId, Entity> _entities = [];
    private readonly List<Relationship> _relationships = [];

    public int Tick { get; set; }
    public Dictionary<string, double> Pressures { get; } = [];

    // =========================================================================
    // ENTITY READ METHODS
    // =========================================================================

    public Entity? GetEntity(EntityId id)
    {
        return _entities.GetValueOrDefault(id);
    }

    public bool HasEntity(EntityId id)
    {
        return _entities.ContainsKey(id);
    }

    public int GetEntityCount(bool includeHistorical = false)
    {
        if (includeHistorical)
            return _entities.Count;

        var count = 0;
        foreach (var entity in _entities.Values)
        {
            if (entity.Status != FrameworkPrimitives.Statuses.Historical)
                count++;
        }
        return count;
    }

    public IReadOnlyList<Entity> GetEntities(bool includeHistorical = false)
    {
        var results = new List<Entity>();
        foreach (var entity in _entities.Values)
        {
            if (!includeHistorical && entity.Status == FrameworkPrimitives.Statuses.Historical)
                continue;
            results.Add(entity);
        }
        return results;
    }

    public IReadOnlyList<Entity> FindEntities(EntityCriteria criteria)
    {
        var results = new List<Entity>();
        foreach (var (id, entity) in _entities)
        {
            if (EntityMatchesCriteria(id, entity, criteria))
                results.Add(entity);
        }
        return results;
    }

    public IReadOnlyList<Entity> GetEntitiesByKind(EntityKind kind, bool includeHistorical = false)
    {
        return FindEntities(EntityCriteria.Create(kind: kind, includeHistorical: includeHistorical));
    }

    public IReadOnlyList<Entity> GetConnectedEntities(
        EntityId entityId,
        RelationshipKind? relationKind = null,
        Direction direction = Direction.Both,
        bool includeHistorical = false)
    {
        var connectedIds = CollectConnectedIds(entityId, relationKind, direction, includeHistorical);

        var results = new List<Entity>();
        foreach (var connectedId in connectedIds)
        {
            var entity = GetEntity(connectedId);
            if (entity is null) continue;
            if (!includeHistorical && entity.Status == FrameworkPrimitives.Statuses.Historical) continue;
            results.Add(entity);
        }
        return results;
    }

    // =========================================================================
    // ENTITY MUTATION METHODS
    // =========================================================================

    public EntityId CreateEntity(Entity entity)
    {
        if (_entities.ContainsKey(entity.Id))
            throw new ArgumentException($"Entity with ID '{entity.Id}' already exists.");

        _entities[entity.Id] = entity;
        return entity.Id;
    }

    public bool UpdateEntity(EntityId id, Action<Entity> mutator)
    {
        if (!_entities.TryGetValue(id, out var entity))
            return false;

        mutator(entity);
        return true;
    }

    public bool DeleteEntity(EntityId id)
    {
        return _entities.Remove(id);
    }

    // =========================================================================
    // RELATIONSHIP READ METHODS
    // =========================================================================

    public IReadOnlyList<Relationship> GetRelationships(bool includeHistorical = false)
    {
        if (includeHistorical)
            return _relationships.ToList();

        var results = new List<Relationship>();
        foreach (var rel in _relationships)
        {
            if (rel.Status != FrameworkPrimitives.Statuses.Historical)
                results.Add(rel);
        }
        return results;
    }

    public IReadOnlyList<Relationship> FindRelationships(RelationshipCriteria criteria)
    {
        var results = new List<Relationship>();
        foreach (var rel in _relationships)
        {
            if (RelationshipMatchesCriteria(rel, criteria))
                results.Add(rel);
        }
        return results;
    }

    public IReadOnlyList<Relationship> GetEntityRelationships(
        EntityId entityId,
        Direction direction = Direction.Both,
        bool includeHistorical = false)
    {
        var results = new List<Relationship>();
        foreach (var rel in _relationships)
        {
            if (!includeHistorical && rel.Status == FrameworkPrimitives.Statuses.Historical)
                continue;

            var matches = direction switch
            {
                Direction.Source => rel.SourceId == entityId,
                Direction.Target => rel.TargetId == entityId,
                Direction.Both => rel.SourceId == entityId || rel.TargetId == entityId,
                _ => false
            };

            if (matches)
                results.Add(rel);
        }
        return results;
    }

    public bool HasRelationship(EntityId srcId, EntityId dstId, RelationshipKind? kind = null)
    {
        foreach (var rel in _relationships)
        {
            if (rel.Status == FrameworkPrimitives.Statuses.Historical)
                continue;
            if (rel.SourceId == srcId && rel.TargetId == dstId)
            {
                if (kind is null || rel.Kind == kind.Value)
                    return true;
            }
        }
        return false;
    }

    // =========================================================================
    // RELATIONSHIP MUTATION METHODS
    // =========================================================================

    public bool AddRelationship(Relationship relationship)
    {
        // Validate that both entities exist
        if (!_entities.ContainsKey(relationship.SourceId) || !_entities.ContainsKey(relationship.TargetId))
            return false;

        // Check for duplicates (same src, dst, kind)
        foreach (var existing in _relationships)
        {
            if (existing.SourceId == relationship.SourceId &&
                existing.TargetId == relationship.TargetId &&
                existing.Kind == relationship.Kind)
                return false;
        }

        _relationships.Add(relationship);
        return true;
    }

    public bool RemoveRelationship(EntityId srcId, EntityId dstId, RelationshipKind kind)
    {
        for (var i = 0; i < _relationships.Count; i++)
        {
            var rel = _relationships[i];
            if (rel.SourceId == srcId && rel.TargetId == dstId && rel.Kind == kind)
            {
                _relationships.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

    private static bool EntityMatchesCriteria(EntityId id, Entity entity, EntityCriteria criteria)
    {
        if (!criteria.IncludeHistorical && entity.Status == FrameworkPrimitives.Statuses.Historical)
            return false;

        if (criteria.Exclude is not null)
        {
            foreach (var excludeId in criteria.Exclude)
            {
                if (excludeId == id)
                    return false;
            }
        }

        if (criteria.Kind is not null && entity.Kind != criteria.Kind.Value)
            return false;

        if (criteria.Subtype is not null && entity.Subtype != criteria.Subtype)
            return false;

        if (criteria.Status is not null && entity.Status != criteria.Status.Value)
            return false;

        if (criteria.ProminenceLabel is not null && entity.Prominence.Label != criteria.ProminenceLabel)
            return false;

        if (criteria.Culture is not null && entity.Culture != criteria.Culture.Value)
            return false;

        if (criteria.Tag is not null && !entity.Tags.Contains(criteria.Tag))
            return false;

        return true;
    }

    private static bool RelationshipMatchesCriteria(Relationship rel, RelationshipCriteria criteria)
    {
        if (!criteria.IncludeHistorical && rel.Status == FrameworkPrimitives.Statuses.Historical)
            return false;

        if (criteria.Kind is not null && rel.Kind != criteria.Kind.Value)
            return false;

        if (criteria.Src is not null && rel.SourceId != criteria.Src.Value)
            return false;

        if (criteria.Dst is not null && rel.TargetId != criteria.Dst.Value)
            return false;

        if (criteria.Category is not null && rel.Category != criteria.Category)
            return false;

        if (rel.Strength < criteria.MinStrength)
            return false;

        return true;
    }

    private HashSet<EntityId> CollectConnectedIds(
        EntityId entityId,
        RelationshipKind? relationKind,
        Direction direction,
        bool includeHistorical)
    {
        var connectedIds = new HashSet<EntityId>();

        foreach (var rel in _relationships)
        {
            if (!includeHistorical && rel.Status == FrameworkPrimitives.Statuses.Historical)
                continue;

            if (relationKind is not null && rel.Kind != relationKind.Value)
                continue;

            if ((direction == Direction.Source || direction == Direction.Both) && rel.SourceId == entityId)
                connectedIds.Add(rel.TargetId);

            if ((direction == Direction.Target || direction == Direction.Both) && rel.TargetId == entityId)
                connectedIds.Add(rel.SourceId);
        }

        return connectedIds;
    }
}
