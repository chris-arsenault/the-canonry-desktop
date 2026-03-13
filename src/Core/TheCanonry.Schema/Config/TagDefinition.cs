namespace TheCanonry.Schema.Config;

public class TagDefinition
{
    public required string Tag { get; init; }
    public string Category { get; init; } = "trait";
    public string Rarity { get; init; } = "common";
    public string Description { get; init; } = "";
    public bool IsAxis { get; init; }
    public bool IsFramework { get; init; }
    public int UsageCount { get; init; }
    public int MinUsage { get; init; }
    public int MaxUsage { get; init; } = 100;
    public IReadOnlyList<string> Templates { get; init; } = [];
    public IReadOnlyList<string> EntityKinds { get; init; } = [];
    public IReadOnlyList<string> RelatedTags { get; init; } = [];
    public IReadOnlyList<string> ConflictingTags { get; init; } = [];
    public IReadOnlyList<string> MutuallyExclusiveWith { get; init; } = [];
    public string ConsolidateInto { get; init; } = "";
}
