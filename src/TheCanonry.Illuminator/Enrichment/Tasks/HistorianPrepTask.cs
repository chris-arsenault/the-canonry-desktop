using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Tasks;

/// <summary>
/// Historian reading notes for a chronicle — private observations, thematic threads, cast dynamics.
/// Used as input to era narrative thread synthesis.
/// </summary>
public sealed class HistorianPrepTask : EnrichmentTaskBase
{
    public override EnrichmentType TaskType => EnrichmentType.HistorianPrep;

    public HistorianPrepTask(TaskContext context) : base(context) { }

    public override async Task<EnrichmentResult> ExecuteAsync(
        EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        progress.Report(new TaskProgress("Generating historian prep notes...", 0.0));

        var systemPrompt = """
            You are a historian preparing reading notes for a chronicle.
            Focus on: thematic threads, cast dynamics, cultural observations, and narrative tension.
            Write concisely — these are private working notes, not polished prose.
            Output only the reading notes text.
            """;
        var userPrompt = $"Prepare reading notes for the chronicle associated with entity {job.TargetEntityId}.";

        var response = await CallLlmAsync(LlmCallType.HistorianPrep, systemPrompt, userPrompt, ct);

        if (response.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Historian prep failed: {response.Error}" };

        progress.Report(new TaskProgress("Historian prep complete.", 1.0));

        return new EnrichmentResult
        {
            Success = true,
            Data = new { PrepNotes = response.Text.Trim() },
            TokenUsage = response.Usage,
        };
    }
}
