namespace TheCanonry.Schema.Config;

public class SeedEntity
{
    public required string Id { get; init; }
    public required string Kind { get; init; }
    public string Subtype { get; init; } = "";
    public required string Name { get; init; }
    public string Summary { get; init; } = "";
    public string Description { get; init; } = "";
    public string Status { get; init; } = "active";
    public double Prominence { get; init; } = 2.5;
    public string Culture { get; init; } = "";
    public Dictionary<string, bool> Tags { get; init; } = [];
    public SeedCoordinates Coordinates { get; init; } = new();
    public int CreatedAt { get; init; }
    public int UpdatedAt { get; init; }
}

public class SeedCoordinates
{
    public double X { get; init; } = 50;
    public double Y { get; init; } = 50;
    public double Z { get; init; } = 50;
}

public class SeedRelationship
{
    public required string Kind { get; init; }
    public required string Src { get; init; }
    public required string Dst { get; init; }
    public double Strength { get; init; } = 0.5;
}
