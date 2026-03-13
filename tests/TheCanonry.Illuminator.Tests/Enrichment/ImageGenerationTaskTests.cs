using TheCanonry.ApiClients.Images;
using TheCanonry.Illuminator.Enrichment;
using TheCanonry.Illuminator.ImagePipeline;
using TheCanonry.Illuminator.Types;
using TheCanonry.Schema.Ids;

namespace TheCanonry.Illuminator.Tests.Enrichment;

public sealed class ImageGenerationTaskTests
{
    private sealed class MockImageClient : IImageClient
    {
        public ImageProvider Provider => ImageProvider.DallE3;
        public bool IsEnabled => true;

        private readonly Func<ImageRequest, Task<ImageResult>> _handler;

        public MockImageClient(Func<ImageRequest, Task<ImageResult>>? handler = null)
        {
            _handler = handler ?? (_ => Task.FromResult(new ImageResult
            {
                ImageData = [1, 2, 3, 4],
                RevisedPrompt = "revised prompt",
            }));
        }

        public Task<ImageResult> GenerateAsync(ImageRequest request, CancellationToken ct = default) =>
            _handler(request);
    }

    [Fact]
    public async Task SuccessfulGeneration_ReturnsImageData()
    {
        var mock = new MockLlmClient();
        var imageClient = new MockImageClient();
        var ctx = TestHelpers.MakeContext(mock);

        var task = new ImageGenerationTask(
            ctx, imageClient,
            id => id == new EntityId("e1") ? "A scholar in icy robes" : null);

        var job = TestHelpers.MakeJob(EnrichmentType.Image);
        var result = await task.ExecuteAsync(job, new Progress<TaskProgress>(), CancellationToken.None);

        Assert.True(result.Success);
        var data = Assert.IsType<ImageGenerationData>(result.Data);
        Assert.NotEmpty(data.ImageData);
        Assert.Equal("dall-e-3", data.Model);
        Assert.Equal("revised prompt", data.RevisedPrompt);
    }

    [Fact]
    public async Task MissingPrompt_ReturnsError()
    {
        var mock = new MockLlmClient();
        var imageClient = new MockImageClient();
        var ctx = TestHelpers.MakeContext(mock);

        var task = new ImageGenerationTask(ctx, imageClient, _ => null);
        var job = TestHelpers.MakeJob(EnrichmentType.Image);

        var result = await task.ExecuteAsync(job, new Progress<TaskProgress>(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("No image prompt", result.Error);
    }

    [Fact]
    public async Task ImageApiError_ReturnsError()
    {
        var mock = new MockLlmClient();
        var imageClient = new MockImageClient(
            _ => Task.FromResult(new ImageResult { Error = "content policy violation" }));
        var ctx = TestHelpers.MakeContext(mock);

        var task = new ImageGenerationTask(
            ctx, imageClient,
            _ => "test prompt");

        var job = TestHelpers.MakeJob(EnrichmentType.Image);
        var result = await task.ExecuteAsync(job, new Progress<TaskProgress>(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("content policy violation", result.Error);
    }

    [Fact]
    public async Task NullImageData_ReturnsError()
    {
        var mock = new MockLlmClient();
        var imageClient = new MockImageClient(
            _ => Task.FromResult(new ImageResult { ImageData = null }));
        var ctx = TestHelpers.MakeContext(mock);

        var task = new ImageGenerationTask(
            ctx, imageClient,
            _ => "test prompt");

        var job = TestHelpers.MakeJob(EnrichmentType.Image);
        var result = await task.ExecuteAsync(job, new Progress<TaskProgress>(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("No image data", result.Error);
    }
}
