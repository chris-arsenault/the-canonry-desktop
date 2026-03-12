namespace TheCanonry.Illuminator.BulkOps;

/// <summary>
/// Lifecycle states for a bulk operation, mirroring the TS BulkOperationShell state machine.
/// </summary>
public enum BulkOperationStatus
{
    Idle,
    Confirming,
    Running,
    Complete,
    Failed,
    Cancelled,
}

/// <summary>
/// Progress snapshot reported after each item is processed.
/// </summary>
public sealed record BulkOperationProgress(
    int Total,
    int Processed,
    int Failed,
    BulkOperationStatus Status,
    IReadOnlyList<BulkFailure> Failures);

/// <summary>
/// A single item failure record within a bulk operation.
/// </summary>
public sealed record BulkFailure(
    string ItemId,
    string ItemName,
    string Error);
