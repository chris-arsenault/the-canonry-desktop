namespace TheCanonry.Illuminator.PrePrint;

/// <summary>
/// Pure computation functions for Pre-Print statistics.
///
/// Ported from apps/illuminator/webui/src/lib/preprint/prePrintStats.ts.
/// Operates on value-tuple collections instead of the full TS record types,
/// keeping the C# API dependency-free.
/// </summary>
public static class PrePrintStatistics
{
    // =========================================================================
    // Word Count
    // =========================================================================

    /// <summary>
    /// Counts words in text by splitting on whitespace.
    /// Returns 0 for null or empty input.
    /// </summary>
    public static int CountWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return text.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    // =========================================================================
    // Word Breakdown
    // =========================================================================

    /// <summary>
    /// Computes a per-content-type word count breakdown.
    /// </summary>
    /// <param name="chronicles">Each element: (body content, optional historian notes).</param>
    /// <param name="entities">Each element: (optional description, optional historian notes).</param>
    /// <param name="eraNarratives">Narrative body text for each completed era narrative.</param>
    /// <param name="staticPages">Page content text for each published static page.</param>
    public static WordCountBreakdown ComputeWordBreakdown(
        IReadOnlyList<(string Content, string? HistorianNotes)> chronicles,
        IReadOnlyList<(string? Description, string? HistorianNotes)> entities,
        IReadOnlyList<string?> eraNarratives,
        IReadOnlyList<string?> staticPages)
    {
        var chronicleWords = chronicles.Sum(c => CountWords(c.Content));
        var chronicleHistorianWords = chronicles.Sum(c => CountWords(c.HistorianNotes));

        var entityWords = entities.Sum(e => CountWords(e.Description));
        var entityHistorianWords = entities.Sum(e => CountWords(e.HistorianNotes));

        var eraNarrativeWords = eraNarratives.Sum(n => CountWords(n));
        var staticPageWords = staticPages.Sum(p => CountWords(p));

        var totalWords = chronicleWords + chronicleHistorianWords
            + entityWords + entityHistorianWords
            + eraNarrativeWords + staticPageWords;

        return new WordCountBreakdown(
            TotalWords: totalWords,
            EntityWords: entityWords,
            ChronicleWords: chronicleWords,
            EraNarrativeWords: eraNarrativeWords,
            StaticPageWords: staticPageWords,
            HistorianWords: entityHistorianWords + chronicleHistorianWords);
    }

    // =========================================================================
    // Image Stats
    // =========================================================================

    /// <summary>
    /// Groups images by aspect and type to produce distribution stats.
    /// </summary>
    /// <param name="images">Each element: (aspect string, type string).</param>
    public static ImageStats ComputeImageStats(IReadOnlyList<(string Aspect, string Type)> images)
    {
        var byAspect = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var byType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var (aspect, type) in images)
        {
            byAspect.TryGetValue(aspect, out var a);
            byAspect[aspect] = a + 1;

            byType.TryGetValue(type, out var t);
            byType[type] = t + 1;
        }

        return new ImageStats(
            TotalImages: images.Count,
            ByAspect: byAspect,
            ByType: byType);
    }

    // =========================================================================
    // Completeness Stats
    // =========================================================================

    /// <summary>
    /// Packages completeness counters (all pre-computed by caller) into a record.
    /// </summary>
    public static CompletenessStats ComputeCompletenessStats(
        int totalEntities, int entitiesWithDescription,
        int totalChronicles, int acceptedChronicles,
        int totalStaticPages, int publishedStaticPages,
        int totalEraNarratives, int completedEraNarratives) =>
        new(
            TotalEntities: totalEntities,
            EntitiesWithDescription: entitiesWithDescription,
            TotalChronicles: totalChronicles,
            AcceptedChronicles: acceptedChronicles,
            TotalStaticPages: totalStaticPages,
            PublishedStaticPages: publishedStaticPages,
            TotalEraNarratives: totalEraNarratives,
            CompletedEraNarratives: completedEraNarratives);

    // =========================================================================
    // Aggregate
    // =========================================================================

    /// <summary>
    /// Computes all stats in one call.
    /// </summary>
    public static PrePrintStats ComputeAll(
        IReadOnlyList<(string Content, string? HistorianNotes)> chronicles,
        IReadOnlyList<(string? Description, string? HistorianNotes)> entities,
        IReadOnlyList<string?> eraNarratives,
        IReadOnlyList<string?> staticPages,
        IReadOnlyList<(string Aspect, string Type)> images,
        int totalEntities, int entitiesWithDescription,
        int totalChronicles, int acceptedChronicles,
        int totalStaticPages, int publishedStaticPages,
        int totalEraNarratives, int completedEraNarratives) =>
        new PrePrintStats(
            WordCounts: ComputeWordBreakdown(chronicles, entities, eraNarratives, staticPages),
            Images: ComputeImageStats(images),
            Completeness: ComputeCompletenessStats(
                totalEntities, entitiesWithDescription,
                totalChronicles, acceptedChronicles,
                totalStaticPages, publishedStaticPages,
                totalEraNarratives, completedEraNarratives));
}
