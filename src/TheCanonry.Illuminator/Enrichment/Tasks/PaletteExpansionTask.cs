using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Tasks;

/// <summary>
/// Curates visual trait categories for an entity kind with extended reasoning.
/// </summary>
public sealed class PaletteExpansionTask : EnrichmentTaskBase
{
    public override EnrichmentType TaskType => EnrichmentType.PaletteExpansion;

    public PaletteExpansionTask(TaskContext context) : base(context) { }

    public override async Task<EnrichmentResult> ExecuteAsync(
        EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        progress.Report(new TaskProgress("Expanding trait palette...", 0.0));

        var systemPrompt = """
            You curate visual trait categories for procedural world-building entities.
            For each category, define a name, description, and 5-8 example traits.
            Output JSON array of categories with: name, description, examples[].
            Focus on visual diversity — silhouettes, textures, color associations, environmental markers.
            """;
        var userPrompt = $"Expand the visual trait palette for entity kind associated with {job.TargetEntityId}.";

        var response = await CallLlmAsync(LlmCallType.PaletteExpansion, systemPrompt, userPrompt, ct);

        if (response.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Palette expansion failed: {response.Error}" };

        progress.Report(new TaskProgress("Palette expansion complete.", 1.0));

        return new EnrichmentResult
        {
            Success = true,
            Data = new { Categories = response.Text.Trim() },
            TokenUsage = response.Usage,
        };
    }
}
