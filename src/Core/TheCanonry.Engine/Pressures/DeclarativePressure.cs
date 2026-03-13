using TheCanonry.Engine.Rules.Types;

namespace TheCanonry.Engine.Pressures;

// =============================================================================
// PRESSURE CONTRACT
// =============================================================================

/// <summary>Source that creates/increases a pressure.</summary>
public sealed record PressureSource(
    string Component,
    double Delta,
    string Formula);

/// <summary>Sink that reduces a pressure.</summary>
public sealed record PressureSink(
    string Component,
    double Delta,
    string Formula);

/// <summary>How a pressure affects another component.</summary>
public sealed record PressureAffect(
    string Component,
    string Effect,
    double Threshold,
    double Factor);

/// <summary>Expected equilibrium behavior of a pressure.</summary>
public sealed record PressureEquilibrium(
    double ExpectedMin,
    double ExpectedMax,
    double RestingPoint,
    int OscillationPeriod);

/// <summary>
/// Documentation contract for a pressure (for UI display).
/// Describes sources, sinks, and expected equilibrium behavior.
/// </summary>
public sealed record PressureContract(
    IReadOnlyList<PressureSource> Sources,
    IReadOnlyList<PressureSink> Sinks,
    IReadOnlyList<PressureAffect> Affects,
    PressureEquilibrium Equilibrium);

// =============================================================================
// FEEDBACK GROWTH
// =============================================================================

/// <summary>
/// Feedback drivers for a pressure.
/// Positive feedback increases pressure; negative feedback decreases it.
/// Feedback factors are Metric instances from the Rules system.
/// </summary>
public sealed record PressureGrowth(
    /// <summary>Factors that increase pressure.</summary>
    IReadOnlyList<Metric> PositiveFeedback,

    /// <summary>Factors that decrease pressure.</summary>
    IReadOnlyList<Metric> NegativeFeedback);

// =============================================================================
// DECLARATIVE PRESSURE
// =============================================================================

/// <summary>
/// Declarative pressure definition - JSON serializable.
/// Pressures are scalar values (-100 to 100) that represent world-state signals
/// driving template and system behavior.
/// </summary>
public sealed record DeclarativePressure(
    /// <summary>Unique pressure identifier.</summary>
    string Id,

    /// <summary>Human-readable name.</summary>
    string Name,

    /// <summary>Human-friendly description of the pressure and what +/- values mean.</summary>
    string Description,

    /// <summary>Initial value (-100 to 100).</summary>
    double InitialValue,

    /// <summary>
    /// Homeostatic factor pulling pressure toward equilibrium (0).
    /// Applied each tick as: delta = (0 - currentValue) * homeostasis.
    /// </summary>
    double Homeostasis,

    /// <summary>Feedback drivers (positive and negative).</summary>
    PressureGrowth Growth,

    /// <summary>Documentation contract (for UI display).</summary>
    PressureContract Contract);
