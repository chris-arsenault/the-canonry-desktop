namespace TheCanonry.Illuminator.BulkOps;

/// <summary>
/// Chronicle entry prepared for bulk tone ranking.
/// </summary>
public sealed record ToneRankingChronicle(
    string ChronicleId,
    string Title,
    string? Summary,
    string? CurrentTone,
    IReadOnlyList<string> EntityIds);

/// <summary>
/// Bulk tone ranking helpers.
///
/// Ported from apps/illuminator/webui/src/components/BulkToneRankingModal.jsx.
///
/// Prepares a chronicle list for LLM-based tone ranking analysis.
/// Chronicles are sorted by title for stable ordering.
/// </summary>
public static class BulkToneRanking
{
    /// <summary>
    /// Prepare a list of chronicles for tone ranking, sorted by title.
    /// Filters to chronicles that have a title (required for ranking).
    /// </summary>
    public static IReadOnlyList<ToneRankingChronicle> PrepareChronicles(
        IReadOnlyList<ToneRankingChronicle> chronicles)
    {
        return chronicles
            .Where(c => !string.IsNullOrWhiteSpace(c.Title))
            .OrderBy(c => c.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
