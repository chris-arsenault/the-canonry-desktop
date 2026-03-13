using TheCanonry.Engine.Pressures;
using TheCanonry.Engine.Statistics;
using TheCanonry.Engine.Templates;
using TheCanonry.Schema.Config;
using TheCanonry.Schema.World;

namespace TheCanonry.Engine.Engine;

// =============================================================================
// NARRATIVE CONFIG
// =============================================================================

/// <summary>
/// Configuration for narrative event tracking.
/// When enabled, the engine captures semantically meaningful state changes
/// and generates NarrativeEvents for story generation.
/// </summary>
public sealed record NarrativeConfig(
    /// <summary>Enable narrative event tracking (default: true).</summary>
    bool Enabled,
    /// <summary>Minimum significance score to include event (default: 0).</summary>
    double MinSignificance);

// =============================================================================
// RELATIONSHIP BUDGET
// =============================================================================

/// <summary>Limits on relationship creation rates.</summary>
public sealed record RelationshipBudget(
    /// <summary>Maximum relationships created per simulation tick.</summary>
    int MaxPerSimulationTick,
    /// <summary>Maximum relationships created per growth phase.</summary>
    int MaxPerGrowthPhase);

// =============================================================================
// ENGINE CONFIG
// =============================================================================

/// <summary>
/// Top-level engine configuration record. This ties together all domain
/// configuration, templates, systems, pressures, and runtime settings
/// needed to run a simulation.
/// </summary>
public sealed record EngineConfig(
    /// <summary>Domain schema defining entity kinds, relationship kinds, and cultures.</summary>
    DomainSchema Schema,

    /// <summary>Era definitions with transition conditions and template/system weights.</summary>
    IReadOnlyList<Era> Eras,

    /// <summary>Declarative template definitions from domain JSON config.</summary>
    IReadOnlyList<DeclarativeTemplate> Templates,

    /// <summary>Declarative system configurations.</summary>
    IReadOnlyList<DeclarativeSystem> Systems,

    /// <summary>Declarative pressure definitions.</summary>
    IReadOnlyList<DeclarativePressure> Pressures,

    /// <summary>Ticks per epoch.</summary>
    int TicksPerEpoch,

    /// <summary>Maximum number of epochs.</summary>
    int MaxEpochs,

    /// <summary>Maximum simulation ticks.</summary>
    int MaxTicks,

    /// <summary>Maximum relationships per type.</summary>
    int MaxRelationshipsPerType,

    /// <summary>Relationship creation rate limits.</summary>
    RelationshipBudget RelationshipBudget,

    /// <summary>Scale factor for world size.</summary>
    double ScaleFactor,

    /// <summary>Default minimum distance between entities.</summary>
    double DefaultMinDistance,

    /// <summary>Smoothing factor for pressure delta per tick.</summary>
    double PressureDeltaSmoothing,

    /// <summary>Per-subtype growth targets for homeostatic regulation.</summary>
    DistributionTargets DistributionTargets,

    /// <summary>Name generation service.</summary>
    INameGenerationService NameService,

    /// <summary>Seed relationships loaded alongside initial entities.</summary>
    IReadOnlyList<Relationship> SeedRelationships,

    /// <summary>Simulation event emitter.</summary>
    ISimulationEmitter Emitter,

    /// <summary>Narrative event tracking configuration.</summary>
    NarrativeConfig NarrativeConfig);
