namespace TheCanonry.Schema.Config;

using TheCanonry.Schema.Ids;

public class AxisBias
{
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Z { get; init; }
}

public class CultureDefinition
{
    public required CultureId Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public bool IsFramework { get; init; }
    public string Homeland { get; init; } = "";
    public string Color { get; init; } = "";
    public Dictionary<string, AxisBias> AxisBiases { get; init; } = [];
    public Dictionary<string, List<string>> HomeRegions { get; init; } = [];
    public string DefaultArtisticStyleId { get; init; } = "";
    public Dictionary<string, string> DefaultCompositionStyles { get; init; } = [];
    public IReadOnlyList<string> StyleKeywords { get; init; } = [];
    public Dictionary<string, string> VisualIdentity { get; init; } = [];
}
