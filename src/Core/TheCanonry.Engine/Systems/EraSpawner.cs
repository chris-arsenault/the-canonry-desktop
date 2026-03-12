namespace TheCanonry.Engine.Systems;

using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Rules;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Engine.Runtime;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.Primitives;
using TheCanonry.Schema.World;

/// <summary>
/// Creates the initial era entity at simulation start.
///
/// Lazy spawning model:
/// - Only the FIRST era is spawned at initialization
/// - Subsequent eras are spawned on-demand by EraTransition when conditions are met
/// - This supports divergent era paths where the next era depends on world state
///
/// This system only runs once: if any era entity already exists, it returns immediately.
/// </summary>
public sealed class EraSpawner : ISimulationSystem
{
    private readonly FrameworkSystemConfig _config;

    public EraSpawner(FrameworkSystemConfig config, WorldRuntime runtime)
    {
        _config = config;
        Runtime = runtime;
    }

    public string Id => _config.Id;
    public string Name => _config.Name;

    /// <summary>The runtime this system operates on.</summary>
    internal WorldRuntime Runtime { get; }

    public void Initialize() { }

    public Task<SystemResult> ApplyAsync(double modifier)
    {
        // Check if any era entities already exist
        var existingEras = Runtime.FindEntities(EntityCriteria.Create(
            kind: FrameworkPrimitives.EntityKinds.Era,
            includeHistorical: true));

        if (existingEras.Count > 0)
        {
            return Task.FromResult(SystemResult.Empty with
            {
                Description = $"{existingEras.Count} era entities already exist"
            });
        }

        // Get eras from config
        var configEras = Runtime.Config.Eras;
        if (configEras.Count == 0)
        {
            return Task.FromResult(SystemResult.Empty with
            {
                Description = "No eras defined in config"
            });
        }

        // Only create the FIRST era at init
        var firstEraConfig = configEras[0];
        var eraEntity = CreateEraEntity(firstEraConfig, Runtime.CurrentTick, FrameworkPrimitives.Statuses.Current);

        Runtime.CreateEntity(eraEntity);
        Runtime.CurrentEra = firstEraConfig;

        // Apply entry effects for the first era
        var pressureChanges = ApplyEntryEffects(Runtime, firstEraConfig);

        Runtime.Log(LogLevel.Info, $"[EraSpawner] Started first era: {firstEraConfig.Name}");

        return Task.FromResult(new SystemResult(
            RelationshipsAdded: [],
            RelationshipsAdjusted: [],
            RelationshipsToArchive: [],
            EntitiesModified: [],
            PressureChanges: pressureChanges,
            Description: $"Started first era: {firstEraConfig.Name}",
            Details: new Dictionary<string, object?>(),
            NarrationsByGroup: new Dictionary<string, string>()));
    }

    // =========================================================================
    // STATIC HELPERS (used by EraTransition for lazy spawning)
    // =========================================================================

    /// <summary>
    /// Create an era entity from config. Used by both EraSpawner and EraTransition.
    /// </summary>
    internal static Entity CreateEraEntity(Era configEra, int tick, EntityStatus status)
    {
        var entity = new Entity(
            id: new EntityId(configEra.Id.Value),
            kind: FrameworkPrimitives.EntityKinds.Era,
            subtype: configEra.Id.Value,
            name: configEra.Name,
            culture: FrameworkPrimitives.Cultures.World,
            eraId: new EraId(configEra.Id.Value),
            coordinates: new SemanticCoordinates(50.0, 50.0, 50.0),
            createdBy: new ExecutionContext(tick, ExecutionSource.Framework, "era_spawner", true, ""),
            tick: tick);

        entity.UpdateStatus(status, tick);
        entity.SetProminence(new Prominence(5.0), tick); // Eras are always mythic
        entity.SetSummary(configEra.Summary, tick);
        entity.LockSummary();

        entity.Tags.Set(FrameworkPrimitives.Tags.Temporal, true);
        entity.Tags.Set(FrameworkPrimitives.Tags.Era, true);
        entity.Tags.Set(FrameworkPrimitives.Tags.EraId, configEra.Id.Value);

        return entity;
    }

    /// <summary>
    /// Apply entry effects (pressure mutations) when transitioning INTO an era.
    /// </summary>
    internal static Dictionary<string, double> ApplyEntryEffects(WorldRuntime runtime, Era configEra)
    {
        var pressureChanges = new Dictionary<string, double>();
        var mutations = configEra.EntryEffects.Mutations;

        foreach (var mutation in mutations)
        {
            if (pressureChanges.TryGetValue(mutation.PressureId, out var current))
                pressureChanges[mutation.PressureId] = current + mutation.Delta;
            else
                pressureChanges[mutation.PressureId] = mutation.Delta;
        }

        return pressureChanges;
    }
}
