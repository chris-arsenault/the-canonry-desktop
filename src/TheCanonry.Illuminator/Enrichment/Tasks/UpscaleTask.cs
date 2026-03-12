using TheCanonry.ApiClients.Images;
using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Tasks;

/// <summary>
/// Upscales an existing image using an image processing API (fal.ai upscale, etc.).
/// </summary>
public sealed class UpscaleTask : EnrichmentTaskBase
{
    private readonly IImageClient? _upscaleClient;

    public override EnrichmentType TaskType => EnrichmentType.Upscale;

    public UpscaleTask(TaskContext context, IImageClient? upscaleClient = null)
        : base(context)
    {
        _upscaleClient = upscaleClient;
    }

    public override async Task<EnrichmentResult> ExecuteAsync(
        EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        if (_upscaleClient is null)
            return new EnrichmentResult { Success = false, Error = "No upscale client configured" };

        progress.Report(new TaskProgress("Upscaling image...", 0.0));

        var request = new ImageRequest
        {
            Prompt = $"upscale entity {job.TargetEntityId}",
            Model = "fal-upscale",
            Width = 2048,
            Height = 2048,
        };

        var result = await _upscaleClient.GenerateAsync(request, ct);

        if (result.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Upscale failed: {result.Error}" };

        if (result.ImageData is null)
            return new EnrichmentResult { Success = false, Error = "Upscale returned no image data" };

        progress.Report(new TaskProgress("Upscale complete.", 1.0));

        return new EnrichmentResult
        {
            Success = true,
            Data = new { ImageData = result.ImageData, Width = 2048, Height = 2048 },
            TokenUsage = result.Usage,
        };
    }
}
