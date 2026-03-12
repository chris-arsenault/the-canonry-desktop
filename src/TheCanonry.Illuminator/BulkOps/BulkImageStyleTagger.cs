namespace TheCanonry.Illuminator.BulkOps;

using TheCanonry.Illuminator.Catalog;

/// <summary>
/// Bulk image style tagging helpers.
///
/// Ported from apps/illuminator/webui/src/hooks/useEntityBulkImageOperations.ts
///
/// Provides batch preparation for style tagging operations.
/// Actual task dispatch and DB writes are handled by the service/UI layer.
/// </summary>
public static class BulkImageStyleTagger
{
    /// <summary>
    /// Maximum items per dispatch batch when tagging styles.
    /// </summary>
    public const int TagStylesBatchSize = 50;

    /// <summary>
    /// Split a list of entity IDs into batches of <see cref="TagStylesBatchSize"/> for dispatch.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<string>> PrepareTagStyleBatches(
        IReadOnlyList<string> entityIds)
    {
        if (entityIds.Count == 0)
            return Array.Empty<IReadOnlyList<string>>();

        var batches = new List<IReadOnlyList<string>>();
        for (var i = 0; i < entityIds.Count; i += TagStylesBatchSize)
        {
            var batch = entityIds
                .Skip(i)
                .Take(TagStylesBatchSize)
                .ToList();
            batches.Add(batch);
        }
        return batches;
    }

    /// <summary>
    /// Assign primary styles using the deterministic distribution balancer.
    /// Delegates to <see cref="ImageStyleAssignment.AssignPrimary"/>.
    /// </summary>
    public static IReadOnlyList<StyleAssignmentResult> AssignPrimaryStyles(
        IReadOnlyList<StyleAssignmentInput> items) =>
        ImageStyleAssignment.AssignPrimary(items);

    /// <summary>
    /// Assign secondary styles using the pair-novelty greedy algorithm.
    /// Delegates to <see cref="ImageStyleAssignment.AssignSecondary"/>.
    /// </summary>
    public static IReadOnlyList<StyleAssignmentResult> AssignSecondaryStyles(
        IReadOnlyList<StyleAssignmentResult> primaryResults,
        IReadOnlyList<StyleAssignmentInput> items) =>
        ImageStyleAssignment.AssignSecondary(primaryResults, items);
}
