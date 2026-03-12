using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheCanonry.ApiClients.Llm;

/// <summary>
/// Claude LLM client that calls the Anthropic Messages API directly.
/// Supports both synchronous JSON and SSE streaming modes.
/// </summary>
public sealed class ClaudeLlmClient : ILlmClient
{
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";
    private const int MaxRetries = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ConcurrentDictionary<string, LlmResponse> _cache = new();

    public ClaudeLlmClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
    {
        var cacheKey = ComputeCacheKey(request);
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return cached with { Cached = true };
        }

        LlmResponse? lastResponse = null;

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            using var httpRequest = BuildHttpRequest(request, streaming: false);
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(httpRequest, ct);
            }
            catch (Exception ex) when (attempt < MaxRetries && IsTransient(ex))
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt), ct);
                continue;
            }

            if (IsRetryableStatus(response.StatusCode) && attempt < MaxRetries)
            {
                response.Dispose();
                await Task.Delay(TimeSpan.FromSeconds(attempt), ct);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(ct);
                response.Dispose();
                lastResponse = new LlmResponse
                {
                    Text = "",
                    Usage = new TokenUsage(0, 0),
                    Error = $"API error {(int)response.StatusCode}: {errorText}",
                    RequestId = ExtractRequestId(response),
                };
                if (attempt < MaxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(attempt), ct);
                    continue;
                }
                return lastResponse;
            }

            var result = await ParseJsonResponse(response, ct);
            response.Dispose();

            _cache[cacheKey] = result;
            return result;
        }

        return lastResponse ?? new LlmResponse
        {
            Text = "",
            Usage = new TokenUsage(0, 0),
            Error = "Max retries exceeded",
        };
    }

    public async IAsyncEnumerable<LlmChunk> StreamAsync(
        LlmRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var httpRequest = BuildHttpRequest(request, streaming: true);
        using var response = await _httpClient.SendAsync(
            httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync(ct);
            yield return new LlmChunk(LlmChunkType.Error, errorText);
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var eventType = "";
        string? line;

        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            ct.ThrowIfCancellationRequested();

            if (line.StartsWith("event: ", StringComparison.Ordinal))
            {
                eventType = line[7..].Trim();
                continue;
            }

            if (!line.StartsWith("data: ", StringComparison.Ordinal)) continue;

            var data = line[6..];
            if (data == "[DONE]") continue;

            JsonDocument? doc = null;
            try
            {
                doc = JsonDocument.Parse(data);
            }
            catch (JsonException)
            {
                continue;
            }

            using (doc)
            {
                var root = doc.RootElement;

                switch (eventType)
                {
                    case "content_block_delta":
                    {
                        if (!root.TryGetProperty("delta", out var delta)) break;
                        var deltaType = delta.TryGetProperty("type", out var t) ? t.GetString() : null;
                        if (deltaType == "text_delta")
                        {
                            var text = delta.TryGetProperty("text", out var tv) ? tv.GetString() ?? "" : "";
                            yield return new LlmChunk(LlmChunkType.Text, text);
                        }
                        else if (deltaType == "thinking_delta")
                        {
                            var thinking = delta.TryGetProperty("thinking", out var tk) ? tk.GetString() ?? "" : "";
                            yield return new LlmChunk(LlmChunkType.Thinking, thinking);
                        }
                        break;
                    }
                    case "message_stop":
                        yield return new LlmChunk(LlmChunkType.Done, "");
                        yield break;
                    case "error":
                    {
                        var msg = root.TryGetProperty("error", out var err)
                            ? (err.TryGetProperty("message", out var em) ? em.GetString() ?? data : data)
                            : data;
                        yield return new LlmChunk(LlmChunkType.Error, msg);
                        yield break;
                    }
                }
            }
        }
    }

    internal HttpRequestMessage BuildHttpRequest(LlmRequest request, bool streaming)
    {
        var bodyObj = new Dictionary<string, object?>
        {
            ["model"] = request.Model,
            ["max_tokens"] = request.MaxTokens,
            ["system"] = new[] { new { type = "text", text = request.SystemPrompt } },
            ["messages"] = new[] { new { role = "user", content = request.UserPrompt } },
        };

        var useThinking = request.ThinkingBudget is > 0;

        if (useThinking)
        {
            bodyObj["thinking"] = new { type = "enabled", budget_tokens = request.ThinkingBudget!.Value };
            // temperature must be 1 when thinking is enabled; omit it to use API default
        }
        else
        {
            bodyObj["temperature"] = request.Temperature;
        }

        if (request.TopP.HasValue)
        {
            bodyObj["top_p"] = request.TopP.Value;
        }

        if (streaming)
        {
            bodyObj["stream"] = true;
        }

        var json = JsonSerializer.Serialize(bodyObj, JsonOptions);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        httpRequest.Headers.Add("x-api-key", _apiKey);
        httpRequest.Headers.Add("anthropic-version", AnthropicVersion);

        return httpRequest;
    }

    private static async Task<LlmResponse> ParseJsonResponse(HttpResponseMessage response, CancellationToken ct)
    {
        var rawJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        var text = new StringBuilder();
        var thinking = new StringBuilder();

        if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var block in content.EnumerateArray())
            {
                var blockType = block.TryGetProperty("type", out var bt) ? bt.GetString() : null;
                if (blockType == "text" && block.TryGetProperty("text", out var tv))
                    text.Append(tv.GetString());
                else if (blockType == "thinking" && block.TryGetProperty("thinking", out var tk))
                    thinking.Append(tk.GetString());
            }
        }

        var inputTokens = 0;
        var outputTokens = 0;
        if (root.TryGetProperty("usage", out var usage))
        {
            if (usage.TryGetProperty("input_tokens", out var it)) inputTokens = it.GetInt32();
            if (usage.TryGetProperty("output_tokens", out var ot)) outputTokens = ot.GetInt32();
        }

        var requestId = ExtractRequestId(response);

        return new LlmResponse
        {
            Text = text.ToString(),
            Thinking = thinking.Length > 0 ? thinking.ToString() : null,
            Usage = new TokenUsage(inputTokens, outputTokens),
            Cached = false,
            RequestId = requestId,
        };
    }

    private static string ComputeCacheKey(LlmRequest request)
    {
        var payload = $"{request.Model}|{request.SystemPrompt}|{request.UserPrompt}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexStringLower(hash);
    }

    private static string? ExtractRequestId(HttpResponseMessage response)
    {
        string[] headerNames = ["request-id", "x-request-id", "anthropic-request-id"];
        foreach (var name in headerNames)
        {
            if (response.Headers.TryGetValues(name, out var values))
            {
                var val = values.FirstOrDefault();
                if (val is not null) return val;
            }
        }
        return null;
    }

    private static bool IsRetryableStatus(HttpStatusCode status) =>
        status is HttpStatusCode.TooManyRequests or (HttpStatusCode)529;

    private static bool IsTransient(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException;
}
