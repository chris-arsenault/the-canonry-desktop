using TheCanonry.ApiClients.Llm;
using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.ImagePipeline;

/// <summary>
/// Optional Claude-based prompt reformatting for image generation.
/// Rewrites entity descriptions into prompts optimized for a specific image model.
/// </summary>
public sealed class ImagePromptFormatter
{
    private readonly ILlmClient _llmClient;

    public ImagePromptFormatter(ILlmClient llmClient)
    {
        _llmClient = llmClient;
    }

    /// <summary>
    /// Reformat an image prompt using Claude. Returns the original prompt if formatting fails.
    /// </summary>
    public async Task<ImagePromptFormatResult> FormatAsync(
        string originalPrompt, string imageModel, bool isChronicleImage, CancellationToken ct)
    {
        var family = ImageSettings.GetModelFamily(imageModel);
        var callType = isChronicleImage
            ? LlmCallType.ImageChronicleFormatting
            : LlmCallType.ImagePromptFormatting;

        var meta = LlmCallDefaults.GetMetadata(callType);
        var request = new LlmRequest
        {
            SystemPrompt = "You are a prompt engineer specializing in image generation. Respond only with the reformatted prompt, no explanations or preamble.",
            UserPrompt = $"Reformat the following description as an optimized prompt for {imageModel} ({family}):\n\n{originalPrompt}",
            Model = meta.DefaultModel,
            MaxTokens = meta.DefaultMaxTokens > 0 ? meta.DefaultMaxTokens : 2048,
            Temperature = 0.3,
        };

        try
        {
            var response = await _llmClient.CompleteAsync(request, ct);
            if (response.Error is null && !string.IsNullOrWhiteSpace(response.Text))
            {
                return new ImagePromptFormatResult
                {
                    Prompt = response.Text.Trim(),
                    WasFormatted = true,
                    Usage = response.Usage,
                };
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Fall through to return original prompt
        }

        return new ImagePromptFormatResult { Prompt = originalPrompt, WasFormatted = false };
    }
}

/// <summary>
/// Result of image prompt formatting.
/// </summary>
public sealed record ImagePromptFormatResult
{
    public required string Prompt { get; init; }
    public bool WasFormatted { get; init; }
    public TokenUsage? Usage { get; init; }
}
