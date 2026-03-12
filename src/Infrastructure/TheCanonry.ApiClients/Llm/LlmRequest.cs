namespace TheCanonry.ApiClients.Llm;

/// <summary>
/// A request to an LLM completion endpoint.
/// </summary>
public sealed record LlmRequest
{
    public required string SystemPrompt { get; init; }
    public required string UserPrompt { get; init; }
    public required string Model { get; init; }
    public int MaxTokens { get; init; } = 1024;
    public double Temperature { get; init; } = 1.0;
    public double? TopP { get; init; }

    /// <summary>
    /// When set to a value greater than zero, enables extended thinking with
    /// a budget of this many tokens (Sonnet 4.6 / Opus 4.6 only).
    /// </summary>
    public int? ThinkingBudget { get; init; }

    /// <summary>
    /// When true, bypass SSE streaming and use a single synchronous JSON response.
    /// </summary>
    public bool DisableStreaming { get; init; }
}
