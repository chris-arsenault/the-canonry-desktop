using TheCanonry.ApiClients.Llm;

namespace TheCanonry.ApiClients.Images;

/// <summary>
/// The result of an image generation or processing request.
/// </summary>
public sealed record ImageResult
{
    /// <summary>
    /// The generated/processed image bytes (PNG). Null on error.
    /// </summary>
    public byte[]? ImageData { get; init; }

    /// <summary>
    /// Revised prompt returned by the provider (DALL-E 3, GPT Image).
    /// </summary>
    public string? RevisedPrompt { get; init; }

    /// <summary>
    /// Token usage (GPT Image models return token counts).
    /// </summary>
    public TokenUsage? Usage { get; init; }

    /// <summary>
    /// Error message when the request failed.
    /// </summary>
    public string? Error { get; init; }
}
