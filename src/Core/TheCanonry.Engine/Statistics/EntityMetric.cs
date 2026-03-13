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

    private readonly List<int> _history = [];

    /// <summary>Last N tick counts (window size controlled by PopulationTracker).</summary>
    public IReadOnlyList<int> History => _history;

    /// <summary>Internal mutable access to history for PopulationTracker.</summary>
    internal List<int> MutableHistory => _history;
}
