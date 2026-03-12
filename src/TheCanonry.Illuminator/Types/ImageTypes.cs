namespace TheCanonry.Illuminator.Types;

/// <summary>
/// Aspect ratio classification for generated images.
/// </summary>
public enum ImageAspect
{
    Portrait,
    Landscape,
    Square,
}

/// <summary>
/// The purpose/context of a generated image.
/// </summary>
public enum ImageType
{
    Entity,
    Scene,
    Cover,
    Chronicle,
    EraNarrative,
    Other,
}

/// <summary>
/// A generated image record with metadata.
/// </summary>
public sealed record ImageRecord
{
    public required string Id { get; init; }
    public required string EntityId { get; init; }
    public required string Prompt { get; init; }
    public required string Model { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required ImageAspect Aspect { get; init; }
    public required ImageType Type { get; init; }
    public string? FilePath { get; init; }
    public required long CreatedAt { get; init; }
    public string? RevisedPrompt { get; init; }
    public decimal? EstimatedCost { get; init; }
    public decimal? ActualCost { get; init; }
    public int? InputTokens { get; init; }
    public int? OutputTokens { get; init; }
    public string? ArtisticStyleId { get; init; }
    public string? CompositionStyleId { get; init; }
    public string? ColorPaletteId { get; init; }
    public string? Title { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
}
