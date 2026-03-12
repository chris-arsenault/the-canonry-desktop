namespace TheCanonry.Illuminator.Config;

/// <summary>
/// Image generation settings (model, dimensions, quality, etc.)
/// </summary>
public sealed class ImageGenerationConfig
{
    public string Model { get; set; } = "fal-ai/flux-pro/v1.1-ultra";
    public int Width { get; set; } = 1024;
    public int Height { get; set; } = 768;
    public string AspectRatio { get; set; } = "4:3";
    public int NumInferenceSteps { get; set; } = 28;
    public double GuidanceScale { get; set; } = 3.5;
    public bool SafetyTolerance { get; set; } = true;
}
