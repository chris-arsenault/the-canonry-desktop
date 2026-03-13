namespace TheCanonry.Engine.Statistics;

/// <summary>
/// Tracks population metrics for a single pressure value.
/// </summary>
public class PressureMetric
{
    public string Id { get; init; } = "";
    public double Value { get; set; }
    public double Target { get; set; }
    public double Deviation { get; set; }
    public double Trend { get; set; }
    private readonly List<double> _history = [];

    public IReadOnlyList<double> History => _history;

    /// <summary>Internal mutable access to history for PopulationTracker.</summary>
    internal List<double> MutableHistory => _history;
}
