namespace TheCanonry.Illuminator.ImagePipeline;

/// <summary>
/// Image model family classification for prompt template selection and size constraints.
/// </summary>
public enum ImageModelFamily
{
    DallE2,
    DallE3,
    GptImage,
    Flux1,
    Flux2,
    WaveSpeed,
    Unknown,
}

/// <summary>
/// Image generation settings and model family resolution.
/// </summary>
public static class ImageSettings
{
    /// <summary>
    /// Classify an image model string into its model family.
    /// </summary>
    public static ImageModelFamily GetModelFamily(string model) => model switch
    {
        "dall-e-2" => ImageModelFamily.DallE2,
        "dall-e-3" => ImageModelFamily.DallE3,
        _ when model.StartsWith("gpt-image", StringComparison.OrdinalIgnoreCase) => ImageModelFamily.GptImage,
        _ when model.StartsWith("flux-2", StringComparison.OrdinalIgnoreCase) => ImageModelFamily.Flux2,
        _ when model.StartsWith("flux-", StringComparison.OrdinalIgnoreCase) => ImageModelFamily.Flux1,
        _ when model.Contains("wavespeed", StringComparison.OrdinalIgnoreCase) ||
               model.Contains("recraft", StringComparison.OrdinalIgnoreCase) ||
               model.Contains("nano-banana", StringComparison.OrdinalIgnoreCase) ||
               model.Contains("seedream", StringComparison.OrdinalIgnoreCase) ||
               model.Contains("qwen-image", StringComparison.OrdinalIgnoreCase) => ImageModelFamily.WaveSpeed,
        _ => ImageModelFamily.Unknown,
    };

    /// <summary>
    /// Maximum prompt length (characters) for a given model family.
    /// Longer prompts are truncated before sending to the API.
    /// </summary>
    public static int GetMaxPromptLength(ImageModelFamily family) => family switch
    {
        ImageModelFamily.DallE2 => 1000,
        ImageModelFamily.DallE3 => 4000,
        ImageModelFamily.GptImage => 32000,
        ImageModelFamily.Flux1 => 4000,
        ImageModelFamily.Flux2 => 8000,
        ImageModelFamily.WaveSpeed => 4000,
        _ => 4000,
    };

    /// <summary>
    /// Default size string for a model family when "auto" is requested.
    /// </summary>
    public static string GetDefaultSize(ImageModelFamily family) => family switch
    {
        ImageModelFamily.DallE2 => "512x512",
        ImageModelFamily.DallE3 => "1024x1024",
        ImageModelFamily.GptImage => "1024x1024",
        ImageModelFamily.Flux1 => "1024x1024",
        ImageModelFamily.Flux2 => "1024x1024",
        ImageModelFamily.WaveSpeed => "1024x1024",
        _ => "1024x1024",
    };

    /// <summary>
    /// Resolve "auto" or named sizes to a concrete WxH string for a model family.
    /// </summary>
    public static string ResolveSize(string model, string requestedSize)
    {
        if (requestedSize != "auto" && requestedSize.Contains('x'))
            return requestedSize;

        var family = GetModelFamily(model);
        return GetDefaultSize(family);
    }
}
