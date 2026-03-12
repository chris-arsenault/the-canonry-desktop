namespace TheCanonry.Illuminator.Catalog;

// ─── Coverage Types ───────────────────────────────────────────────────────────

/// <summary>
/// Per-field metadata coverage statistics for an image catalog.
/// </summary>
public sealed record FieldCoverage(
    string FieldName,
    int Present,
    int Missing,
    int Derivable,
    string? DerivableSource);

/// <summary>
/// Full coverage analysis result for a catalog of images.
/// </summary>
public sealed record CoverageReport(
    IReadOnlyList<FieldCoverage> Fields,
    int TotalImages);

// ─── Catalog Entry ────────────────────────────────────────────────────────────

/// <summary>
/// A single entry in the exported image catalog.
/// Mutable metadata fields (title, style assignments, tags) may be filled
/// by deterministic or LLM-based fill passes.
/// </summary>
public sealed record ImageCatalogEntry
{
    public required string ImageId { get; init; }
    public string? Title { get; set; }
    public string? ArtisticStyleId { get; set; }
    public string? CompositionStyleId { get; set; }
    public string? ColorPaletteId { get; set; }
    public IReadOnlyList<string>? Tags { get; set; }
    public required string EntityId { get; init; }
    public required string EntityName { get; init; }
    public required string EntityKind { get; init; }
    public required string Culture { get; init; }
    public required string Model { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required string Aspect { get; init; }
    public required string ThumbPath { get; init; }
    public required string FullPath { get; init; }
    public string? HqPath { get; init; }
    // Style rankings from LLM
    public IReadOnlyList<string>? ArtisticStyleRanked { get; set; }
    public IReadOnlyList<string>? CompositionStyleRanked { get; set; }
    public IReadOnlyList<string>? ColorPaletteRanked { get; set; }
}

// ─── Catalog Document ─────────────────────────────────────────────────────────

/// <summary>
/// The full exported catalog document: versioned, timestamped, with facets.
/// </summary>
public sealed record ImageCatalog(
    int Version,
    DateTime GeneratedAt,
    string BaseUrl,
    IReadOnlyList<ImageCatalogEntry> Images,
    CatalogFacets Facets);

/// <summary>
/// Facet lists for catalog filtering UI.
/// </summary>
public sealed record CatalogFacets(
    IReadOnlyList<string> Styles,
    IReadOnlyList<string> Palettes,
    IReadOnlyList<string> EntityKinds,
    IReadOnlyList<string> Cultures,
    IReadOnlyList<string> Models,
    IReadOnlyList<string> ImageTypes);
