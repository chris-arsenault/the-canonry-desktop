namespace TheCanonry.ApiClients.Images;

/// <summary>
/// Abstraction over image generation/processing providers.
/// </summary>
public interface IImageClient
{
    ImageProvider Provider { get; }
    bool IsEnabled { get; }
    Task<ImageResult> GenerateAsync(ImageRequest request, CancellationToken ct = default);
}
