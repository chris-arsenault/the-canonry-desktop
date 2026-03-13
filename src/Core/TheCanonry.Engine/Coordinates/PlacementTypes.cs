using TheCanonry.Schema.Config;
using TheCanonry.Schema.World;

namespace TheCanonry.Engine.Coordinates;

/// <summary>
/// Result of a culture-aware placement operation.
/// </summary>
public class PlacementResult
{
    public required SemanticCoordinates Coordinates { get; init; }
    public string RegionId { get; init; } = "";
    public IReadOnlyList<string> AllRegionIds { get; init; } = [];
    public Dictionary<string, object> DerivedTags { get; init; } = [];
    public string Strategy { get; init; } = "";
}

/// <summary>
/// Axis definition with low/high tags for gradient-based tag derivation.
/// </summary>
public class AxisDefinition
{
    public required string Id { get; init; }
    public string LowTag { get; init; } = "";
    public string HighTag { get; init; } = "";
}

/// <summary>
/// Result of emergent region creation.
/// </summary>
public class EmergentRegionResult
{
    public required SemanticRegion Region { get; init; }
    public required SemanticCoordinates Coordinates { get; init; }
}
