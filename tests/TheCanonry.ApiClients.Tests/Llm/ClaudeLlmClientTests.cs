using System.Net;
using System.Text.Json;
using TheCanonry.ApiClients.Llm;

namespace TheCanonry.ApiClients.Tests.Llm;

public sealed class ClaudeLlmClientTests
{
    private static LlmRequest MakeRequest(
        string system = "You are a test assistant.",
        string user = "Say hello.",
        string model = LlmModel.Sonnet46,
        int maxTokens = 256,
        int? thinkingBudget = null) =>
        new()
        {
            SystemPrompt = system,
            UserPrompt = user,
            Model = model,
            MaxTokens = maxTokens,
            ThinkingBudget = thinkingBudget,
        };

    private static string MakeJsonResponse(
        string text = "Hello!",
        string? thinking = null,
        int inputTokens = 10,
        int outputTokens = 5)
    {
        var content = new List<object>();
        if (thinking is not null)
            content.Add(new { type = "thinking", thinking });
        content.Add(new { type = "text", text });

        var response = new
        {
            content,
            usage = new { input_tokens = inputTokens, output_tokens = outputTokens },
        };
        return JsonSerializer.Serialize(response);
    }

    [Fact]
    public async Task CompleteAsync_BuildsCorrectRequestBody()
    {
        using var handler = new MockHandler()
            .Respond(HttpStatusCode.OK, MakeJsonResponse());
        using var httpClient = new HttpClient(handler);
        var client = new ClaudeLlmClient(httpClient, "test-key");

        var request = MakeRequest();
        await client.CompleteAsync(request);

        Assert.NotNull(handler.FirstRequestBody);
        using var doc = JsonDocument.Parse(handler.FirstRequestBody);
        var root = doc.RootElement;

        Assert.Equal(LlmModel.Sonnet46, root.GetProperty("model").GetString());
        Assert.Equal(256, root.GetProperty("max_tokens").GetInt32());

        // system should be an array with one text block
        var system = root.GetProperty("system");
        Assert.Equal(JsonValueKind.Array, system.ValueKind);
        Assert.Equal("text", system[0].GetProperty("type").GetString());
        Assert.Equal("You are a test assistant.", system[0].GetProperty("text").GetString());

        // messages array
        var messages = root.GetProperty("messages");
        Assert.Equal("user", messages[0].GetProperty("role").GetString());
        Assert.Equal("Say hello.", messages[0].GetProperty("content").GetString());

        // Verify headers
        var httpRequest = handler.Requests[0];
        Assert.Contains("test-key", httpRequest.Headers.GetValues("x-api-key"));
        Assert.Contains("2023-06-01", httpRequest.Headers.GetValues("anthropic-version"));
    }

    [Fact]
    public async Task CompleteAsync_ParsesNonStreamingResponse()
    {
        using var handler = new MockHandler()
            .Respond(HttpStatusCode.OK, MakeJsonResponse("World hello!", inputTokens: 42, outputTokens: 7));
        using var httpClient = new HttpClient(handler);
        var client = new ClaudeLlmClient(httpClient, "test-key");

        var result = await client.CompleteAsync(MakeRequest());

        Assert.Equal("World hello!", result.Text);
        Assert.Null(result.Thinking);
        Assert.Equal(42, result.Usage.InputTokens);
        Assert.Equal(7, result.Usage.OutputTokens);
        Assert.False(result.Cached);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task CompleteAsync_ParsesThinkingResponse()
    {
        using var handler = new MockHandler()
            .Respond(HttpStatusCode.OK, MakeJsonResponse(
                text: "The answer is 42.",
                thinking: "Let me think about this carefully...",
                inputTokens: 50,
                outputTokens: 20));
        using var httpClient = new HttpClient(handler);
        var client = new ClaudeLlmClient(httpClient, "test-key");

        var result = await client.CompleteAsync(MakeRequest(thinkingBudget: 4096));

        Assert.Equal("The answer is 42.", result.Text);
        Assert.Equal("Let me think about this carefully...", result.Thinking);
        Assert.Equal(50, result.Usage.InputTokens);
        Assert.Equal(20, result.Usage.OutputTokens);
    }

    [Fact]
    public async Task CompleteAsync_ThinkingBudget_IncludedInRequestBody()
    {
        using var handler = new MockHandler()
            .Respond(HttpStatusCode.OK, MakeJsonResponse());
        using var httpClient = new HttpClient(handler);
        var client = new ClaudeLlmClient(httpClient, "test-key");

        await client.CompleteAsync(MakeRequest(thinkingBudget: 8192));

        Assert.NotNull(handler.FirstRequestBody);
        using var doc = JsonDocument.Parse(handler.FirstRequestBody);
        var root = doc.RootElement;

        var thinking = root.GetProperty("thinking");
        Assert.Equal("enabled", thinking.GetProperty("type").GetString());
        Assert.Equal(8192, thinking.GetProperty("budget_tokens").GetInt32());

        // temperature should NOT be present when thinking is enabled
        Assert.False(root.TryGetProperty("temperature", out _));
    }

    [Fact]
    public async Task CompleteAsync_RetriesOn429()
    {
        using var handler = new MockHandler()
            .Respond(HttpStatusCode.TooManyRequests, """{"error":{"message":"rate limited"}}""")
            .Respond(HttpStatusCode.OK, MakeJsonResponse("Success after retry"));
        using var httpClient = new HttpClient(handler);
        var client = new ClaudeLlmClient(httpClient, "test-key");

        var result = await client.CompleteAsync(MakeRequest());

        Assert.Equal("Success after retry", result.Text);
        Assert.Null(result.Error);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task CompleteAsync_CacheHitReturnsSameResponse()
    {
        using var handler = new MockHandler()
            .Respond(HttpStatusCode.OK, MakeJsonResponse("Cached response", inputTokens: 10, outputTokens: 3));
        using var httpClient = new HttpClient(handler);
        var client = new ClaudeLlmClient(httpClient, "test-key");

        var request = MakeRequest();
        var firstResult = await client.CompleteAsync(request);
        var secondResult = await client.CompleteAsync(request);

        Assert.Equal("Cached response", firstResult.Text);
        Assert.False(firstResult.Cached);
        Assert.Equal("Cached response", secondResult.Text);
        Assert.True(secondResult.Cached);

        // Only one HTTP request should have been made
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task CompleteAsync_ReturnsErrorAfterAllRetriesExhausted()
    {
        using var handler = new MockHandler()
            .Respond(HttpStatusCode.TooManyRequests, """{"error":"rate limited"}""")
            .Respond(HttpStatusCode.TooManyRequests, """{"error":"rate limited"}""")
            .Respond(HttpStatusCode.TooManyRequests, """{"error":"rate limited"}""");
        using var httpClient = new HttpClient(handler);
        var client = new ClaudeLlmClient(httpClient, "test-key");

        var result = await client.CompleteAsync(MakeRequest());

        Assert.NotNull(result.Error);
        Assert.Contains("429", result.Error);
        Assert.Equal(3, handler.Requests.Count);
    }
}
