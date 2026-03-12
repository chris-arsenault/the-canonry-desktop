namespace TheCanonry.Illuminator.BulkOps;

/// <summary>
/// Chronicle summary prepared for fact coverage analysis.
/// </summary>
public sealed record FactCoverageChronicle(
    string ChronicleId,
    string Title,
    string? Summary,
    IReadOnlyList<string> EntityIds);

/// <summary>
/// Bulk fact coverage helpers.
///
/// Ported from apps/illuminator/webui/src/components/BulkFactCoverageModal.jsx.
///
/// Prepares chronicle summaries for LLM-based fact coverage analysis.
/// The analysis checks how well canon facts are represented across chronicles.
/// </summary>
public static class BulkFactCoverage
{
    /// <summary>
    /// Build the chronicle list for fact coverage analysis.
    /// Filters to chronicles that have both a title and summary.
    /// </summary>
    public static IReadOnlyList<FactCoverageChronicle> PrepareChronicles(
        IReadOnlyList<FactCoverageChronicle> chronicles)
    {
        return chronicles
            .Where(c => !string.IsNullOrWhiteSpace(c.Title) && !string.IsNullOrWhiteSpace(c.Summary))
            .ToList();
    }
}
