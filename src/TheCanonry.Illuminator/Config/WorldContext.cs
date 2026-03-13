using TheCanonry.Illuminator.Chronicle.PerspectiveSynthesis;

namespace TheCanonry.Illuminator.Config;

public sealed class WorldContext
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string ToneCore { get; init; }
    public required IReadOnlyList<CanonFact> CanonFacts { get; init; }
    public string? SpeciesConstraint { get; init; }
    public IReadOnlyList<WorldDynamic>? WorldDynamics { get; set; }
}

/// <summary>
/// A single world canon fact at config level. Distinct from CanonFactWithMetadata
/// in TheCanonry.Illuminator.Chronicle.PerspectiveSynthesis which is a synthesis-layer type.
/// </summary>
public sealed record CanonFact(string Id, string Text, string Type, bool Required, bool Disabled);
