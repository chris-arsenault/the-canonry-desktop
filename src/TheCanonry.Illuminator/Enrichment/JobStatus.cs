namespace TheCanonry.Illuminator.Enrichment;

/// <summary>
/// Lifecycle status of an enrichment job.
/// </summary>
public enum JobStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled,
}
