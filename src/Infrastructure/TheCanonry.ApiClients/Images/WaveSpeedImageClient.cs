using System.Text;
using System.Text.Json;

namespace TheCanonry.ApiClients.Images;

/// <summary>
/// Image generation client for WaveSpeed.ai.
/// Async task workflow: POST task -> poll for completion -> download image.
/// </summary>
public sealed class WaveSpeedImageClient : IImageClient
{
    private const string ApiBase = "https://api.wavespeed.ai/api/v3";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan PollTimeout = TimeSpan.FromMinutes(5);

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public WaveSpeedImageClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public ImageProvider Provider => ImageProvider.WaveSpeed;
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
        var body = BuildRequestBody(request);
        var json = JsonSerializer.Serialize(body);

        // Step 1: Submit task
        using var submitRequest = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/{request.Model}")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        submitRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");

        using var submitResponse = await _httpClient.SendAsync(submitRequest, ct);
        var submitText = await submitResponse.Content.ReadAsStringAsync(ct);

        if (!submitResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"WaveSpeed submit error {(int)submitResponse.StatusCode}: {submitText}");

        using var submitDoc = JsonDocument.Parse(submitText);
        var dataEl = submitDoc.RootElement.GetProperty("data");
        var pollUrl = dataEl.GetProperty("urls").GetProperty("get").GetString()
            ?? throw new InvalidOperationException("WaveSpeed returned no poll URL");

        // Step 2: Poll for completion
        var pollStart = DateTime.UtcNow;
        string? outputUrl = null;

        while (DateTime.UtcNow - pollStart < PollTimeout)
        {
            await Task.Delay(PollInterval, ct);

            using var pollRequest = new HttpRequestMessage(HttpMethod.Get, pollUrl);
            pollRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");

            using var pollResponse = await _httpClient.SendAsync(pollRequest, ct);
            if (!pollResponse.IsSuccessStatusCode)
                throw new InvalidOperationException($"WaveSpeed poll error {(int)pollResponse.StatusCode}");

            var pollText = await pollResponse.Content.ReadAsStringAsync(ct);
            using var pollDoc = JsonDocument.Parse(pollText);
            var pollData = pollDoc.RootElement.GetProperty("data");
            var status = pollData.GetProperty("status").GetString() ?? "";

            if (status == "completed")
            {
                if (pollData.TryGetProperty("outputs", out var outputs) &&
                    outputs.ValueKind == JsonValueKind.Array &&
                    outputs.GetArrayLength() > 0)
                {
                    outputUrl = outputs[0].GetString();
                }
                break;
            }

            if (status == "failed")
            {
                var error = pollData.TryGetProperty("error", out var errEl)
                    ? errEl.GetString() ?? "unknown error"
                    : "unknown error";
                throw new InvalidOperationException($"WaveSpeed task failed: {error}");
            }
            // created | processing -> keep polling
        }

        if (outputUrl is null)
            throw new TimeoutException("WaveSpeed task timed out or returned no output");

        // Step 3: Download the image
        using var imageResponse = await _httpClient.GetAsync(outputUrl, ct);
        if (!imageResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to download WaveSpeed image: {(int)imageResponse.StatusCode}");

        var imageData = await imageResponse.Content.ReadAsByteArrayAsync(ct);

        return new ImageResult { ImageData = imageData };
    }

    internal static Dictionary<string, object?> BuildRequestBody(ImageRequest request)
    {
        var body = new Dictionary<string, object?>
        {
            ["prompt"] = request.Prompt,
            ["width"] = request.Width,
            ["height"] = request.Height,
        };

        // Merge any additional provider-specific params
        if (request.AdditionalParams is not null)
        {
            foreach (var (key, value) in request.AdditionalParams)
                body[key] = value;
        }

        return body;
    }
}
