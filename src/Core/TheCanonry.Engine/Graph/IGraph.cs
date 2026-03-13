using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

namespace TheCanonry.Engine.Graph;

/// <summary>
/// Core graph interface defining all entity and relationship operations.
/// This is the primary abstraction for the simulation engine's world state.
/// </summary>
public interface IGraph
{
    // =========================================================================
    // ENTITY READ METHODS
    // =========================================================================

    /// <summary>Get an entity by ID, or null if not found.</summary>
    Entity? GetEntity(EntityId id);

    /// <summary>Check whether an entity exists in the graph.</summary>
    bool HasEntity(EntityId id);

    /// <summary>Count entities. Excludes historical by default.</summary>
    int GetEntityCount(bool includeHistorical = false);

    /// <summary>Get all entities. Excludes historical by default.</summary>
    IReadOnlyList<Entity> GetEntities(bool includeHistorical = false);

    /// <summary>Find entities matching all non-null criteria fields (AND logic).</summary>
    IReadOnlyList<Entity> FindEntities(EntityCriteria criteria);

    /// <summary>Get all entities of a specific kind. Excludes historical by default.</summary>
    IReadOnlyList<Entity> GetEntitiesByKind(EntityKind kind, bool includeHistorical = false);

    /// <summary>
    /// Get entities connected to the given entity via relationships.
    /// Optionally filter by relationship kind and direction.
    /// Excludes historical entities and relationships by default.
    /// </summary>
    IReadOnlyList<Entity> GetConnectedEntities(
        EntityId entityId,
        RelationshipKind? relationKind = null,
        Direction direction = Direction.Both,
        bool includeHistorical = false);

    // =========================================================================
    // ENTITY MUTATION METHODS
    // =========================================================================

    /// <summary>
    /// Add an entity to the graph. The entity must have a unique ID.
    /// Returns the entity's ID.
    /// </summary>
    EntityId CreateEntity(Entity entity);

    /// <summary>
    /// Apply a mutation to an existing entity via a callback.
    /// Returns true if the entity was found and mutated.
    /// </summary>
    bool UpdateEntity(EntityId id, Action<Entity> mutator);

    /// <summary>
    /// Remove an entity from the graph.
    /// Returns true if the entity existed and was removed.
    /// </summary>
    bool DeleteEntity(EntityId id);

    // =========================================================================
    // RELATIONSHIP READ METHODS
    // =========================================================================

    /// <summary>Get all relationships. Excludes historical by default.</summary>
    IReadOnlyList<Relationship> GetRelationships(bool includeHistorical = false);

    /// <summary>Find relationships matching all non-null criteria fields (AND logic).</summary>
    IReadOnlyList<Relationship> FindRelationships(RelationshipCriteria criteria);

    /// <summary>
    /// Get relationships involving a specific entity.
    /// Direction controls whether the entity must be source, target, or either.
    /// Excludes historical by default.
    /// </summary>
    IReadOnlyList<Relationship> GetEntityRelationships(
        EntityId entityId,
        Direction direction = Direction.Both,
        bool includeHistorical = false);

    /// <summary>
    /// Check whether an active relationship exists between two entities.
    /// Optionally filter by relationship kind.
    /// </summary>
    bool HasRelationship(EntityId srcId, EntityId dstId, RelationshipKind? kind = null);

    // =========================================================================
    // RELATIONSHIP MUTATION METHODS
    // =========================================================================

    /// <summary>
    /// Add a relationship to the graph.
    /// Returns true if added, false if duplicate or invalid (entities not found).
    /// </summary>
    bool AddRelationship(Relationship relationship);

    /// <summary>
    /// Remove a specific relationship by source, target, and kind.
    /// Returns true if found and removed.
    /// </summary>
    bool RemoveRelationship(EntityId srcId, EntityId dstId, RelationshipKind kind);

    // =========================================================================
    // GRAPH STATE
    // =========================================================================

    /// <summary>Current simulation tick.</summary>
    int Tick { get; set; }

    /// <summary>Pressure values keyed by pressure ID.</summary>
    Dictionary<string, double> Pressures { get; }
}
