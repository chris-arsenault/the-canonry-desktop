using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Tasks;

/// <summary>
/// Batch revision of entity summaries/descriptions using world dynamics.
/// </summary>
public sealed class SummaryRevisionTask : EnrichmentTaskBase
{
    public override EnrichmentType TaskType => EnrichmentType.SummaryRevision;

    public SummaryRevisionTask(TaskContext context) : base(context) { }

    public override async Task<EnrichmentResult> ExecuteAsync(
        EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        progress.Report(new TaskProgress("Revising entity summaries...", 0.0));

        var systemPrompt = "You revise entity summaries to incorporate world dynamics. Output JSON with revised summary and description for each entity.";
        var userPrompt = $"Revise the summary and description for entity {job.TargetEntityId} using current world dynamics.";

        var response = await CallLlmAsync(LlmCallType.RevisionSummary, systemPrompt, userPrompt, ct);

        if (response.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Summary revision failed: {response.Error}" };

        progress.Report(new TaskProgress("Summary revision complete.", 1.0));

        return new EnrichmentResult
        {
            Success = true,
            Data = new { RevisedText = response.Text.Trim() },
            TokenUsage = response.Usage,
        };
    }
}
