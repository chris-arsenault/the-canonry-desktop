using System.Text;
using System.Text.Json;

namespace TheCanonry.ApiClients.Images;

/// <summary>
/// Image generation client for Black Forest Labs (BFL) Flux models.
/// Async queue workflow: POST task -> poll for completion -> download image.
/// Desktop app calls BFL directly (no CORS relay needed).
/// </summary>
public sealed class BflImageClient : IImageClient
{
    private const string BflApiBase = "https://api.bfl.ai/v1";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan PollTimeout = TimeSpan.FromMinutes(5);

    private static readonly HashSet<string> AspectRatioModels = ["flux-pro-1.1-ultra", "flux-pro-1.1-ultra-raw"];

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public BflImageClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public ImageProvider Provider => ImageProvider.BflFlux;
    public bool IsEnabled => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<ImageResult> GenerateAsync(ImageRequest request, CancellationToken ct = default)
    {
        if (!IsEnabled)
            return new ImageResult { Error = "Client disabled - missing API key" };

        try
        {
            return await ExecuteRequest(request, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new ImageResult { Error = ex.Message };
        }
    }

    private async Task<ImageResult> ExecuteRequest(ImageRequest request, CancellationToken ct)
    {
        var model = request.Model;
        var endpoint = ResolveEndpoint(model);
        var body = BuildRequestBody(request, model);
        var json = JsonSerializer.Serialize(body);

        // Step 1: Submit task
        using var submitRequest = new HttpRequestMessage(HttpMethod.Post, $"{BflApiBase}/{endpoint}")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        submitRequest.Headers.Add("x-key", _apiKey);
        submitRequest.Headers.Add("accept", "application/json");

        using var submitResponse = await _httpClient.SendAsync(submitRequest, ct);
        var submitText = await submitResponse.Content.ReadAsStringAsync(ct);

        if (!submitResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"BFL submit error {(int)submitResponse.StatusCode}: {submitText}");

        using var submitDoc = JsonDocument.Parse(submitText);
        var taskId = submitDoc.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("BFL returned no task ID");
        var pollingUrl = submitDoc.RootElement.GetProperty("polling_url").GetString()
            ?? throw new InvalidOperationException("BFL returned no polling URL");

        // Step 2: Poll for completion
        var pollStart = DateTime.UtcNow;
        string? outputUrl = null;

        while (DateTime.UtcNow - pollStart < PollTimeout)
        {
            await Task.Delay(PollInterval, ct);

            using var pollRequest = new HttpRequestMessage(HttpMethod.Get, pollingUrl);
            pollRequest.Headers.Add("x-key", _apiKey);
            pollRequest.Headers.Add("accept", "application/json");

            using var pollResponse = await _httpClient.SendAsync(pollRequest, ct);
            if (!pollResponse.IsSuccessStatusCode) continue;

            var pollText = await pollResponse.Content.ReadAsStringAsync(ct);
            using var pollDoc = JsonDocument.Parse(pollText);
            var status = pollDoc.RootElement.GetProperty("status").GetString() ?? "";

            if (status == "Ready")
            {
                outputUrl = pollDoc.RootElement
                    .GetProperty("result")
                    .GetProperty("sample")
                    .GetString();
                break;
            }

            if (IsTerminalFailure(status))
            {
                var error = pollDoc.RootElement.TryGetProperty("error", out var errEl)
                    ? errEl.GetString() ?? status
                    : status;
                throw new InvalidOperationException($"BFL task rejected ({status}): {error}");
            }
        }

        if (outputUrl is null)
            throw new TimeoutException("BFL task timed out");

        // Step 3: Download the image
        using var imageResponse = await _httpClient.GetAsync(outputUrl, ct);
        if (!imageResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to download BFL image: {(int)imageResponse.StatusCode}");

        var imageData = await imageResponse.Content.ReadAsByteArrayAsync(ct);

        return new ImageResult { ImageData = imageData };
    }

    internal static Dictionary<string, object?> BuildRequestBody(ImageRequest request, string model)
    {
        var body = new Dictionary<string, object?>
        {
            ["prompt"] = request.Prompt,
            ["disable_pup"] = true,
            ["output_format"] = "png",
            ["safety_tolerance"] = 5,
        };

        if (AspectRatioModels.Contains(model))
        {
            body["aspect_ratio"] = request.AspectRatio ?? SizeToAspectRatio(request.Width, request.Height);
        }
        else
        {
            // BFL requires multiples of 16
            body["width"] = RoundTo16(request.Width);
            body["height"] = RoundTo16(request.Height);
        }

        if (model == "flux-pro-1.1-ultra-raw")
            body["raw"] = true;

        return body;
    }

    private static string ResolveEndpoint(string model) =>
        model == "flux-pro-1.1-ultra-raw" ? "flux-pro-1.1-ultra" : model;

    private static string SizeToAspectRatio(int width, int height)
    {
        if (width == height) return "1:1";
        return width > height ? "16:9" : "9:16";
    }

    private static int RoundTo16(int value) => (int)Math.Round(value / 16.0) * 16;

    private static bool IsTerminalFailure(string status)
    {
        var s = status.ToLowerInvariant();
        return s is "error" or "failed"
            || s.Contains("moderat", StringComparison.Ordinal)
            || s.Contains("rejected", StringComparison.Ordinal)
            || s.Contains("not found", StringComparison.Ordinal)
            || s.Contains("cancelled", StringComparison.Ordinal);
    }
}
