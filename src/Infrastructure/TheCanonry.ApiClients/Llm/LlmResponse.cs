namespace TheCanonry.ApiClients.Llm;

/// <summary>
/// The result of a non-streaming LLM completion.
/// </summary>
public sealed record LlmResponse
{
    public required string Text { get; init; }
    public string? Thinking { get; init; }
    public required TokenUsage Usage { get; init; }
    public bool Cached { get; init; }
    public string? Error { get; init; }
    public string? RequestId { get; init; }
}
