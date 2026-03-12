namespace TheCanonry.Illuminator.BulkOps;

/// <summary>
/// Summary of a single entity prepared for bulk backport processing.
/// </summary>
public sealed record BulkBackportEntitySummary(
    string EntityId,
    string EntityName,
    string EntityKind,
    string EntitySubtype,
    int ChronicleCount);

/// <summary>
/// Bulk backport helpers.
///
/// Ported from apps/illuminator/webui/src/components/BulkBackportModal.jsx and
/// related hooks in the illuminator app.
///
/// Backport processes entities in batches of MAX_BATCH_SIZE, sorted by
/// chronicle count descending (most-referenced first).
/// </summary>
public static class BulkBackport
{
    /// <summary>
    /// Maximum entities processed in a single LLM call during bulk backport.
    /// </summary>
    public const int MaxBatchSize = 8;

    /// <summary>
    /// Sort entities by chronicle count descending (most-referenced entities first)
    /// and split into batches of <see cref="MaxBatchSize"/>.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<BulkBackportEntitySummary>> PrepareBatches(
        IReadOnlyList<BulkBackportEntitySummary> entities)
    {
        if (entities.Count == 0)
            return Array.Empty<IReadOnlyList<BulkBackportEntitySummary>>();

        var sorted = entities
            .OrderByDescending(e => e.ChronicleCount)
            .ToList();

        var batches = new List<IReadOnlyList<BulkBackportEntitySummary>>();
        for (var i = 0; i < sorted.Count; i += MaxBatchSize)
        {
            batches.Add(sorted.GetRange(i, Math.Min(MaxBatchSize, sorted.Count - i)));
        }
        return batches;
    }
}
