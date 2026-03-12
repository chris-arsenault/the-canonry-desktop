using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Tasks;

/// <summary>
/// Era narrative generation task — synthesizes a long-form era narrative
/// from chronicle prep briefs through a threads -> generate -> edit pipeline.
/// </summary>
public sealed class EraNarrativeTask : EnrichmentTaskBase
{
    public override EnrichmentType TaskType => EnrichmentType.EraNarrative;

    public EraNarrativeTask(TaskContext context) : base(context) { }

    public override async Task<EnrichmentResult> ExecuteAsync(
        EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        progress.Report(new TaskProgress("Synthesizing era narrative threads...", 0.0));

        var threadsSys = "You are a historian synthesizing thematic threads from chronicle prep briefs. Output JSON with threads, thesis, and counterweight.";
        var threadsUser = $"Synthesize the major threads for era narrative. Entity context: {job.TargetEntityId}";

        var threadsResponse = await CallLlmAsync(LlmCallType.HistorianEraNarrativeThreads, threadsSys, threadsUser, ct);
        if (threadsResponse.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Thread synthesis failed: {threadsResponse.Error}" };

        progress.Report(new TaskProgress("Generating era narrative prose...", 0.33));

        var genSys = "You are a historian writing a mythic-historical era narrative (5000-7000 words). Write from the thread synthesis.";
        var genUser = $"THREADS:\n{threadsResponse.Text}\n\nWrite the era narrative.";

        var genResponse = await CallLlmAsync(LlmCallType.HistorianEraNarrativeGenerate, genSys, genUser, ct);
        if (genResponse.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Prose generation failed: {genResponse.Error}" };

        progress.Report(new TaskProgress("Copy-editing era narrative...", 0.66));

        var editSys = "You are an editor tightening prose, cutting modern register, and strengthening mythic voice. Output only the revised text.";
        var editUser = genResponse.Text;

        var editResponse = await CallLlmAsync(LlmCallType.HistorianEraNarrativeEdit, editSys, editUser, ct);

        var finalContent = editResponse.Error is null ? editResponse.Text.Trim() : genResponse.Text.Trim();

        progress.Report(new TaskProgress("Era narrative complete.", 1.0));

        var totalInput = threadsResponse.Usage.InputTokens + genResponse.Usage.InputTokens + editResponse.Usage.InputTokens;
        var totalOutput = threadsResponse.Usage.OutputTokens + genResponse.Usage.OutputTokens + editResponse.Usage.OutputTokens;

        return new EnrichmentResult
        {
            Success = true,
            Data = new { Content = finalContent, ThreadSynthesis = threadsResponse.Text },
            TokenUsage = new ApiClients.Llm.TokenUsage(totalInput, totalOutput),
        };
    }
}
