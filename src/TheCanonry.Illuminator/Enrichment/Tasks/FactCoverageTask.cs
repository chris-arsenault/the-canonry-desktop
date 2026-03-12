using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Tasks;

/// <summary>
/// Classifies canon fact presence in chronicle narratives.
/// Rates each fact as missing, mentioned, prevalent, or integral.
/// </summary>
public sealed class FactCoverageTask : EnrichmentTaskBase
{
    public override EnrichmentType TaskType => EnrichmentType.FactCoverage;

    public FactCoverageTask(TaskContext context) : base(context) { }

    public override async Task<EnrichmentResult> ExecuteAsync(
        EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        progress.Report(new TaskProgress("Analyzing fact coverage...", 0.0));

        var systemPrompt = """
            You classify how thoroughly each canon fact is represented in a chronicle narrative.
            For each fact, assign a rating and provide evidence.
            Output JSON: { "entries": [{ "factId": string, "factText": string, "rating": "missing"|"mentioned"|"prevalent"|"integral", "evidence": string }] }
            """;
        var userPrompt = $"Analyze fact coverage for the chronicle associated with entity {job.TargetEntityId}.";

        var response = await CallLlmAsync(LlmCallType.ChronicleFactCoverage, systemPrompt, userPrompt, ct);

        if (response.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Fact coverage analysis failed: {response.Error}" };

        progress.Report(new TaskProgress("Fact coverage analysis complete.", 1.0));

        return new EnrichmentResult
        {
            Success = true,
            Data = new { Report = response.Text.Trim() },
            TokenUsage = response.Usage,
        };
    }
}
