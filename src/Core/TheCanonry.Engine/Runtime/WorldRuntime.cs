namespace TheCanonry.Engine.Runtime;

using TheCanonry.Engine.Coordinates;
using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Selection;
using TheCanonry.Engine.Statistics;
using TheCanonry.Schema.Config;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

/// <summary>
/// Runtime orchestration layer that combines graph data with selection,
/// placement, pressure, and population tracking services. All template
/// and simulation system code interacts with the world through this facade
/// rather than reaching into subsystems directly.
/// </summary>
public sealed class WorldRuntime
{
    private readonly IGraph _graph;
    private readonly TargetSelector _targetSelector;
    private readonly CoordinateContext _coordinateContext;
    private readonly PopulationTracker _populationTracker;
    private readonly EngineConfig _config;
    private readonly Random _random;

    private Era? _currentEra;

    public WorldRuntime(
        EngineConfig config,
        IGraph? graph = null,
        int? randomSeed = null)
    {
        _config = config;
        _graph = graph ?? new GraphStore();
        _targetSelector = new TargetSelector();
        _coordinateContext = new CoordinateContext(config.Schema);
        _populationTracker = new PopulationTracker(config.DistributionTargets);
        _random = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
    }

    // =========================================================================
    // CONFIGURATION ACCESS
    // =========================================================================

    /// <summary>The engine configuration used to initialize this runtime.</summary>
    public EngineConfig Config => _config;

    /// <summary>The domain schema from configuration.</summary>
    public DomainSchema Schema => _config.Schema;

    // =========================================================================
    // TICK / EPOCH TRACKING
    // =========================================================================

    /// <summary>Current simulation tick (delegates to graph).</summary>
    public int CurrentTick
    {
        get => _graph.Tick;
        set => _graph.Tick = value;
    }

    /// <summary>Current epoch, derived from tick and ticks-per-epoch config.</summary>
    public int CurrentEpoch =>
        _config.TicksPerEpoch > 0 ? CurrentTick / _config.TicksPerEpoch : 0;

    /// <summary>Advance the simulation tick by one.</summary>
    public void AdvanceTick() => _graph.Tick++;

    // =========================================================================
    // ERA MANAGEMENT
    // =========================================================================

    /// <summary>The currently active era.</summary>
    public Era? CurrentEra
    {
        get => _currentEra;
        set => _currentEra = value;
    }

    // =========================================================================
    // PRESSURE MANAGEMENT
    // =========================================================================

    /// <summary>Get the current value of a pressure, or 0 if not set.</summary>
    public double GetPressure(string pressureId)
    {
        return _graph.Pressures.TryGetValue(pressureId, out var value) ? value : 0.0;
    }

    /// <summary>Set a pressure to an exact value, clamped to [-100, 100].</summary>
    public void SetPressure(string pressureId, double value)
    {
        _graph.Pressures[pressureId] = Math.Clamp(value, -100.0, 100.0);
    }

    /// <summary>
    /// Modify a pressure by a delta, clamped to [-100, 100].
    /// </summary>
    public void ModifyPressure(string pressureId, double delta)
    {
        var current = GetPressure(pressureId);
        SetPressure(pressureId, current + delta);
    }

    /// <summary>Get all current pressure values (read-only view).</summary>
    public IReadOnlyDictionary<string, double> GetAllPressures() => _graph.Pressures;

    // =========================================================================
    // ENTITY READ OPERATIONS (delegates to IGraph)
    // =========================================================================

    /// <summary>Get an entity by ID, or null if not found.</summary>
    public Entity? GetEntity(EntityId id) => _graph.GetEntity(id);

    /// <summary>Check whether an entity exists.</summary>
    public bool HasEntity(EntityId id) => _graph.HasEntity(id);

    /// <summary>Count entities. Excludes historical by default.</summary>
    public int GetEntityCount(bool includeHistorical = false) =>
        _graph.GetEntityCount(includeHistorical);

    /// <summary>Get all entities. Excludes historical by default.</summary>
    public IReadOnlyList<Entity> GetEntities(bool includeHistorical = false) =>
        _graph.GetEntities(includeHistorical);

    /// <summary>Find entities matching criteria.</summary>
    public IReadOnlyList<Entity> FindEntities(EntityCriteria criteria) =>
        _graph.FindEntities(criteria);

    /// <summary>Get all entities of a specific kind.</summary>
    public IReadOnlyList<Entity> GetEntitiesByKind(EntityKind kind, bool includeHistorical = false) =>
        _graph.GetEntitiesByKind(kind, includeHistorical);

    /// <summary>Get entities connected via relationships.</summary>
    public IReadOnlyList<Entity> GetConnectedEntities(
        EntityId entityId,
        RelationshipKind? relationKind = null,
        Direction direction = Direction.Both,
        bool includeHistorical = false) =>
        _graph.GetConnectedEntities(entityId, relationKind, direction, includeHistorical);

    // =========================================================================
    // ENTITY MUTATION OPERATIONS (delegates to IGraph)
    // =========================================================================

    /// <summary>Add an entity to the graph. Returns the entity's ID.</summary>
    public EntityId CreateEntity(Entity entity) => _graph.CreateEntity(entity);

    /// <summary>Apply a mutation to an existing entity via a callback.</summary>
    public bool UpdateEntity(EntityId id, Action<Entity> mutator) =>
        _graph.UpdateEntity(id, mutator);

    /// <summary>Remove an entity from the graph.</summary>
    public bool DeleteEntity(EntityId id) => _graph.DeleteEntity(id);

