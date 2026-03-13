namespace TheCanonry.Illuminator.BulkOps;

/// <summary>
/// Generic sequential bulk operation runner.
///
/// Ported from apps/illuminator/webui/src/components/BulkOperationShell.jsx and related hooks.
///
/// Runs items one at a time (matching browser behavior), tracks success/failure per item,
/// reports progress after each item, and supports cancellation.
/// On item failure: records a BulkFailure and continues to the next item.
/// </summary>
public static class BulkOperationRunner
{
    /// <summary>
    /// Process all items sequentially. Reports progress after each item.
    /// Failures are caught per-item and recorded in the returned progress.
    /// Returns the final BulkOperationProgress.
    /// </summary>
    public static async Task<BulkOperationProgress> RunAsync<T>(
        IReadOnlyList<T> items,
        Func<T, string> idSelector,
        Func<T, string> nameSelector,
        Func<T, CancellationToken, Task> processor,
        IProgress<BulkOperationProgress>? progress = null,
        CancellationToken ct = default)
    {
        var total = items.Count;
        var processed = 0;
        var failed = 0;
        var failures = new List<BulkFailure>();

        void Report(BulkOperationStatus status)
        {
            progress?.Report(new BulkOperationProgress(
                total, processed, failed, status, failures.AsReadOnly()));
        }

        Report(BulkOperationStatus.Running);

        foreach (var item in items)
        {
            if (ct.IsCancellationRequested)
                break;

            var id = idSelector(item);
            var name = nameSelector(item);

            try
            {
                await processor(item, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Honour cancellation — stop processing
                break;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                failed++;
                failures.Add(new BulkFailure(id, name, ex.Message));
            }

            processed++;
            Report(BulkOperationStatus.Running);
        }

        var finalStatus = ct.IsCancellationRequested
            ? BulkOperationStatus.Cancelled
            : failed > 0 && processed == 0
                ? BulkOperationStatus.Failed
                : BulkOperationStatus.Complete;

        var final = new BulkOperationProgress(total, processed, failed, finalStatus, failures.AsReadOnly());
        progress?.Report(final);
        return final;
    }
}
