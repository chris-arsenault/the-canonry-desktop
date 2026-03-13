using TheCanonry.Illuminator.Enrichment.Prompts;
using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Tasks;

/// <summary>
/// Motif variation payload — vary mode: rewrite overused phrases in annotations.
/// </summary>
public sealed record MotifVaryPayload(
    string MotifLabel,
    IReadOnlyList<MotifVariationPrompts.MotifInstance> Instances);

/// <summary>
/// Motif variation payload — weave mode: incorporate target phrase into sentences.
/// </summary>
public sealed record MotifWeavePayload(
    string TargetPhrase,
    IReadOnlyList<MotifVariationPrompts.MotifWeaveInstance> Instances);

/// <summary>
/// Rewrites annotation clauses to vary overused phrases (vary mode)
/// or weaves a motif phrase into existing sentences (weave mode).
/// Mode is determined by the job's Payload type.
/// </summary>
public sealed class MotifVariationTask : EnrichmentTaskBase
{
    public override EnrichmentType TaskType => EnrichmentType.MotifVariation;

    public MotifVariationTask(TaskContext context) : base(context) { }

    public override async Task<EnrichmentResult> ExecuteAsync(
        EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        return job.Payload switch
        {
            MotifWeavePayload weave => await ExecuteWeaveAsync(weave, progress, ct),
            MotifVaryPayload vary => await ExecuteVaryAsync(vary, progress, ct),
            _ => new EnrichmentResult { Success = false, Error = "MotifVariationTask requires a MotifVaryPayload or MotifWeavePayload" },
        };
    }

    private async Task<EnrichmentResult> ExecuteVaryAsync(
        MotifVaryPayload payload, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        if (payload.Instances.Count == 0)
            return new EnrichmentResult { Success = false, Error = "No instances to vary" };

        progress.Report(new TaskProgress("Varying overused motifs...", 0.0));

        var systemPrompt = MotifVariationPrompts.BuildVarySystemPrompt(payload.MotifLabel);
        var userPrompt = MotifVariationPrompts.BuildVaryUserPrompt(payload.Instances);

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

    private async Task<EnrichmentResult> ExecuteWeaveAsync(
        MotifWeavePayload payload, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        if (payload.Instances.Count == 0)
            return new EnrichmentResult { Success = false, Error = "No instances to weave" };

        progress.Report(new TaskProgress("Weaving motif into sentences...", 0.0));

        var systemPrompt = MotifVariationPrompts.BuildWeaveSystemPrompt(payload.TargetPhrase);
        var userPrompt = MotifVariationPrompts.BuildWeaveUserPrompt(payload.Instances);

        var response = await CallLlmAsync(LlmCallType.HistorianMotifVariation, systemPrompt, userPrompt, ct);

        if (response.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Motif weave failed: {response.Error}" };

        progress.Report(new TaskProgress("Motif weave complete.", 1.0));

        return new EnrichmentResult
        {
            Success = true,
            Data = new { Variations = response.Text.Trim() },
            TokenUsage = response.Usage,
        };
    }
}
