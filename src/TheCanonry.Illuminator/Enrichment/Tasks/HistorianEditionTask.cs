using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Tasks;

/// <summary>
/// Historian-voiced rewrite of entity description from the full description archive.
/// </summary>
public sealed class HistorianEditionTask : EnrichmentTaskBase
{
    public override EnrichmentType TaskType => EnrichmentType.HistorianEdition;

    public HistorianEditionTask(TaskContext context) : base(context) { }

    public override async Task<EnrichmentResult> ExecuteAsync(
        EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        progress.Report(new TaskProgress("Generating historian edition...", 0.0));

        var systemPrompt = """
            You are a historian writing a scholarly synthesis of an entity's description
            from multiple archived versions. Maintain the historian's established voice.
            Output only the revised description text.
            """;
        var userPrompt = $"Produce a historian-voiced edition for entity {job.TargetEntityId} from its description archive.";

        var response = await CallLlmAsync(LlmCallType.HistorianEdition, systemPrompt, userPrompt, ct);

        if (response.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Historian edition failed: {response.Error}" };

        progress.Report(new TaskProgress("Historian edition complete.", 1.0));

        return new EnrichmentResult
        {
            Success = true,
            Data = new { RevisedDescription = response.Text.Trim() },
            TokenUsage = response.Usage,
        };
    }
}
