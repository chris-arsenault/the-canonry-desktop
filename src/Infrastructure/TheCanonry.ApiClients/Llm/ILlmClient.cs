namespace TheCanonry.ApiClients.Llm;

/// <summary>
/// Abstraction over LLM completion providers (Anthropic Claude, etc.).
/// </summary>
public interface ILlmClient
{
    Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default);
    IAsyncEnumerable<LlmChunk> StreamAsync(LlmRequest request, CancellationToken ct = default);
}
