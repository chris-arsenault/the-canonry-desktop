using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Tasks;

/// <summary>
/// Batch tags entities with artistic style, composition, and color palette rankings.
/// </summary>
public sealed class EntityTagImageStylesTask : EnrichmentTaskBase
{
    public override EnrichmentType TaskType => EnrichmentType.EntityTagImageStyles;

    public EntityTagImageStylesTask(TaskContext context) : base(context) { }

    public override async Task<EnrichmentResult> ExecuteAsync(
        EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        progress.Report(new TaskProgress("Tagging entity image styles...", 0.0));

        var systemPrompt = """
            You assign artistic style, composition, and color palette rankings to entity images.
            For each entity, rank the top 5 of each category and suggest primary + secondary selections.
            Output JSON: {
              "entities": [{
                "entityId": string,
                "rankedArtisticStyleIds": string[],
                "rankedCompositionStyleIds": string[],
                "rankedColorPaletteIds": string[],
                "visualTags": string[],
                "suggestedArtisticStyleId": string,
                "suggestedCompositionStyleId": string,
                "suggestedColorPaletteId": string
              }]
            }
            """;
        var userPrompt = $"Tag image styles for entity {job.TargetEntityId} based on their visual thesis and description.";

        var response = await CallLlmAsync(LlmCallType.EntityBatchTagImageStyles, systemPrompt, userPrompt, ct);

        if (response.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Image style tagging failed: {response.Error}" };

        progress.Report(new TaskProgress("Image style tagging complete.", 1.0));

        return new EnrichmentResult
        {
            Success = true,
            Data = new { Styles = response.Text.Trim() },
            TokenUsage = response.Usage,
        };
    }
}
