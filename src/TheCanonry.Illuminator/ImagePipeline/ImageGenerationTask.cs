using TheCanonry.ApiClients.Images;
using TheCanonry.ApiClients.Llm;
using TheCanonry.Illuminator.Enrichment;
using TheCanonry.Illuminator.Enrichment.Tasks;
using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.ImagePipeline;

/// <summary>
/// Enrichment task that generates an image for an entity.
/// Dispatches to IImageClient after optional Claude-based prompt formatting.
/// </summary>
public sealed class ImageGenerationTask : EnrichmentTaskBase
{
    private readonly IImageClient _imageClient;
    private readonly ImagePromptFormatter? _promptFormatter;
    private readonly Func<Schema.Ids.EntityId, string?> _promptResolver;
    private readonly string _imageModel;
    private readonly bool _formatPrompts;

    public override EnrichmentType TaskType => EnrichmentType.Image;

    /// <param name="context">Shared task context with LLM client.</param>
    /// <param name="imageClient">The image generation client to use.</param>
    /// <param name="promptResolver">Resolves entity ID to an image prompt string.</param>
    /// <param name="imageModel">The target image model (e.g. "dall-e-3", "flux-2-pro").</param>
    /// <param name="formatPrompts">Whether to use Claude to reformat prompts before generation.</param>
    public ImageGenerationTask(
        TaskContext context,
        IImageClient imageClient,
        Func<Schema.Ids.EntityId, string?> promptResolver,
        string imageModel = "dall-e-3",
        bool formatPrompts = false)
        : base(context)
    {
        _imageClient = imageClient;
        _promptResolver = promptResolver;
        _imageModel = imageModel;
        _formatPrompts = formatPrompts;
        _promptFormatter = formatPrompts ? new ImagePromptFormatter(context.LlmClient) : null;
    }

    public override async Task<EnrichmentResult> ExecuteAsync(
        EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        var originalPrompt = _promptResolver(job.TargetEntityId);
        if (string.IsNullOrWhiteSpace(originalPrompt))
            return new EnrichmentResult { Success = false, Error = $"No image prompt for entity {job.TargetEntityId}" };

        progress.Report(new TaskProgress("Preparing image prompt...", 0.1));

        // Optional prompt formatting
        var finalPrompt = originalPrompt;
        TokenUsage? formatUsage = null;
        if (_formatPrompts && _promptFormatter is not null)
        {
            var formatResult = await _promptFormatter.FormatAsync(originalPrompt, _imageModel, false, ct);
            finalPrompt = formatResult.Prompt;
            formatUsage = formatResult.Usage;
        }

        // Enforce model-specific prompt length limits
        var family = ImageSettings.GetModelFamily(_imageModel);
        var maxLen = ImageSettings.GetMaxPromptLength(family);
        if (finalPrompt.Length > maxLen)
            finalPrompt = finalPrompt[..maxLen];

        progress.Report(new TaskProgress("Generating image...", 0.3));

        var size = ImageSettings.ResolveSize(_imageModel, "auto");
        var parts = size.Split('x');
        var width = int.Parse(parts[0]);
        var height = int.Parse(parts[1]);

        var request = new ImageRequest
        {
            Prompt = finalPrompt,
            Model = _imageModel,
            Width = width,
            Height = height,
        };

        var result = await _imageClient.GenerateAsync(request, ct);

        if (result.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Image generation failed: {result.Error}" };

        if (result.ImageData is null)
            return new EnrichmentResult { Success = false, Error = "No image data returned from API" };

        progress.Report(new TaskProgress("Image generated.", 1.0));

        // Combine format usage + image usage
        var imageUsage = result.Usage;
        var totalInput = (formatUsage?.InputTokens ?? 0) + (imageUsage?.InputTokens ?? 0);
        var totalOutput = (formatUsage?.OutputTokens ?? 0) + (imageUsage?.OutputTokens ?? 0);

        return new EnrichmentResult
        {
            Success = true,
            Data = new ImageGenerationData
            {
                ImageData = result.ImageData,
                Width = width,
                Height = height,
                Model = _imageModel,
                FinalPrompt = finalPrompt,
                RevisedPrompt = result.RevisedPrompt,
            },
            TokenUsage = new TokenUsage(totalInput, totalOutput),
        };
    }
}

/// <summary>
/// Result data from image generation.
/// </summary>
public sealed record ImageGenerationData
{
    public required byte[] ImageData { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required string Model { get; init; }
    public required string FinalPrompt { get; init; }
    public string? RevisedPrompt { get; init; }
}
