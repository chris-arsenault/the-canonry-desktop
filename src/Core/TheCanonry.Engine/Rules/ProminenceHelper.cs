namespace TheCanonry.Engine.Rules;

/// <summary>
/// Helper methods for converting between prominence labels and numeric thresholds.
/// Mirrors the TS prominenceThreshold/prominenceLabel functions.
/// </summary>
public static class ProminenceHelper
{
    /// <summary>Convert a label string to its numeric threshold (lower bound of range).</summary>
    public static double LabelToThreshold(string label) => label.ToLowerInvariant() switch
    {
        "forgotten" => 0.0,
        "marginal" => 1.0,
        "recognized" => 2.0,
        "renowned" => 3.0,
        "mythic" => 4.0,
        _ => 0.0
    };

    /// <summary>Convert a numeric prominence value to its display label.</summary>
    public static string ValueToLabel(double value) => value switch
    {
        < 1.0 => "forgotten",
        < 2.0 => "marginal",
        < 3.0 => "recognized",
        < 4.0 => "renowned",
        _ => "mythic"
    };

    /// <summary>Clamp prominence value to valid range [0, 5].</summary>
    public static double Clamp(double value) => Math.Max(0.0, Math.Min(5.0, value));
}
