using System.Net;
using System.Text.Json;
using TheCanonry.ApiClients.Images;

namespace TheCanonry.ApiClients.Tests.Images;

public sealed class ImageClientTests
{
    // ── DALL-E ──────────────────────────────────────────────

    [Fact]
    public async Task DalleClient_BuildsCorrectRequestBody()
    {
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG magic bytes
        var b64 = Convert.ToBase64String(imageBytes);
        var responseJson = JsonSerializer.Serialize(new
        {
            data = new[] { new { b64_json = b64, revised_prompt = "A revised prompt" } },
        });

        var handler = new MockHandler().Respond(HttpStatusCode.OK, responseJson);
        using var httpClient = new HttpClient(handler);
        var client = new DalleImageClient(httpClient, "test-key", "dall-e-3");

        var request = new ImageRequest
        {
            Prompt = "A sunset over mountains",
            Model = "dall-e-3",
            Width = 1024,
            Height = 1024,
            Quality = "hd",
        };

        var result = await client.GenerateAsync(request);

        // Verify request body
        Assert.NotNull(handler.FirstRequestBody);
        using var doc = JsonDocument.Parse(handler.FirstRequestBody);
        var root = doc.RootElement;

        Assert.Equal("dall-e-3", root.GetProperty("model").GetString());
        Assert.Equal("A sunset over mountains", root.GetProperty("prompt").GetString());
        Assert.Equal(1, root.GetProperty("n").GetInt32());
        Assert.Equal("b64_json", root.GetProperty("response_format").GetString());
        Assert.Equal("hd", root.GetProperty("quality").GetString());
        Assert.Equal("1024x1024", root.GetProperty("size").GetString());

        // Verify auth header
        var authHeader = handler.Requests[0].Headers.Authorization;
        Assert.NotNull(authHeader);
        Assert.Equal("Bearer", authHeader.Scheme);
        Assert.Equal("test-key", authHeader.Parameter);

        // Verify result
        Assert.NotNull(result.ImageData);
        Assert.Equal(imageBytes, result.ImageData);
        Assert.Equal("A revised prompt", result.RevisedPrompt);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task DalleClient_HandlesApiError()
    {
        var handler = new MockHandler()
            .Respond(HttpStatusCode.BadRequest, """{"error":{"message":"Invalid prompt"}}""");
        using var httpClient = new HttpClient(handler);
        var client = new DalleImageClient(httpClient, "test-key", "dall-e-3");

        var result = await client.GenerateAsync(new ImageRequest
        {
            Prompt = "test",
            Model = "dall-e-3",
        });

        Assert.Null(result.ImageData);
        Assert.NotNull(result.Error);
        Assert.Contains("400", result.Error);
    }

    // ── BFL ─────────────────────────────────────────────────

    [Fact]
    public async Task BflClient_SubmitPollDownloadFlow()
    {
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        var handler = new MockHandler()
            // Step 1: Submit -> returns task id and polling URL
            .Respond(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                id = "task-123",
                polling_url = "https://api.bfl.ai/v1/get_result?id=task-123",
            }))
            // Step 2: First poll -> Pending
            .Respond(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                id = "task-123",
                status = "Pending",
            }))
            // Step 3: Second poll -> Ready with result URL
            .Respond(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                id = "task-123",
                status = "Ready",
                result = new { sample = "https://storage.bfl.ai/output-123.png" },
            }))
            // Step 4: Image download
            .RespondBytes(HttpStatusCode.OK, imageBytes);

        using var httpClient = new HttpClient(handler);
        var client = new BflImageClient(httpClient, "bfl-key");

        var result = await client.GenerateAsync(new ImageRequest
        {
            Prompt = "A fantasy castle",
            Model = "flux-pro-1.1-ultra",
            Width = 1024,
            Height = 1024,
        });

        Assert.NotNull(result.ImageData);
        Assert.Equal(imageBytes, result.ImageData);
        Assert.Null(result.Error);

        // Verify submit request had correct header
        Assert.Contains("bfl-key", handler.Requests[0].Headers.GetValues("x-key"));

        // Verify we made 4 requests total: submit + 2 polls + download
        Assert.Equal(4, handler.Requests.Count);
    }

    [Fact]
    public async Task BflClient_HandlesTerminalFailure()
    {
        var handler = new MockHandler()
            .Respond(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                id = "task-456",
                polling_url = "https://api.bfl.ai/v1/get_result?id=task-456",
            }))
            .Respond(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                id = "task-456",
                status = "Content Moderated",
                error = "Content policy violation",
            }));

        using var httpClient = new HttpClient(handler);
        var client = new BflImageClient(httpClient, "bfl-key");

        var result = await client.GenerateAsync(new ImageRequest
        {
            Prompt = "test",
            Model = "flux-pro-1.1-ultra",
        });

        Assert.Null(result.ImageData);
        Assert.NotNull(result.Error);
        Assert.Contains("rejected", result.Error);
    }

    // ── fal.ai ──────────────────────────────────────────────

    [Fact]
    public async Task FalClient_QueueSubmitPollResultFlow()
    {
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG magic bytes

        var handler = new MockHandler()
            // Step 1: Queue submit
            .Respond(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                request_id = "fal-req-789",
                status = "IN_QUEUE",
            }))
            // Step 2: Poll -> IN_PROGRESS
            .Respond(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                status = "IN_PROGRESS",
            }))
            // Step 3: Poll -> COMPLETED
            .Respond(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                status = "COMPLETED",
            }))
            // Step 4: Result metadata
            .Respond(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                image = new { url = "https://fal.run/output/result-789.png", width = 2048, height = 2048 },
            }))
            // Step 5: Image download
            .RespondBytes(HttpStatusCode.OK, imageBytes);

        using var httpClient = new HttpClient(handler);
        var client = new FalImageClient(httpClient, "fal-key");

        var result = await client.GenerateAsync(new ImageRequest
        {
            Prompt = "data:image/png;base64,iVBOR...",
            Model = "clarity",
            AdditionalParams = new Dictionary<string, object>
            {
                ["factor"] = 2,
                ["creativity"] = 0.35,
                ["resemblance"] = 0.6,
                ["prompt"] = "enhance details",
                ["negative_prompt"] = "blur",
            },
        });

        Assert.NotNull(result.ImageData);
        Assert.Equal(imageBytes, result.ImageData);
        Assert.Null(result.Error);

        // Verify requests: submit + 2 polls + result + download = 5
        Assert.Equal(5, handler.Requests.Count);

        // Verify auth header uses Key scheme
        Assert.Contains("Key fal-key", handler.Requests[0].Headers.GetValues("Authorization"));
    }

    [Fact]
    public async Task FalClient_HandlesFalFailure()
    {
        var handler = new MockHandler()
            .Respond(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                request_id = "fal-req-fail",
                status = "IN_QUEUE",
            }))
            .Respond(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                status = "FAILED",
                error = "Model timeout",
            }));

        using var httpClient = new HttpClient(handler);
        var client = new FalImageClient(httpClient, "fal-key");

        var result = await client.GenerateAsync(new ImageRequest
        {
            Prompt = "data:image/png;base64,...",
            Model = "clarity",
        });

        Assert.Null(result.ImageData);
        Assert.NotNull(result.Error);
        Assert.Contains("Model timeout", result.Error);
    }

    // ── WaveSpeed ───────────────────────────────────────────

    [Fact]
    public async Task WaveSpeedClient_SubmitPollDownloadFlow()
    {
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        var handler = new MockHandler()
            // Step 1: Submit
            .Respond(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                code = 200,
                message = "ok",
                data = new
                {
                    id = "ws-task-1",
                    status = "created",
                    urls = new { get = "https://api.wavespeed.ai/api/v3/status/ws-task-1" },
                },
            }))
            // Step 2: Poll -> processing
            .Respond(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                code = 200,
                message = "ok",
                data = new
                {
                    id = "ws-task-1",
                    model = "flux-schnell",
                    status = "processing",
                },
            }))
            // Step 3: Poll -> completed
            .Respond(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                code = 200,
                message = "ok",
                data = new
                {
                    id = "ws-task-1",
                    model = "flux-schnell",
                    status = "completed",
                    outputs = new[] { "https://wavespeed.ai/output/result-1.png" },
                },
            }))
            // Step 4: Image download
            .RespondBytes(HttpStatusCode.OK, imageBytes);

        using var httpClient = new HttpClient(handler);
        var client = new WaveSpeedImageClient(httpClient, "ws-key");

        var result = await client.GenerateAsync(new ImageRequest
        {
            Prompt = "A dragon breathing fire",
            Model = "flux-schnell",
            Width = 1024,
            Height = 1024,
        });

        Assert.NotNull(result.ImageData);
        Assert.Equal(imageBytes, result.ImageData);
        Assert.Null(result.Error);

        // Verify auth header
        Assert.Contains("Bearer ws-key", handler.Requests[0].Headers.GetValues("Authorization"));

        // 4 requests: submit + 2 polls + download
        Assert.Equal(4, handler.Requests.Count);
    }

    // ── Disabled client ─────────────────────────────────────

    [Fact]
    public async Task DisabledClient_ReturnsError()
    {
        using var httpClient = new HttpClient();
        var client = new DalleImageClient(httpClient, "", "dall-e-3");

        Assert.False(client.IsEnabled);

        var result = await client.GenerateAsync(new ImageRequest
        {
            Prompt = "test",
            Model = "dall-e-3",
        });

        Assert.Null(result.ImageData);
        Assert.NotNull(result.Error);
        Assert.Contains("missing API key", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
