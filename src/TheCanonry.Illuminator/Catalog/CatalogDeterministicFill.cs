namespace TheCanonry.Illuminator.Catalog;

/// <summary>
/// Fills derivable metadata fields without calling an LLM.
///
/// Ported from deterministic fill logic in the TS catalog pipeline.
///
/// Rules applied (only to entries where the field is currently empty):
///   Title          — "{EntityName} — {ImageId}" if missing
///   ArtisticStyleId     — top-1 from ArtisticStyleRanked if available and field empty
///   CompositionStyleId  — top-1 from CompositionStyleRanked if available and field empty
///   ColorPaletteId      — top-1 from ColorPaletteRanked if available and field empty
///
/// Returns new <see cref="ImageCatalogEntry"/> instances (immutable pattern);
/// entries that required no changes are returned unchanged.
/// </summary>
public static class CatalogDeterministicFill
{
    /// <summary>
    /// Fill all derivable fields and return the updated list.
    /// Existing non-null/non-empty values are never overwritten.
    /// </summary>
    public static IReadOnlyList<ImageCatalogEntry> Fill(IReadOnlyList<ImageCatalogEntry> images)
    {
        var result = new ImageCatalogEntry[images.Count];
        for (var i = 0; i < images.Count; i++)
            result[i] = FillEntry(images[i]);
        return result;
    }

    // -------------------------------------------------------------------------
    // Entry-level fill
    // -------------------------------------------------------------------------

    private static ImageCatalogEntry FillEntry(ImageCatalogEntry entry)
    {
        // Work with a mutable copy
        var filled = entry with { };

        // Title — derivable from entity name + image id
        if (string.IsNullOrEmpty(filled.Title))
            filled.Title = $"{filled.EntityName} — {filled.ImageId}";

        // Style fields — fill from ranked lists (top-1)
        if (string.IsNullOrEmpty(filled.ArtisticStyleId)
            && filled.ArtisticStyleRanked is { Count: > 0 })
            filled.ArtisticStyleId = filled.ArtisticStyleRanked[0];

        if (string.IsNullOrEmpty(filled.CompositionStyleId)
            && filled.CompositionStyleRanked is { Count: > 0 })
            filled.CompositionStyleId = filled.CompositionStyleRanked[0];

        if (string.IsNullOrEmpty(filled.ColorPaletteId)
            && filled.ColorPaletteRanked is { Count: > 0 })
            filled.ColorPaletteId = filled.ColorPaletteRanked[0];

        return filled;
    }
}
