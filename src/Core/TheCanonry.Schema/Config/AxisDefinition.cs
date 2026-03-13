namespace TheCanonry.Schema.Config;

public class AxisDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public required string LowTag { get; init; }
    public required string HighTag { get; init; }
}