    // =========================================================================
    // RELATIONSHIP READ OPERATIONS (delegates to IGraph)
    // =========================================================================

    /// <summary>Get all relationships. Excludes historical by default.</summary>
    public IReadOnlyList<Relationship> GetRelationships(bool includeHistorical = false) =>
        _graph.GetRelationships(includeHistorical);

    /// <summary>Find relationships matching criteria.</summary>
    public IReadOnlyList<Relationship> FindRelationships(RelationshipCriteria criteria) =>
        _graph.FindRelationships(criteria);

    /// <summary>Get relationships involving a specific entity.</summary>
    public IReadOnlyList<Relationship> GetEntityRelationships(
        EntityId entityId,
        Direction direction = Direction.Both,
        bool includeHistorical = false) =>
        _graph.GetEntityRelationships(entityId, direction, includeHistorical);

    /// <summary>Check if a relationship exists between two entities.</summary>
    public bool HasRelationship(EntityId srcId, EntityId dstId, RelationshipKind? kind = null) =>
        _graph.HasRelationship(srcId, dstId, kind);

    // =========================================================================
    // RELATIONSHIP MUTATION OPERATIONS (delegates to IGraph)
    // =========================================================================

    /// <summary>Add a relationship to the graph.</summary>
    public bool AddRelationship(Relationship relationship) =>
        _graph.AddRelationship(relationship);

    /// <summary>Remove a specific relationship.</summary>
    public bool RemoveRelationship(EntityId srcId, EntityId dstId, RelationshipKind kind) =>
        _graph.RemoveRelationship(srcId, dstId, kind);

    // =========================================================================
    // TARGET SELECTION (delegates to TargetSelector)
    // =========================================================================

    /// <summary>
    /// Select entities using weighted scoring with hub penalties.
    /// This is the recommended way to select entities for connections.
    /// </summary>
    public SelectionResult SelectTargets(EntityKind kind, int count, SelectionBias? bias = null) =>
        _targetSelector.SelectTargets(_graph, kind, count, bias);

    // =========================================================================
    // COORDINATE PLACEMENT (delegates to CoordinateContext)
    // =========================================================================

    /// <summary>
    /// Place an entity on its kind's semantic plane using culture-aware strategy.
    /// </summary>
    public PlacementResult PlaceEntity(
        string entityKind,
        string cultureId,
        IReadOnlyList<Entity>? referenceEntities = null,
        PlacementOptions? options = null) =>
        _coordinateContext.PlaceEntity(entityKind, cultureId, referenceEntities, options);

    /// <summary>Find the primary region containing a point.</summary>
    public SemanticRegion? LookupRegion(string entityKind, SemanticCoordinates point) =>
        _coordinateContext.LookupRegion(entityKind, point);

    /// <summary>Get all regions for an entity kind.</summary>
    public IReadOnlyList<SemanticRegion> GetRegions(string entityKind) =>
        _coordinateContext.GetRegions(entityKind);

    /// <summary>Euclidean distance between two 3D semantic coordinates.</summary>
    public static double GetDistance(SemanticCoordinates a, SemanticCoordinates b) =>
        CoordinateContext.GetDistance(a, b);

    // =========================================================================
    // ENTITY ARCHIVAL (delegates to EntityArchival)
    // =========================================================================

    /// <summary>Mark an entity as historical and optionally archive its relationships.</summary>
    public void ArchiveEntity(EntityId entityId, bool archiveRelationships = true) =>
        EntityArchival.ArchiveEntity(_graph, entityId, archiveRelationships);

    /// <summary>Archive multiple entities.</summary>
    public void ArchiveEntities(IEnumerable<EntityId> entityIds, bool archiveRelationships = true) =>
        EntityArchival.ArchiveEntities(_graph, entityIds, archiveRelationships);

    /// <summary>Transfer relationships from source entities to a target entity.</summary>
    public int TransferRelationships(IEnumerable<EntityId> sourceIds, EntityId targetId) =>
        EntityArchival.TransferRelationships(_graph, sourceIds, targetId);

    /// <summary>Create part_of relationships from members to a container.</summary>
    public int CreatePartOfRelationships(IEnumerable<EntityId> memberIds, EntityId containerId) =>
        EntityArchival.CreatePartOfRelationships(_graph, memberIds, containerId);

    // =========================================================================
    // POPULATION TRACKING (delegates to PopulationTracker)
    // =========================================================================

    /// <summary>Update population metrics from current graph state.</summary>
    public void UpdatePopulationMetrics() => _populationTracker.Update(_graph);

    /// <summary>Get current population metrics snapshot.</summary>
    public PopulationMetrics GetPopulationMetrics() => _populationTracker.GetMetrics();

    /// <summary>Get population summary statistics.</summary>
    public PopulationSummary GetPopulationSummary() => _populationTracker.GetSummary();

    // =========================================================================
    // RANDOM NUMBER GENERATION
    // =========================================================================

    /// <summary>Get the next random integer in [0, maxExclusive).</summary>
    public int NextRandomInt(int maxExclusive) => _random.Next(maxExclusive);

    /// <summary>Get the next random double in [0.0, 1.0).</summary>
    public double NextRandomDouble() => _random.NextDouble();

    // =========================================================================
    // LOGGING (delegates to ISimulationEmitter)
    // =========================================================================

    /// <summary>Log a message via the simulation emitter.</summary>
    public void Log(LogLevel level, string message, IReadOnlyDictionary<string, object?>? context = null) =>
        _config.Emitter.Log(level, message, context);
}
