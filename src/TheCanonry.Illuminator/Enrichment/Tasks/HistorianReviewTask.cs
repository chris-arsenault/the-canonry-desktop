using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Tasks;

/// <summary>
/// Historian annotation generation for entity descriptions or chronicle narratives.
/// Produces scholarly margin notes anchored to source text.
/// </summary>
public sealed class HistorianReviewTask : EnrichmentTaskBase
{
    public override EnrichmentType TaskType => EnrichmentType.HistorianReview;

    public HistorianReviewTask(TaskContext context) : base(context) { }

    public override async Task<EnrichmentResult> ExecuteAsync(
        EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        progress.Report(new TaskProgress("Generating historian annotations...", 0.0));

        var systemPrompt = """
            You are a world historian annotating source material with margin notes.
            For each note, output JSON with:
            - noteId: string (unique)
            - anchorPhrase: string (exact substring from source text)
            - text: string (the annotation)
            - type: "commentary" | "correction" | "tangent" | "skepticism" | "pedantic" | "temporal"
            Output: { "notes": [...] }
            """;
        var userPrompt = $"Annotate the source text for entity {job.TargetEntityId} with 3-8 historian margin notes.";

        var response = await CallLlmAsync(LlmCallType.HistorianEntityReview, systemPrompt, userPrompt, ct);

        if (response.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Historian review failed: {response.Error}" };

        progress.Report(new TaskProgress("Historian annotations complete.", 1.0));

        return new EnrichmentResult
        {
            Success = true,
            Data = new { Notes = response.Text.Trim() },
            TokenUsage = response.Usage,
        };
    }
}
