using System.Text;
using System.Text.Json;

namespace TheCanonry.ApiClients.Images;

/// <summary>
/// Image upscaling client for fal.ai.
/// Async queue workflow: POST submit -> poll status -> GET result -> download image.
/// Desktop app calls fal.ai directly (no CORS relay needed).
/// </summary>
public sealed class FalImageClient : IImageClient
{
    private const string QueueBase = "https://queue.fal.run";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan PollTimeout = TimeSpan.FromMinutes(10);

    /// <summary>
    /// fal.ai model IDs for each upscaler variant.
    /// </summary>
    internal static readonly Dictionary<string, string> ModelIds = new()
    {
        ["clarity"] = "fal-ai/clarity-upscaler",
        ["creative"] = "fal-ai/creative-upscaler",
        ["topaz"] = "fal-ai/topaz/upscale/image",
    };

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public FalImageClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public ImageProvider Provider => ImageProvider.FalUpscale;
    public bool IsEnabled => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<ImageResult> GenerateAsync(ImageRequest request, CancellationToken ct = default)
    {
        if (!IsEnabled)
            return new ImageResult { Error = "Client disabled - missing API key" };

        try
        {
            return await ExecuteRequest(request, ct);
        }
        catch (Exception ex)
        {
            return new ImageResult { Error = ex.Message };
        }
    }

