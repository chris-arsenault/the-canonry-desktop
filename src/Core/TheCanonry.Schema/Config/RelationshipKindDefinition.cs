namespace TheCanonry.Schema.Config;

using TheCanonry.Schema.Domain;

public class RelationshipVerbs
{
    public required string Formed { get; init; }
    public required string Ended { get; init; }
    public string InverseFormed { get; init; } = "";
    public string InverseEnded { get; init; } = "";
}

public class RelationshipKindDefinition
{
    public required RelationshipKind Kind { get; init; }
    public string Name { get; init; } = "";
    public required string Description { get; init; }
    public bool IsFramework { get; init; }
    public IReadOnlyList<string> SrcKinds { get; init; } = [];
    public IReadOnlyList<string> DstKinds { get; init; } = [];
    public bool Symmetric { get; init; }
    public string Category { get; init; } = "";
    public bool Cullable { get; init; }
    public string DecayRate { get; init; } = "none";
    public Polarity Polarity { get; init; } = Polarity.Neutral;
    public RelationshipVerbs? Verbs { get; init; }
}
