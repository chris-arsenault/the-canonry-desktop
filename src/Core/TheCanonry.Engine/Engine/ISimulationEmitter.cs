using TheCanonry.Schema.Ids;

namespace TheCanonry.Engine.Engine;

// =============================================================================
// EVENT PAYLOAD TYPES
// =============================================================================

/// <summary>Simulation progress state.</summary>
public enum SimulationPhase
{
    Initializing,
    Validating,
    Running,
    Finalizing,
    Paused
}

/// <summary>Progress information emitted during simulation.</summary>
public sealed record ProgressPayload(
    SimulationPhase Phase,
    int Tick,
    int MaxTicks,
    int Epoch,
    int TotalEpochs,
    int EntityCount,
    int RelationshipCount);

/// <summary>Log level for simulation messages.</summary>
public enum LogLevel
{
    Info,
    Warn,
    Error,
    Debug
}

/// <summary>Log message from simulation.</summary>
public sealed record LogPayload(
    LogLevel Level,
    string Message,
    long Timestamp,
    IReadOnlyDictionary<string, object?> Context);

/// <summary>Epoch start information.</summary>
public sealed record EpochStartPayload(
    int Epoch,
    EraId EraId,
    string EraName,
    string EraSummary,
    int Tick);

/// <summary>System action summary emitted when a system does meaningful work.</summary>
public sealed record SystemActionPayload(
    int Tick,
    int Epoch,
    string SystemId,
    string SystemName,
    int RelationshipsAdded,
    int EntitiesModified,
    IReadOnlyDictionary<string, double> PressureChanges,
    string Description);

/// <summary>Error during simulation.</summary>
public sealed record ErrorPayload(
    string Message,
    string? Stack,
    string Phase,
    IReadOnlyDictionary<string, object?> Context);

// =============================================================================
// EMITTER INTERFACE
// =============================================================================

/// <summary>
/// Event emitter for simulation. WorldEngine requires this to emit events
/// during simulation for real-time UI updates and diagnostics.
/// </summary>
public interface ISimulationEmitter
{
    /// <summary>Emit simulation progress update.</summary>
    void Progress(ProgressPayload payload);

    /// <summary>Emit a log message.</summary>
    void Log(LogLevel level, string message, IReadOnlyDictionary<string, object?>? context = null);

    /// <summary>Emit epoch start event.</summary>
    void EpochStart(EpochStartPayload payload);

    /// <summary>Emit system action event.</summary>
    void SystemAction(SystemActionPayload payload);

    /// <summary>Emit era transition event.</summary>
    void EraTransition(EraId fromEra, string fromName, EraId toEra, string toName, int tick);

    /// <summary>Emit simulation error.</summary>
    void Error(ErrorPayload payload);

    /// <summary>Emit simulation completion.</summary>
    void Complete();
}
