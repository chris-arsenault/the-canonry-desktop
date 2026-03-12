using TheCanonry.Illuminator.Enrichment.Prompts;
using TheCanonry.Illuminator.Types;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

namespace TheCanonry.Illuminator.Enrichment.Tasks;

/// <summary>
/// Standalone visual thesis regeneration (2-step: thesis + traits).
/// Used when description already exists and only visual identity needs updating.
/// </summary>
public sealed class VisualThesisTask : EnrichmentTaskBase
{
    private readonly Func<EntityId, Entity?> _entityResolver;
    private readonly string _kindInstructions;

    public override EnrichmentType TaskType => EnrichmentType.VisualThesis;

    public VisualThesisTask(TaskContext context, Func<EntityId, Entity?> entityResolver,
        string kindInstructions = "Focus on physical form, posture, and distinguishing features.")
        : base(context)
    {
        _entityResolver = entityResolver;
        _kindInstructions = kindInstructions;
    }

    public override async Task<EnrichmentResult> ExecuteAsync(
        EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        var entity = _entityResolver(job.TargetEntityId);
        if (entity is null)
            return new EnrichmentResult { Success = false, Error = $"Entity {job.TargetEntityId} not found" };

        if (string.IsNullOrEmpty(entity.Description))
            return new EnrichmentResult { Success = false, Error = "Entity has no description for visual thesis generation" };

        progress.Report(new TaskProgress("Generating visual thesis...", 0.0));

        var (thesisSys, thesisUser) = DescriptionPrompts.BuildVisualThesisPrompt(
            entity, entity.Description, _kindInstructions);
        var thesisResponse = await CallLlmAsync(LlmCallType.DescriptionVisualThesis, thesisSys, thesisUser, ct);

        if (thesisResponse.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Visual thesis failed: {thesisResponse.Error}" };

        var visualThesis = thesisResponse.Text.Trim();
        if (string.IsNullOrEmpty(visualThesis))
            return new EnrichmentResult { Success = false, Error = "Visual thesis returned empty" };

        progress.Report(new TaskProgress("Generating visual traits...", 0.5));

        var (traitsSys, traitsUser) = DescriptionPrompts.BuildVisualTraitsPrompt(
            entity, visualThesis, entity.Description, _kindInstructions);
        var traitsResponse = await CallLlmAsync(LlmCallType.DescriptionVisualTraits, traitsSys, traitsUser, ct);

        var visualTraits = traitsResponse.Error is null
            ? traitsResponse.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.TrimStart('-', '*', ' ').Trim())
                .Where(l => l.Length > 0).ToList()
            : new List<string>();

        progress.Report(new TaskProgress("Visual thesis complete.", 1.0));

        var totalInput = thesisResponse.Usage.InputTokens + traitsResponse.Usage.InputTokens;
        var totalOutput = thesisResponse.Usage.OutputTokens + traitsResponse.Usage.OutputTokens;

        return new EnrichmentResult
        {
            Success = true,
            Data = new { VisualThesis = visualThesis, VisualTraits = (IReadOnlyList<string>)visualTraits },
            TokenUsage = new ApiClients.Llm.TokenUsage(totalInput, totalOutput),
        };
    }
}
