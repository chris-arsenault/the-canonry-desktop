using System.Text;
using System.Text.Json;
using TheCanonry.ApiClients.Llm;

namespace TheCanonry.ApiClients.Images;

/// <summary>
/// Image generation client for OpenAI DALL-E 2/3 and GPT Image models.
/// </summary>
public sealed class DalleImageClient : IImageClient
{
    private const string ApiUrl = "https://api.openai.com/v1/images/generations";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public DalleImageClient(HttpClient httpClient, string apiKey, string model = "dall-e-3")
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _model = model;
    }

    public ImageProvider Provider => _model switch
    {
        "dall-e-2" => ImageProvider.DallE2,
        "dall-e-3" => ImageProvider.DallE3,
        _ when _model.StartsWith("gpt-image", StringComparison.OrdinalIgnoreCase) => ImageProvider.GptImage,
        _ => ImageProvider.DallE3,
    };

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<ImageResult> GenerateAsync(ImageRequest request, CancellationToken ct = default)
    {
        if (!IsEnabled)
            return new ImageResult { Error = "Client disabled - missing API key" };

        var body = BuildRequestBody(request);
        var json = JsonSerializer.Serialize(body);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(httpRequest, ct);
        }
        catch (Exception ex)
        {
            return new ImageResult { Error = ex.Message };
        }

        using (response)
        {
            var responseText = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                return new ImageResult { Error = $"API error {(int)response.StatusCode}: {responseText}" };

            return ParseResponse(responseText);
        }
    }

    internal Dictionary<string, object?> BuildRequestBody(ImageRequest request)
    {
        var isGptImage = _model.StartsWith("gpt-image", StringComparison.OrdinalIgnoreCase);
        var size = $"{request.Width}x{request.Height}";

        var body = new Dictionary<string, object?>
        {
            ["model"] = _model,
            ["prompt"] = request.Prompt,
            ["n"] = 1,
        };

        if (size != "0x0")
            body["size"] = size;

        if (request.Quality is not null)
            body["quality"] = request.Quality;

        if (!isGptImage)
            body["response_format"] = "b64_json";

        return body;
    }

    private static ImageResult ParseResponse(string responseText)
    {
        using var doc = JsonDocument.Parse(responseText);
        var root = doc.RootElement;

        string? b64Data = null;
        string? revisedPrompt = null;

        if (root.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.Array &&
            data.GetArrayLength() > 0)
        {
            var first = data[0];
            if (first.TryGetProperty("b64_json", out var b64))
                b64Data = b64.GetString();
            if (first.TryGetProperty("revised_prompt", out var rp))
                revisedPrompt = rp.GetString();
        }

        TokenUsage? usage = null;
        if (root.TryGetProperty("usage", out var usageEl))
        {
            var inputTokens = usageEl.TryGetProperty("input_tokens", out var it) ? it.GetInt32() : 0;
            var outputTokens = usageEl.TryGetProperty("output_tokens", out var ot) ? ot.GetInt32() : 0;
            usage = new TokenUsage(inputTokens, outputTokens);
        }

        byte[]? imageData = null;
        if (b64Data is not null)
            imageData = Convert.FromBase64String(b64Data);

        return new ImageResult
        {
            ImageData = imageData,
            RevisedPrompt = revisedPrompt,
            Usage = usage,
        };
    }
}
