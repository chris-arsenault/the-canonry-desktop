namespace TheCanonry.Engine.Statistics;

/// <summary>
/// Tracks population metrics for a single relationship kind.
/// </summary>
public class RelationshipMetric
{
    public string Kind { get; init; } = "";
    public int Count { get; set; }
    public int Target { get; set; }
    public double Deviation { get; set; }
    public double Trend { get; set; }
    public List<int> History { get; } = [];
}
