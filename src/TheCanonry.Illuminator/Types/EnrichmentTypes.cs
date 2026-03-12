namespace TheCanonry.Illuminator.Types;

/// <summary>
/// The kind of enrichment operation to perform on an entity or world object.
/// </summary>
public enum EnrichmentType
{
    Description,
    VisualThesis,
    Image,
    EntityChronicle,
    PaletteExpansion,
    DynamicsGeneration,
    SummaryRevision,
    ChronicleBackport,
    HistorianEdition,
    HistorianReview,
    HistorianChronology,
    HistorianPrep,
    EraNarrative,
    MotifVariation,
    FactCoverage,
    ToneRanking,
    BulkToneRanking,
    EntityTagImageStyles,
    Upscale,
}

/// <summary>
/// Aggregated enrichment state stored on each entity.
/// Mirrors the TS EntityEnrichment interface — holds visual enrichment data
/// and metadata while summary/description live directly on the entity.
/// </summary>
public sealed record EntityEnrichment
{
    public TextEnrichment? Text { get; init; }
    public ImageEnrichment? Image { get; init; }
    public ChronicleEnrichmentRef? EntityChronicle { get; init; }
    public IReadOnlyList<string>? SlugAliases { get; init; }
    public ImageStyleAssignment? ImageStyle { get; init; }
}

/// <summary>
/// Text enrichment metadata (visual fields, aliases, costs).
/// Content (summary, description) lives on the entity itself.
/// </summary>
public sealed record TextEnrichment
{
    public required IReadOnlyList<string> Aliases { get; init; }
    public string? VisualThesis { get; init; }
    public required IReadOnlyList<string> VisualTraits { get; init; }
    public required long GeneratedAt { get; init; }
    public required string Model { get; init; }
    public decimal? EstimatedCost { get; init; }
    public decimal? ActualCost { get; init; }
    public int? InputTokens { get; init; }
    public int? OutputTokens { get; init; }
}

/// <summary>
/// Image enrichment metadata referencing a stored image.
/// </summary>
public sealed record ImageEnrichment
{
    public required string ImageId { get; init; }
    public required long GeneratedAt { get; init; }
    public required string Model { get; init; }
    public string? RevisedPrompt { get; init; }
    public decimal? EstimatedCost { get; init; }
    public decimal? ActualCost { get; init; }
    public int? InputTokens { get; init; }
    public int? OutputTokens { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public ImageAspect? Aspect { get; init; }
}

/// <summary>
/// Reference to a generated chronicle stored separately.
/// </summary>
public sealed record ChronicleEnrichmentRef
{
    public required string ChronicleId { get; init; }
    public required long GeneratedAt { get; init; }
    public required string Model { get; init; }
    public decimal? EstimatedCost { get; init; }
    public decimal? ActualCost { get; init; }
    public int? InputTokens { get; init; }
    public int? OutputTokens { get; init; }
}

/// <summary>
/// LLM-ranked style assignments for image generation.
/// </summary>
public sealed record ImageStyleAssignment
{
    public required IReadOnlyList<string> RankedArtisticStyleIds { get; init; }
    public required IReadOnlyList<string> RankedCompositionStyleIds { get; init; }
    public required IReadOnlyList<string> RankedColorPaletteIds { get; init; }
    public required IReadOnlyList<string> VisualTags { get; init; }
    public required string SuggestedArtisticStyleId { get; init; }
    public required string SuggestedCompositionStyleId { get; init; }
    public required string SuggestedColorPaletteId { get; init; }
    public string? SecondaryArtisticStyleId { get; init; }
    public string? SecondaryCompositionStyleId { get; init; }
    public string? SecondaryColorPaletteId { get; init; }
}
