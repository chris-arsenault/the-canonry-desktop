using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Tasks;

/// <summary>
/// Rewrites annotation clauses to vary overused phrases while preserving historian voice.
/// </summary>
public sealed class MotifVariationTask : EnrichmentTaskBase
{
    public override EnrichmentType TaskType => EnrichmentType.MotifVariation;

    public MotifVariationTask(TaskContext context) : base(context) { }

    public override async Task<EnrichmentResult> ExecuteAsync(
        EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        progress.Report(new TaskProgress("Varying overused motifs...", 0.0));

        var systemPrompt = """
            You rewrite annotation phrases to eliminate repetition while preserving
            the historian's established voice. For each phrase, output a varied alternative.
            Output JSON: { "variations": [{ "original": string, "varied": string }] }
            """;
        var userPrompt = $"Vary the overused motif phrases for annotations on entity {job.TargetEntityId}.";

        var response = await CallLlmAsync(LlmCallType.HistorianMotifVariation, systemPrompt, userPrompt, ct);

        if (response.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Motif variation failed: {response.Error}" };

        progress.Report(new TaskProgress("Motif variation complete.", 1.0));

        return new EnrichmentResult
        {
            Success = true,
            Data = new { Variations = response.Text.Trim() },
            TokenUsage = response.Usage,
        };
    }
}
