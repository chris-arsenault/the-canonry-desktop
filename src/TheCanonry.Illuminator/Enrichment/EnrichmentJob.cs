using TheCanonry.ApiClients.Llm;
using TheCanonry.Illuminator.Types;
using TheCanonry.Schema.Ids;

namespace TheCanonry.Illuminator.Enrichment;

/// <summary>
/// A mutable enrichment job that tracks lifecycle state, progress, and cost.
/// </summary>
public sealed class EnrichmentJob
{
    private static long _nextId;

    public long Id { get; }
    public EnrichmentType TaskType { get; }
    public EntityId TargetEntityId { get; }
    public SimulationSlotId SlotId { get; }

    public JobStatus Status { get; private set; }
    public DateTimeOffset QueuedAt { get; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public int InputTokens { get; private set; }
    public int OutputTokens { get; private set; }
    public decimal EstimatedCost { get; set; }

    public string? ErrorMessage { get; private set; }
    public string? ErrorDetail { get; private set; }
    public int AttemptCount { get; private set; }

    public string? ProgressMessage { get; private set; }
    public double? ProgressFraction { get; private set; }

    /// <summary>
    /// Optional typed payload for tasks that need structured input beyond the entity ID.
    /// Tasks cast this to their expected payload type.
    /// </summary>
    public object? Payload { get; init; }

    public EnrichmentJob(EnrichmentType taskType, EntityId targetEntityId, SimulationSlotId slotId)
    {
        Id = Interlocked.Increment(ref _nextId);
        TaskType = taskType;
        TargetEntityId = targetEntityId;
        SlotId = slotId;
        Status = JobStatus.Queued;
        QueuedAt = DateTimeOffset.UtcNow;
    }

    public void MarkRunning()
    {
        Status = JobStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;
        AttemptCount++;
    }

    public void MarkCompleted(TokenUsage? usage)
    {
        Status = JobStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        if (usage is not null)
        {
            InputTokens = usage.InputTokens;
            OutputTokens = usage.OutputTokens;
        }
    }

    public void MarkFailed(Exception ex)
    {
        Status = JobStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        ErrorMessage = ex.Message;
        ErrorDetail = ex.ToString();
    }

    public void MarkCancelled()
    {
        Status = JobStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void ReportProgress(string message, double? fraction = null)
    {
        ProgressMessage = message;
        ProgressFraction = fraction;
    }
}
