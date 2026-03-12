namespace TheCanonry.Engine.Engine;

using TheCanonry.Engine.Runtime;
using TheCanonry.Engine.Systems;

/// <summary>
/// Factory that creates ISimulationSystem instances from DeclarativeSystem configs.
/// Reads the SystemType discriminator and dispatches to the appropriate system constructor.
///
/// Port of: apps/lore-weave/lib/engine/systemInterpreter.ts
/// </summary>
public static class SystemInterpreter
{
    /// <summary>
    /// Create a single ISimulationSystem from a DeclarativeSystem config.
    /// </summary>
    /// <param name="config">The declarative system configuration.</param>
    /// <param name="runtime">The world runtime that the system will operate on.</param>
    /// <returns>An ISimulationSystem instance ready for initialization and execution.</returns>
    public static ISimulationSystem Create(DeclarativeSystem config, WorldRuntime runtime)
    {
        return config.SystemType switch
        {
            SystemType.EraTransition => CreateEraTransition(config, runtime),
            SystemType.ConnectionEvolution => CreateDomainSystem<ConnectionEvolution>(config, runtime),
            SystemType.RelationshipMaintenance => CreateRelationshipMaintenance(config, runtime),
            SystemType.ThresholdTrigger => CreateDomainSystem<ThresholdTrigger>(config, runtime),
            SystemType.GraphContagion => CreateDomainSystem<GraphContagion>(config, runtime),
            SystemType.ClusterFormation => CreateDomainSystem<ClusterFormation>(config, runtime),
            SystemType.TagDiffusion => CreateDomainSystem<TagDiffusion>(config, runtime),
            SystemType.PlaneDiffusion => CreateDomainSystem<PlaneDiffusion>(config, runtime),
            SystemType.UniversalCatalyst => CreateUniversalCatalyst(config, runtime),
            SystemType.EraSpawner => throw new NotSupportedException(
                "EraSpawner is handled directly by WorldEngine, not through SystemInterpreter."),
            SystemType.Growth => throw new NotSupportedException(
                "Growth system is handled directly by WorldEngine, not through SystemInterpreter."),
            _ => throw new ArgumentOutOfRangeException(
                nameof(config),
                config.SystemType,
                $"Unknown system type: {config.SystemType}")
        };
    }

    /// <summary>
    /// Load multiple systems from declarative configurations.
    /// Filters out disabled systems.
    /// </summary>
    public static IReadOnlyList<ISimulationSystem> LoadSystems(
        IReadOnlyList<DeclarativeSystem> configs,
        WorldRuntime runtime)
    {
        var systems = new List<ISimulationSystem>();

        foreach (var config in configs)
        {
            if (!config.Enabled)
                continue;

            // EraSpawner and Growth are handled by WorldEngine directly
            if (config.SystemType is SystemType.EraSpawner or SystemType.Growth)
                continue;

            systems.Add(Create(config, runtime));
        }

        return systems;
    }

    // =========================================================================
    // FRAMEWORK SYSTEM CONSTRUCTORS (strongly-typed configs)
    // =========================================================================

    private static ISimulationSystem CreateEraTransition(DeclarativeSystem config, WorldRuntime runtime)
    {
        var c = config.Config;
        var typedConfig = new EraTransitionConfig(
            Id: GetRequiredString(c, "id"),
            Name: GetRequiredString(c, "name"),
            Description: GetStringOrDefault(c, "description"),
            ProminenceSnapshotEnabled: GetBoolOrDefault(c, "prominenceSnapshotEnabled"),
            ProminenceSnapshotMinProminence: GetStringOrDefault(c, "prominenceSnapshotMinProminence", "recognized"));

        return new Systems.EraTransition(typedConfig, runtime);
    }

    private static ISimulationSystem CreateUniversalCatalyst(DeclarativeSystem config, WorldRuntime runtime)
    {
        var c = config.Config;
        var typedConfig = new UniversalCatalystConfig(
            Id: GetRequiredString(c, "id"),
            Name: GetRequiredString(c, "name"),
            Description: GetStringOrDefault(c, "description"),
            ActionAttemptRate: GetDoubleOrDefault(c, "actionAttemptRate", 0.3),
            PressureMultiplier: GetDoubleOrDefault(c, "pressureMultiplier", 1.5),
            ProminenceUpChanceOnSuccess: GetDoubleOrDefault(c, "prominenceUpChanceOnSuccess", 0.1),
            ProminenceDownChanceOnFailure: GetDoubleOrDefault(c, "prominenceDownChanceOnFailure", 0.05));

        return new Systems.UniversalCatalyst(typedConfig, runtime);
    }

