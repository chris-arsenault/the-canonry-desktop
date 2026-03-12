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
    public List<double> History { get; } = [];
}
