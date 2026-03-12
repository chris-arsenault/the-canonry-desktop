namespace TheCanonry.Illuminator.Config;

/// <summary>
/// Per-culture key-value identity attributes (naming conventions, visual motifs, etc.)
/// </summary>
public sealed class CultureIdentityMap
{
    public required Dictionary<string, Dictionary<string, string>> ByCulture { get; init; }
}
