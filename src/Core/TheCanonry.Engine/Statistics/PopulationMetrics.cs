namespace TheCanonry.Engine.Statistics;

/// <summary>
/// Aggregation of all population metrics at a given tick.
/// </summary>
public class PopulationMetrics
{
    public int Tick { get; set; }
    public Dictionary<string, EntityMetric> Entities { get; } = [];
    public Dictionary<string, RelationshipMetric> Relationships { get; } = [];
    public Dictionary<string, PressureMetric> Pressures { get; } = [];
}
