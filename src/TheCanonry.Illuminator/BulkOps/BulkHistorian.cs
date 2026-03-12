namespace TheCanonry.Illuminator.BulkOps;

/// <summary>
/// Bulk historian operation types.
/// Ported from apps/illuminator/webui/src/hooks/useBulkHistorian.ts
/// </summary>
public enum BulkHistorianOperation
{
    Review,
    Edition,
    Clear,
}

/// <summary>
/// Bulk historian helpers: tone cycling and entity ordering for the review operation.
///
/// The actual LLM calls are wired in the UI/service layer.
/// This class provides the deterministic logic for tone assignment and sorting.
/// </summary>
public static class BulkHistorian
{
    /// <summary>
    /// Tones assigned cyclically during a bulk review operation.
    /// From the TS constant TONE_CYCLE in useBulkHistorian.ts.
    /// </summary>
    public static readonly string[] ReviewToneCycle =
        ["witty", "weary", "forensic", "elegiac", "cantankerous"];

    /// <summary>
    /// Sort entities by (Kind, Culture, Prominence) and assign tones cyclically
    /// from <see cref="ReviewToneCycle"/>.
    ///
    /// Returns a list of (EntityId, AssignedTone) in sorted order.
    /// </summary>
    public static IReadOnlyList<(string EntityId, string Tone)> AssignReviewTones(
        IReadOnlyList<(string EntityId, string Kind, string Culture, double Prominence)> entities)
    {
        if (entities.Count == 0)
            return Array.Empty<(string, string)>();

        var sorted = entities
            .OrderBy(e => e.Kind, StringComparer.Ordinal)
            .ThenBy(e => e.Culture, StringComparer.Ordinal)
            .ThenBy(e => e.Prominence)
            .ToList();

        var result = new (string EntityId, string Tone)[sorted.Count];
        for (var i = 0; i < sorted.Count; i++)
        {
            result[i] = (sorted[i].EntityId, ReviewToneCycle[i % ReviewToneCycle.Length]);
        }

        return result;
    }
}
