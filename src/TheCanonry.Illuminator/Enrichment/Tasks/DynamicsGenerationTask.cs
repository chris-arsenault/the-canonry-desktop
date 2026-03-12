using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Tasks;

/// <summary>
/// Multi-turn world dynamics synthesis from lore and entity data.
/// </summary>
public sealed class DynamicsGenerationTask : EnrichmentTaskBase
{
    public override EnrichmentType TaskType => EnrichmentType.DynamicsGeneration;

    public DynamicsGenerationTask(TaskContext context) : base(context) { }

    public override async Task<EnrichmentResult> ExecuteAsync(
        EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        progress.Report(new TaskProgress("Synthesizing world dynamics...", 0.0));

        var systemPrompt = """
            You synthesize world dynamics — macro-level forces and tensions that shape relationships.
            Output JSON array of dynamics, each with:
            - id: string
            - text: string (the dynamic statement)
            - cultures: string[] (affected cultures, or ["*"] for universal)
            - kinds: string[] (affected entity kinds, or ["*"] for universal)
            """;
        var userPrompt = $"Synthesize world dynamics for the world containing entity {job.TargetEntityId}.";

        var response = await CallLlmAsync(LlmCallType.DynamicsGeneration, systemPrompt, userPrompt, ct);

        if (response.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Dynamics generation failed: {response.Error}" };

        progress.Report(new TaskProgress("World dynamics synthesized.", 1.0));

        return new EnrichmentResult
        {
            Success = true,
            Data = new { Dynamics = response.Text.Trim() },
            TokenUsage = response.Usage,
        };
    }
}
