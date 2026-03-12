namespace TheCanonry.Engine.Pressures;

using TheCanonry.Engine.Runtime;

/// <summary>
/// Runtime pressure object with executable growth function.
/// Pressures are scalar values (-100 to 100) representing world-state signals
/// that drive template and system behavior.
///
/// Port of: apps/lore-weave/lib/engine/types.ts (Pressure interface)
/// </summary>
public sealed class Pressure
{
    /// <summary>Unique pressure identifier.</summary>
    public string Id { get; }

    /// <summary>Human-readable name.</summary>
    public string Name { get; }

    /// <summary>Current pressure value, clamped to [-100, 100].</summary>
    public double Value { get; set; }

    /// <summary>
    /// Homeostatic factor pulling pressure toward equilibrium (0).
    /// Applied each tick as: delta = (0 - currentValue) * homeostasis.
    /// </summary>
    public double Homeostasis { get; }

    /// <summary>Minimum allowed value (default: -100).</summary>
    public double MinValue { get; }

    /// <summary>Maximum allowed value (default: 100).</summary>
    public double MaxValue { get; }

    /// <summary>
    /// Growth function that evaluates feedback metrics to compute pressure delta per tick.
    /// Takes a WorldRuntime (for graph access) and returns the net feedback value.
    /// </summary>
    public Func<WorldRuntime, double> GrowthFunction { get; }

    public Pressure(
        string id,
        string name,
        double initialValue,
        double homeostasis,
        Func<WorldRuntime, double> growthFunction,
        double minValue = -100.0,
        double maxValue = 100.0)
    {
        Id = id;
        Name = name;
        Value = Math.Clamp(initialValue, minValue, maxValue);
        Homeostasis = homeostasis;
        GrowthFunction = growthFunction;
        MinValue = minValue;
        MaxValue = maxValue;
    }

    /// <summary>
    /// Evaluate the growth function and apply the delta to the pressure value.
    /// The delta is the sum of positive/negative feedback metrics.
    /// </summary>
    public void ApplyGrowth(WorldRuntime runtime)
    {
        var delta = GrowthFunction(runtime);
        Value = Math.Clamp(Value + delta, MinValue, MaxValue);
    }

    /// <summary>
    /// Apply homeostatic pull toward equilibrium (0).
    /// The pull strength is proportional to the current value and the homeostasis factor.
    /// </summary>
    /// <param name="rate">
    /// Additional scaling factor for the homeostasis pull (e.g., smoothing factor from engine config).
    /// The effective delta is: (0 - Value) * Homeostasis * rate.
    /// </param>
    public void ApplyHomeostasis(double rate = 1.0)
    {
        var delta = (0.0 - Value) * Homeostasis * rate;
        Value = Math.Clamp(Value + delta, MinValue, MaxValue);
    }
}
