namespace TheCanonry.Illuminator.Catalog;

/// <summary>
/// Analyzes per-field metadata coverage for a catalog of images.
///
/// Ported from apps/illuminator/webui/src/lib/catalogAnalysis.ts.
///
/// For each tracked field, reports:
///   Present   — entry has a non-null, non-empty value
///   Missing   — entry lacks the value
///   Derivable — missing entry can be filled without LLM (from other data on the entry)
///
/// Fields tracked: title, tags, artisticStyleId, compositionStyleId, colorPaletteId
/// </summary>
public static class CatalogAnalysis
{
    /// <summary>
    /// Produces a <see cref="CoverageReport"/> summarizing how completely
    /// the tracked metadata fields are filled across all images.
    /// </summary>
    public static CoverageReport AnalyzeCoverage(IReadOnlyList<ImageCatalogEntry> images)
    {
        var fields = new List<FieldCoverage>
        {
            AnalyzeField(
                images,
                "title",
                img => img.Title,
                // Title is always derivable — can be built from EntityName + ImageId
                _ => true,
                "entityName + imageId pattern"),

            AnalyzeField(
                images,
                "tags",
                img => img.Tags is { Count: > 0 } ? img.Tags : null,
                // Tags are derivable if the image has a ranked palette (proxy for having style context)
                img => img.ColorPaletteRanked is { Count: > 0 }
                    || img.ArtisticStyleRanked is { Count: > 0 },
                "ranked style lists"),

            AnalyzeField(
                images,
                "artisticStyleId",
                img => img.ArtisticStyleId,
                img => img.ArtisticStyleRanked is { Count: > 0 },
                "artisticStyleRanked top-1"),

            AnalyzeField(
                images,
                "compositionStyleId",
                img => img.CompositionStyleId,
                img => img.CompositionStyleRanked is { Count: > 0 },
                "compositionStyleRanked top-1"),

            AnalyzeField(
                images,
                "colorPaletteId",
                img => img.ColorPaletteId,
                img => img.ColorPaletteRanked is { Count: > 0 },
                "colorPaletteRanked top-1"),
        };

        return new CoverageReport(fields, images.Count);
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    private static bool IsPresent(object? val)
    {
        if (val is null) return false;
        if (val is string s) return s.Length > 0;
        if (val is IReadOnlyList<string> list) return list.Count > 0;
        return true;
    }

    private static FieldCoverage AnalyzeField(
        IReadOnlyList<ImageCatalogEntry> images,
        string fieldName,
        Func<ImageCatalogEntry, object?> getValue,
        Func<ImageCatalogEntry, bool> canDerive,
        string derivableSource)
    {
        var present = 0;
        var missing = 0;
        var derivable = 0;

        foreach (var img in images)
        {
            var val = getValue(img);
            var hasValue = IsPresent(val);

            if (hasValue)
            {
                present++;
            }
            else
            {
                missing++;
                if (canDerive(img))
                    derivable++;
            }
        }

        return new FieldCoverage(fieldName, present, missing, derivable, derivableSource);
    }
}
