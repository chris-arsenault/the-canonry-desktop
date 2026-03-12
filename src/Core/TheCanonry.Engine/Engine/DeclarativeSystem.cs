namespace TheCanonry.Engine.Engine;

using TheCanonry.Engine.Rules.Types;

// =============================================================================
// SYSTEM TYPE DISCRIMINATOR
// =============================================================================

/// <summary>
/// Identifies which system class a declarative system configuration maps to.
/// </summary>
public enum SystemType
{
    ConnectionEvolution,
    GraphContagion,
    ThresholdTrigger,
    ClusterFormation,
    TagDiffusion,
    PlaneDiffusion,
    EraSpawner,
    EraTransition,
    UniversalCatalyst,
    RelationshipMaintenance,
    Growth
}

// =============================================================================
// FRAMEWORK SYSTEM CONFIGS
// =============================================================================

/// <summary>
/// Base framework system config. All framework systems have id, name, description.
/// </summary>
public sealed record FrameworkSystemConfig(
    string Id,
    string Name,
    string Description);

/// <summary>
/// Era transition system config.
/// Handles transitions between eras based on exit/entry conditions.
/// </summary>
public sealed record EraTransitionConfig(
    string Id,
    string Name,
    string Description,
    bool ProminenceSnapshotEnabled,
    string ProminenceSnapshotMinProminence);

/// <summary>
/// Universal catalyst system config.
/// Enables agents to perform domain-defined actions.
/// </summary>
public sealed record UniversalCatalystConfig(
    string Id,
    string Name,
    string Description,
    /// <summary>Base chance per tick that agents attempt actions (default: 0.3).</summary>
    double ActionAttemptRate,
    /// <summary>How much pressures amplify action attempt rates (default: 1.5).</summary>
    double PressureMultiplier,
    /// <summary>Chance of prominence increase on successful action (0-1, default: 0.1).</summary>
    double ProminenceUpChanceOnSuccess,
    /// <summary>Chance of prominence decrease on failed action (0-1, default: 0.05).</summary>
    double ProminenceDownChanceOnFailure);

/// <summary>
/// Relationship maintenance system config.
/// Handles decay, reinforcement, and culling of relationships.
/// </summary>
public sealed record RelationshipMaintenanceConfig(
    string Id,
    string Name,
    string Description,
    /// <summary>Run maintenance every N ticks (default: 5).</summary>
    int MaintenanceFrequency,
    /// <summary>Remove cullable relationships below this strength (default: 0.15).</summary>
    double CullThreshold,
    /// <summary>Don't decay or cull relationships younger than this many ticks (default: 20).</summary>
    int GracePeriod,
    /// <summary>Strength increase when reinforcement conditions are met (default: 0.02).</summary>
    double ReinforcementBonus,
    /// <summary>Maximum relationship strength (default: 1.0).</summary>
    double MaxStrength,
    /// <summary>
    /// Relationship kinds indicating entities are in "proximity" for reinforcement.
    /// Two entities are in proximity if they both have the same destination via these relationships.
    /// </summary>
    IReadOnlyList<string> ProximityRelationshipKinds);

// =============================================================================
// DECLARATIVE SYSTEM
// =============================================================================

/// <summary>
/// Declarative system configuration. Contains the system type discriminator
/// and a parameters dictionary for type-specific configuration.
///
/// Domain systems (ConnectionEvolution, GraphContagion, etc.) store their config
/// as a parameter dictionary. Framework systems use strongly-typed configs.
///
/// The system interpreter uses SystemType to create the appropriate runtime system.
/// </summary>
public sealed record DeclarativeSystem(
    /// <summary>Which system class this configuration maps to.</summary>
    SystemType SystemType,

    /// <summary>Whether this system is active (default: true).</summary>
    bool Enabled,

    /// <summary>
    /// System-specific configuration parameters.
    /// The system interpreter reads type-specific fields from this dictionary.
    /// For framework systems, use the strongly-typed config records above.
    /// </summary>
    IReadOnlyDictionary<string, object?> Config);
