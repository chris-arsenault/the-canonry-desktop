namespace TheCanonry.Persistence.Entities;

public class EnrichmentJobEntity
{
    public long Id { get; set; }
    public required string TaskType { get; set; }
    public required string TargetEntityId { get; set; }
    public required string SlotSimulationRunId { get; set; }
    public required string Status { get; set; }
    public required DateTime QueuedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal EstimatedCost { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDetail { get; set; }
    public required int AttemptCount { get; set; }
    public string? ProgressMessage { get; set; }
    public double? ProgressFraction { get; set; }
}
