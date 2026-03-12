using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Tasks;

/// <summary>
/// Ranks historian annotation tones by suitability for a chronicle.
/// </summary>
public sealed class ToneRankingTask : EnrichmentTaskBase
{
    public override EnrichmentType TaskType => EnrichmentType.ToneRanking;

    public ToneRankingTask(TaskContext context) : base(context) { }

    public override async Task<EnrichmentResult> ExecuteAsync(
        EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        progress.Report(new TaskProgress("Ranking historian tones...", 0.0));

        var toneNames = Enum.GetNames<HistorianTone>();
        var toneList = string.Join(", ", toneNames.Select(t => t.ToLowerInvariant()));

        var systemPrompt = $"You rank historian annotation tones by suitability for a chronicle's content and themes.\nAvailable tones: {toneList}\nOutput JSON: {{ \"ranking\": [top3 tone names], \"rationale\": string }}";
        var userPrompt = $"Rank the best historian tones for the chronicle associated with entity {job.TargetEntityId}.";

        var response = await CallLlmAsync(LlmCallType.ChronicleToneRanking, systemPrompt, userPrompt, ct);

        if (response.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Tone ranking failed: {response.Error}" };

        progress.Report(new TaskProgress("Tone ranking complete.", 1.0));

        return new EnrichmentResult
        {
            Success = true,
            Data = new { Ranking = response.Text.Trim() },
            TokenUsage = response.Usage,
        };
    }
}