    private async Task<ImageResult> ExecuteRequest(ImageRequest request, CancellationToken ct)
    {
        var modelId = ResolveModelId(request.Model);
        var body = BuildRequestBody(request);
        var json = JsonSerializer.Serialize(body);

        // Step 1: Submit to queue
        using var submitRequest = new HttpRequestMessage(HttpMethod.Post, $"{QueueBase}/{modelId}")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        AddAuthHeader(submitRequest);

        using var submitResponse = await _httpClient.SendAsync(submitRequest, ct);
        var submitText = await submitResponse.Content.ReadAsStringAsync(ct);

        if (!submitResponse.IsSuccessStatusCode)
        {
            var errorDetail = ExtractErrorDetail(submitText);
            throw new InvalidOperationException($"fal submit error {(int)submitResponse.StatusCode}: {errorDetail}");
        }

        using var submitDoc = JsonDocument.Parse(submitText);
        var requestId = submitDoc.RootElement.GetProperty("request_id").GetString()
            ?? throw new InvalidOperationException("fal returned no request_id");

        // Step 2: Poll for completion
        var pollStart = DateTime.UtcNow;

        while (DateTime.UtcNow - pollStart < PollTimeout)
        {
            await Task.Delay(PollInterval, ct);

            using var statusRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{QueueBase}/{modelId}/requests/{requestId}/status");
            AddAuthHeader(statusRequest);

            using var statusResponse = await _httpClient.SendAsync(statusRequest, ct);
            if (!statusResponse.IsSuccessStatusCode) continue;

            var statusText = await statusResponse.Content.ReadAsStringAsync(ct);
            using var statusDoc = JsonDocument.Parse(statusText);
            var status = statusDoc.RootElement.GetProperty("status").GetString() ?? "";

            if (status == "COMPLETED") break;

            if (status == "FAILED")
            {
                var error = statusDoc.RootElement.TryGetProperty("error", out var errEl)
                    ? errEl.GetString() ?? "unknown error"
                    : "unknown error";
                throw new InvalidOperationException($"fal task failed: {error}");
            }
            // IN_QUEUE | IN_PROGRESS -> keep polling
        }

        // Step 3: Fetch result metadata
        using var resultRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{QueueBase}/{modelId}/requests/{requestId}");
        AddAuthHeader(resultRequest);

        using var resultResponse = await _httpClient.SendAsync(resultRequest, ct);
        if (!resultResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"fal result error {(int)resultResponse.StatusCode}");

        var resultText = await resultResponse.Content.ReadAsStringAsync(ct);
        using var resultDoc = JsonDocument.Parse(resultText);
        var resultRoot = resultDoc.RootElement;

        // fal models return image in either .image or .images[0]
        string? imageUrl = null;
        if (resultRoot.TryGetProperty("image", out var imageEl) &&
            imageEl.TryGetProperty("url", out var urlEl))
        {
            imageUrl = urlEl.GetString();
        }
        else if (resultRoot.TryGetProperty("images", out var imagesEl) &&
                 imagesEl.ValueKind == JsonValueKind.Array &&
                 imagesEl.GetArrayLength() > 0 &&
                 imagesEl[0].TryGetProperty("url", out var firstUrl))
        {
            imageUrl = firstUrl.GetString();
        }

        if (imageUrl is null)
            throw new InvalidOperationException("fal returned no output image");

        // Step 4: Download the image
        using var imageResponse = await _httpClient.GetAsync(imageUrl, ct);
        if (!imageResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to download fal image: {(int)imageResponse.StatusCode}");

        var imageData = await imageResponse.Content.ReadAsByteArrayAsync(ct);

        return new ImageResult { ImageData = imageData };
    }

    internal static Dictionary<string, object?> BuildRequestBody(ImageRequest request)
    {
        var model = request.Model.ToLowerInvariant();
        var additionalParams = request.AdditionalParams;

        T GetParam<T>(string key, T defaultValue) =>
            additionalParams is not null && additionalParams.TryGetValue(key, out var val) && val is T typed
                ? typed
                : defaultValue;

        int GetIntParam(string key, int defaultValue) =>
            additionalParams is not null && additionalParams.TryGetValue(key, out var val)
                ? val switch { int i => i, long l => (int)l, double d => (int)d, _ => defaultValue }
                : defaultValue;

        double GetDoubleParam(string key, double defaultValue) =>
            additionalParams is not null && additionalParams.TryGetValue(key, out var val)
                ? val switch { double d => d, int i => i, long l => l, float f => f, _ => defaultValue }
                : defaultValue;

        return model switch
        {
            "clarity" => new Dictionary<string, object?>
            {
                ["image_url"] = request.Prompt, // For upscale, Prompt carries the image data URI
                ["prompt"] = GetParam<string>("prompt", ""),
                ["negative_prompt"] = GetParam<string>("negative_prompt", ""),
                ["upscale_factor"] = GetIntParam("factor", 2),
                ["creativity"] = GetDoubleParam("creativity", 0.35),
                ["resemblance"] = GetDoubleParam("resemblance", 0.6),
                ["guidance_scale"] = 4,
                ["num_inference_steps"] = 28,
                ["enable_safety_checker"] = false,
            },
            "creative" => new Dictionary<string, object?>
            {
                ["image_url"] = request.Prompt,
                ["prompt"] = GetParam<string>("prompt", ""),
                ["negative_prompt"] = GetParam<string>("negative_prompt", ""),
                ["scale"] = GetIntParam("factor", 2),
                ["creativity"] = GetDoubleParam("creativity", 0.35),
                ["detail"] = 2.0,
                ["shape_preservation"] = 0.5,
                ["model_type"] = "SDXL",
                ["guidance_scale"] = 5,
                ["num_inference_steps"] = 30,
                ["enable_safety_checks"] = false,
            },
            "topaz" => new Dictionary<string, object?>
            {
                ["image_url"] = request.Prompt,
                ["model"] = "CGI",
                ["upscale_factor"] = GetIntParam("factor", 2),
                ["output_format"] = "png",
                ["face_enhancement"] = false,
            },
            _ => new Dictionary<string, object?>
            {
                ["image_url"] = request.Prompt,
                ["upscale_factor"] = GetIntParam("factor", 2),
            },
        };
    }

    private static string ResolveModelId(string model) =>
        ModelIds.TryGetValue(model.ToLowerInvariant(), out var id) ? id : model;

    private void AddAuthHeader(HttpRequestMessage request)
    {
        request.Headers.Add("Authorization", $"Key {_apiKey}");
    }

    private static string ExtractErrorDetail(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            if (root.TryGetProperty("detail", out var d)) return d.GetString() ?? text;
            if (root.TryGetProperty("error", out var e)) return e.GetString() ?? text;
            if (root.TryGetProperty("message", out var m)) return m.GetString() ?? text;
        }
        catch (JsonException) { }
        return text;
    }
}
