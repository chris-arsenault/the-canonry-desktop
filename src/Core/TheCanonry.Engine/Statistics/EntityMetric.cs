namespace TheCanonry.Engine.Statistics;

/// <summary>
/// Tracks population metrics for a single entity kind:subtype combination.
/// Used by PopulationTracker for homeostatic feedback control.
/// </summary>
public class EntityMetric
{
    public string Kind { get; init; } = "";
    public string Subtype { get; init; } = "";
    public int Count { get; set; }
    public int Target { get; set; }

    /// <summary>Deviation from target: (count - target) / max(target, 1).</summary>
    public double Deviation { get; set; }

    /// <summary>Moving average of count deltas between consecutive history entries.</summary>
    public double Trend { get; set; }

    /// <summary>Last N tick counts (window size controlled by PopulationTracker).</summary>
    public List<int> History { get; } = [];
}
