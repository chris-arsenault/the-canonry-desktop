namespace TheCanonry.Illuminator.Config;

public sealed class EntityGuidance
{
    public required Dictionary<string, KindGuidance> ByKind { get; init; }
}

public sealed record KindGuidance(
    string? DomainInstructions,
    string? VisualAvoid,
    string? ProseHints,
    string? TraitGuidance);
