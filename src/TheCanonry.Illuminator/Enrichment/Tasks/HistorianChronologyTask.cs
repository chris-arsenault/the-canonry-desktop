using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Tasks;

/// <summary>
/// Historian assigns chronological years to chronicles within an era.
/// </summary>
public sealed class HistorianChronologyTask : EnrichmentTaskBase
{
    public override EnrichmentType TaskType => EnrichmentType.HistorianChronology;

    public HistorianChronologyTask(TaskContext context) : base(context) { }

    public override async Task<EnrichmentResult> ExecuteAsync(
        EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        progress.Report(new TaskProgress("Assigning chronological years...", 0.0));

        var systemPrompt = """
            You are a historian establishing chronological ordering within an era.
            For each chronicle, assign a year number and reasoning.
            Output JSON: { "chronology": [{ "chronicleId": string, "year": number, "reasoning": string }] }
            """;
        var userPrompt = $"Assign chronological years to chronicles in the era containing entity {job.TargetEntityId}.";

        var response = await CallLlmAsync(LlmCallType.HistorianChronology, systemPrompt, userPrompt, ct);

        if (response.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Chronology assignment failed: {response.Error}" };

        progress.Report(new TaskProgress("Chronology assignment complete.", 1.0));

        return new EnrichmentResult
        {
            Success = true,
            Data = new { Chronology = response.Text.Trim() },
            TokenUsage = response.Usage,
        };
    }
}
