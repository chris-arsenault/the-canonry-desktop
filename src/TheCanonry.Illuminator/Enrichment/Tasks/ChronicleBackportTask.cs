using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Tasks;

/// <summary>
/// Extracts lore from published chronicles and backports to entity descriptions.
/// </summary>
public sealed class ChronicleBackportTask : EnrichmentTaskBase
{
    public override EnrichmentType TaskType => EnrichmentType.ChronicleBackport;

    public ChronicleBackportTask(TaskContext context) : base(context) { }

    public override async Task<EnrichmentResult> ExecuteAsync(
        EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        progress.Report(new TaskProgress("Backporting chronicle lore...", 0.0));

        var systemPrompt = """
            You extract lore from published chronicles and weave it into entity descriptions.
            For each entity, output JSON with:
            - entityId: string
            - revisedDescription: string (the updated description incorporating chronicle lore)
            - anchorPhrase: string (the specific phrase from the chronicle that introduced the lore)
            """;
        var userPrompt = $"Backport lore to entity {job.TargetEntityId} from their published chronicles.";

        var response = await CallLlmAsync(LlmCallType.RevisionLoreBackport, systemPrompt, userPrompt, ct);

        if (response.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Lore backport failed: {response.Error}" };

        progress.Report(new TaskProgress("Lore backport complete.", 1.0));

        return new EnrichmentResult
        {
            Success = true,
            Data = new { RevisedText = response.Text.Trim() },
            TokenUsage = response.Usage,
        };
    }
}
