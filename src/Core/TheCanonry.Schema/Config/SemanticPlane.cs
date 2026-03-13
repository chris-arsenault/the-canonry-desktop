namespace TheCanonry.Schema.Config;

public class SemanticAxis
{
    public required string AxisId { get; init; }
}

public class SemanticPlane
{
    public required SemanticPlaneAxes Axes { get; init; }
    public IReadOnlyList<SemanticRegion> Regions { get; init; } = [];
}

public class SemanticPlaneAxes
{
    public required SemanticAxis X { get; init; }
    public required SemanticAxis Y { get; init; }
    public required SemanticAxis Z { get; init; }
}

public class SemanticRegion
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required string Color { get; init; }
    public required string Culture { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public string Description { get; init; } = "";
    public ZRange? ZRange { get; init; }
    public string? ParentRegion { get; init; }
    public bool Emergent { get; init; }
    public int CreatedAt { get; init; }
    public string CreatedBy { get; init; } = "";
    public RegionBounds? Bounds { get; init; }
    public Dictionary<string, object?> Metadata { get; init; } = [];
}

public readonly record struct ZRange(double Min, double Max);

// Region bounds — discriminated union via sealed hierarchy
public abstract record RegionBounds(string Shape);
public sealed record CircleBounds(double CenterX, double CenterY, double Radius)
    : RegionBounds("circle");
public sealed record RectBounds(double X1, double Y1, double X2, double Y2)
    : RegionBounds("rect");
public sealed record PolygonBounds(IReadOnlyList<PolygonPoint> Points)
    : RegionBounds("polygon");
public readonly record struct PolygonPoint(double X, double Y);