    private static ISimulationSystem CreateRelationshipMaintenance(DeclarativeSystem config, WorldRuntime runtime)
    {
        var c = config.Config;
        var proximityKinds = GetStringListOrDefault(c, "proximityRelationshipKinds");

        var typedConfig = new RelationshipMaintenanceConfig(
            Id: GetRequiredString(c, "id"),
            Name: GetRequiredString(c, "name"),
            Description: GetStringOrDefault(c, "description"),
            MaintenanceFrequency: GetIntOrDefault(c, "maintenanceFrequency", 5),
            CullThreshold: GetDoubleOrDefault(c, "cullThreshold", 0.15),
            GracePeriod: GetIntOrDefault(c, "gracePeriod", 20),
            ReinforcementBonus: GetDoubleOrDefault(c, "reinforcementBonus", 0.02),
            MaxStrength: GetDoubleOrDefault(c, "maxStrength", 1.0),
            ProximityRelationshipKinds: proximityKinds);

        return new Systems.RelationshipMaintenance(typedConfig, runtime);
    }

    // =========================================================================
    // DOMAIN SYSTEM CONSTRUCTOR (dictionary-based config)
    // =========================================================================

    /// <summary>
    /// Create a domain system that takes a raw config dictionary.
    /// Extracts id/name from the config, passes the full dictionary to the constructor.
    /// </summary>
    private static ISimulationSystem CreateDomainSystem<T>(
        DeclarativeSystem config,
        WorldRuntime runtime) where T : ISimulationSystem
    {
        var c = config.Config;
        var id = GetRequiredString(c, "id");
        var name = GetRequiredString(c, "name");

        return typeof(T).Name switch
        {
            nameof(ConnectionEvolution) => new ConnectionEvolution(id, name, c, runtime),
            nameof(ThresholdTrigger) => new ThresholdTrigger(id, name, c, runtime),
            nameof(GraphContagion) => new GraphContagion(id, name, c, runtime),
            nameof(ClusterFormation) => new ClusterFormation(id, name, c, runtime),
            nameof(TagDiffusion) => new TagDiffusion(id, name, c, runtime),
            nameof(PlaneDiffusion) => new PlaneDiffusion(id, name, c, runtime),
            _ => throw new InvalidOperationException($"No constructor mapping for domain system type {typeof(T).Name}")
        };
    }

    // =========================================================================
    // CONFIG DICTIONARY HELPERS
    // =========================================================================

    private static string GetRequiredString(IReadOnlyDictionary<string, object?> config, string key)
    {
        if (config.TryGetValue(key, out var value) && value is string s)
            return s;

        throw new InvalidOperationException(
            $"Required string config key '{key}' is missing or not a string.");
    }

    private static string GetStringOrDefault(IReadOnlyDictionary<string, object?> config, string key, string defaultValue = "")
    {
        if (config.TryGetValue(key, out var value) && value is string s)
            return s;
        return defaultValue;
    }

    private static double GetDoubleOrDefault(IReadOnlyDictionary<string, object?> config, string key, double defaultValue = 0.0)
    {
        if (!config.TryGetValue(key, out var value) || value is null)
            return defaultValue;

        return value switch
        {
            double d => d,
            int i => i,
            long l => l,
            float f => f,
            _ => defaultValue
        };
    }

    private static int GetIntOrDefault(IReadOnlyDictionary<string, object?> config, string key, int defaultValue = 0)
    {
        if (!config.TryGetValue(key, out var value) || value is null)
            return defaultValue;

        return value switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            _ => defaultValue
        };
    }

    private static bool GetBoolOrDefault(IReadOnlyDictionary<string, object?> config, string key, bool defaultValue = false)
    {
        if (config.TryGetValue(key, out var value) && value is bool b)
            return b;
        return defaultValue;
    }

    private static IReadOnlyList<string> GetStringListOrDefault(IReadOnlyDictionary<string, object?> config, string key)
    {
        if (!config.TryGetValue(key, out var value) || value is null)
            return [];

        if (value is IReadOnlyList<string> list)
            return list;

        if (value is IEnumerable<object?> enumerable)
            return enumerable.Where(x => x is string).Cast<string>().ToList();

        return [];
    }
}
