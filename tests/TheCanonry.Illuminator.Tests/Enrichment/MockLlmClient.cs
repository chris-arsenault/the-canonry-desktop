using TheCanonry.ApiClients.Llm;

namespace TheCanonry.Illuminator.Tests.Enrichment;

/// <summary>
/// A mock ILlmClient that returns configured responses in sequence.
/// Each call to CompleteAsync returns the next queued response.
/// </summary>
public sealed class MockLlmClient : ILlmClient
{
    private readonly Queue<LlmResponse> _responses = new();
    private readonly List<LlmRequest> _requests = [];

    public IReadOnlyList<LlmRequest> Requests => _requests;

    public MockLlmClient Enqueue(string text, int inputTokens = 10, int outputTokens = 20) =>
        Enqueue(new LlmResponse
        {
            Text = text,
            Usage = new TokenUsage(inputTokens, outputTokens),
        });

    public MockLlmClient EnqueueError(string error) =>
        Enqueue(new LlmResponse
        {
            Text = "",
            Usage = new TokenUsage(0, 0),
            Error = error,
        });

    public MockLlmClient Enqueue(LlmResponse response)
    {
        _responses.Enqueue(response);
        return this;
    }

    public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
    {
        _requests.Add(request);
        if (_responses.Count == 0)
            throw new InvalidOperationException("MockLlmClient: no more responses queued");
        return Task.FromResult(_responses.Dequeue());
    }

    public IAsyncEnumerable<LlmChunk> StreamAsync(LlmRequest request, CancellationToken ct = default)
    {
        throw new NotSupportedException("MockLlmClient does not support streaming");
    }
}
