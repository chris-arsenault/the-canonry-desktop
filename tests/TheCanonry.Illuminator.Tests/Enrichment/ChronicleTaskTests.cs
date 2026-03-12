using TheCanonry.Illuminator.Chronicle;
using TheCanonry.Illuminator.Enrichment;
using TheCanonry.Illuminator.Types;
using TheCanonry.Schema.Ids;

namespace TheCanonry.Illuminator.Tests.Enrichment;

public sealed class ChronicleTaskTests
{
    private static ChronicleGenerationContext MakeGenContext() => new()
    {
        WorldName = "Iceworld",
        WorldDescription = "A frozen realm of scholars and secrets.",
        Tone = "Mythic, melancholic",
        CanonFacts = ["The ice remembers.", "Scholars guard the deep archives."],
        Entities =
        [
            new EntityContext
            {
                Id = "e1", Name = "Aldric", Kind = "npc", Prominence = "renowned",
                Culture = "northern", Status = "active",
                Summary = "A wandering scholar", Description = "Tall and gaunt."
            }
        ],
        Relationships = [],
        FocalEraName = "The Clever Ice Age",
    };

    [Fact]
    public async Task SuccessfulGeneration_ReturnsContentAndSummary()
    {
        var mock = new MockLlmClient()
            .Enqueue("The wind howled across the archive as Aldric turned the final page.", 100, 200)
            .Enqueue("A scholar discovers a lost truth in the deep archives.", 50, 30)
            .Enqueue("""["The Final Page", "Wind in the Archive", "Aldric's Discovery"]""", 30, 20);

        var ctx = TestHelpers.MakeContext(mock);
        var task = new ChronicleTask(ctx, id => id == new EntityId("e1") ? MakeGenContext() : null);
        var job = TestHelpers.MakeJob(EnrichmentType.EntityChronicle);

        var result = await task.ExecuteAsync(job, new Progress<TaskProgress>(), CancellationToken.None);

        Assert.True(result.Success);
        var data = Assert.IsType<ChronicleData>(result.Data);
        Assert.Contains("Aldric", data.Content);
        Assert.NotNull(data.Summary);
        Assert.Equal("The Final Page", data.Title);
        Assert.Equal(3, data.TitleCandidates!.Count);
        Assert.NotNull(result.TokenUsage);
        Assert.Equal(180, result.TokenUsage.InputTokens);
    }

    [Fact]
    public async Task MissingContext_ReturnsError()
    {
        var mock = new MockLlmClient();
        var ctx = TestHelpers.MakeContext(mock);
        var task = new ChronicleTask(ctx, _ => null);
        var job = TestHelpers.MakeJob(EnrichmentType.EntityChronicle);

        var result = await task.ExecuteAsync(job, new Progress<TaskProgress>(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("No chronicle context", result.Error);
    }

    [Fact]
    public async Task GenerationFails_ReturnsError()
    {
        var mock = new MockLlmClient()
            .EnqueueError("Rate limited");

        var ctx = TestHelpers.MakeContext(mock);
        var task = new ChronicleTask(ctx, _ => MakeGenContext());
        var job = TestHelpers.MakeJob(EnrichmentType.EntityChronicle);

        var result = await task.ExecuteAsync(job, new Progress<TaskProgress>(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Generation failed", result.Error);
    }

    [Fact]
    public async Task EmptyGeneration_ReturnsError()
    {
        var mock = new MockLlmClient()
            .Enqueue("", 10, 5);

        var ctx = TestHelpers.MakeContext(mock);
        var task = new ChronicleTask(ctx, _ => MakeGenContext());
        var job = TestHelpers.MakeJob(EnrichmentType.EntityChronicle);

        var result = await task.ExecuteAsync(job, new Progress<TaskProgress>(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("empty content", result.Error);
    }

    [Fact]
    public async Task MakesThreeLlmCalls()
    {
        var mock = new MockLlmClient()
            .Enqueue("Chronicle content here.", 100, 200)
            .Enqueue("A brief summary.", 50, 30)
            .Enqueue("""["Title One"]""", 30, 20);

        var ctx = TestHelpers.MakeContext(mock);
        var task = new ChronicleTask(ctx, _ => MakeGenContext());
        var job = TestHelpers.MakeJob(EnrichmentType.EntityChronicle);

        await task.ExecuteAsync(job, new Progress<TaskProgress>(), CancellationToken.None);

        Assert.Equal(3, mock.Requests.Count);
    }

    [Fact]
    public async Task SummaryFailure_StillSucceeds()
    {
        var mock = new MockLlmClient()
            .Enqueue("Chronicle content here.", 100, 200)
            .EnqueueError("Summary failed")
            .Enqueue("""["Title One"]""", 30, 20);

        var ctx = TestHelpers.MakeContext(mock);
        var task = new ChronicleTask(ctx, _ => MakeGenContext());
        var job = TestHelpers.MakeJob(EnrichmentType.EntityChronicle);

        var result = await task.ExecuteAsync(job, new Progress<TaskProgress>(), CancellationToken.None);

        Assert.True(result.Success);
        var data = Assert.IsType<ChronicleData>(result.Data);
        Assert.Null(data.Summary);
    }
}
