using TheCanonry.Illuminator.Enrichment;
using TheCanonry.Illuminator.Enrichment.Tasks;
using TheCanonry.Illuminator.Types;
using TheCanonry.Schema.Ids;

namespace TheCanonry.Illuminator.Tests.Enrichment;

public sealed class DescriptionTaskTests
{
    private static readonly string NarrativeJson = """
        {
            "summary": "A wandering scholar of the northern reaches.",
            "description": "Tall and gaunt, the scholar moves through icy corridors with purpose.",
            "aliases": ["The Ice Reader", "Old Frostback"]
        }
        """;

    private const string VisualThesisText = "A gaunt figure wrapped in frost-rimed robes, hunched over an ancient tome.";

    private const string VisualTraitsText = """
        - pale blue eyes with frost-white lashes
        - ink-stained fingers, perpetually cold
        - a satchel of frozen scrolls
        """;

    [Fact]
    public async Task SuccessfulChain_ReturnsAllFields()
    {
        var mock = new MockLlmClient()
            .Enqueue(NarrativeJson, 50, 100)
            .Enqueue(VisualThesisText, 30, 20)
            .Enqueue(VisualTraitsText, 30, 30);

        var entity = TestHelpers.MakeEntity();
        var ctx = TestHelpers.MakeContext(mock);
        var task = new DescriptionTask(ctx, id => id == entity.Id ? entity : null);
        var job = TestHelpers.MakeJob();
        var progress = new Progress<TaskProgress>();

        var result = await task.ExecuteAsync(job, progress, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Null(result.Error);
        var data = Assert.IsType<DescriptionData>(result.Data);
        Assert.Equal("A wandering scholar of the northern reaches.", data.Summary);
        Assert.Contains("Tall and gaunt", data.Description);
        Assert.Equal(2, data.Aliases.Count);
        Assert.Equal(VisualThesisText, data.VisualThesis);
        Assert.Equal(3, data.VisualTraits.Count);
        Assert.NotNull(result.TokenUsage);
        Assert.Equal(110, result.TokenUsage.InputTokens);
        Assert.Equal(150, result.TokenUsage.OutputTokens);
    }

    [Fact]
    public async Task EntityNotFound_ReturnsError()
    {
        var mock = new MockLlmClient();
        var ctx = TestHelpers.MakeContext(mock);
        var task = new DescriptionTask(ctx, _ => null);
        var job = TestHelpers.MakeJob();

        var result = await task.ExecuteAsync(job, new Progress<TaskProgress>(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task NarrativeStepFails_ReturnsError()
    {
        var mock = new MockLlmClient()
            .EnqueueError("API rate limited");

        var entity = TestHelpers.MakeEntity();
        var ctx = TestHelpers.MakeContext(mock);
        var task = new DescriptionTask(ctx, id => id == entity.Id ? entity : null);

        var result = await task.ExecuteAsync(
            TestHelpers.MakeJob(), new Progress<TaskProgress>(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Narrative step failed", result.Error);
    }

    [Fact]
    public async Task EmptyNarrativeResponse_ReturnsError()
    {
        var mock = new MockLlmClient()
            .Enqueue("{}", 10, 10);

        var entity = TestHelpers.MakeEntity();
        var ctx = TestHelpers.MakeContext(mock);
        var task = new DescriptionTask(ctx, id => id == entity.Id ? entity : null);

        var result = await task.ExecuteAsync(
            TestHelpers.MakeJob(), new Progress<TaskProgress>(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("empty", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ThesisStepFails_ReturnsError()
    {
        var mock = new MockLlmClient()
            .Enqueue(NarrativeJson, 50, 100)
            .EnqueueError("Overloaded");

        var entity = TestHelpers.MakeEntity();
        var ctx = TestHelpers.MakeContext(mock);
        var task = new DescriptionTask(ctx, id => id == entity.Id ? entity : null);

        var result = await task.ExecuteAsync(
            TestHelpers.MakeJob(), new Progress<TaskProgress>(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Visual thesis step failed", result.Error);
    }

    [Fact]
    public async Task MakesThreeSequentialLlmCalls()
    {
        var mock = new MockLlmClient()
            .Enqueue(NarrativeJson, 50, 100)
            .Enqueue(VisualThesisText, 30, 20)
            .Enqueue(VisualTraitsText, 30, 30);

        var entity = TestHelpers.MakeEntity();
        var ctx = TestHelpers.MakeContext(mock);
        var task = new DescriptionTask(ctx, id => id == entity.Id ? entity : null);

        await task.ExecuteAsync(TestHelpers.MakeJob(), new Progress<TaskProgress>(), CancellationToken.None);

        Assert.Equal(3, mock.Requests.Count);
    }
}
