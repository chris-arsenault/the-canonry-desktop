namespace TheCanonry.ApiClients.Images;

/// <summary>
/// A request to an image generation or processing endpoint.
/// </summary>
public sealed record ImageRequest
{
    public required string Prompt { get; init; }
    public int Width { get; init; } = 1024;
    public int Height { get; init; } = 1024;
    public required string Model { get; init; }
    public string? Quality { get; init; }
    public string? AspectRatio { get; init; }

    /// <summary>
    /// Provider-specific parameters (e.g. creativity, resemblance for upscale models).
    /// </summary>
    public Dictionary<string, object>? AdditionalParams { get; init; }
}
