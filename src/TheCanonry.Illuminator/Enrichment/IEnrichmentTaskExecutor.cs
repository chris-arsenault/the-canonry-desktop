using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment;

/// <summary>
/// Executes a specific enrichment task type against an entity.
/// </summary>
public interface IEnrichmentTaskExecutor
{
    EnrichmentType TaskType { get; }
    Task<EnrichmentResult> ExecuteAsync(EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct);
}
