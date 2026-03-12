using TheCanonry.Illuminator.Chronicle;
using TheCanonry.Illuminator.Tests.Enrichment;
using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Tests.Chronicle;

public sealed class ChroniclePipelineTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    private static ChronicleGenerationContext MakeMinimalContext() => new()
    {
        WorldName = "Aelthar",
        WorldDescription = "A world of ancient cycles.",
        Tone = "Mythic and melancholic",
        CanonFacts = ["The ice remembers."],
        Entities =
        [
            new EntityContext
            {
                Id = "e1",
                Name = "Aldric",
                Kind = "npc",
                Prominence = "recognized",
                Status = "active",
            },
        ],
        Relationships = [],
    };

    // =========================================================================
    // ChronicleVersionManager tests
    // =========================================================================

    [Fact]
    public void CreateVersion_SetsAllFieldsCorrectly()
    {
        var manager = new ChronicleVersionManager();
        const string content = "Once upon a time in Aelthar, the ice held old secrets.";

        var version = manager.CreateVersion(
            content: content,
            model: "claude-sonnet-4-6-20250514",
            step: VersionStep.Generate,
            systemPrompt: "You are a writer.",
            userPrompt: "Write a chronicle.",
            inputTokens: 100,
            outputTokens: 200,
            estimatedCost: 0.005m,
            actualCost: 0.004m);

        Assert.NotNull(version.VersionId);
        Assert.Equal(8, version.VersionId.Length);
        Assert.Equal(content, version.Content);
        Assert.Equal("claude-sonnet-4-6-20250514", version.Model);
        Assert.Equal(VersionStep.Generate, version.Step);
        Assert.Equal("You are a writer.", version.SystemPrompt);
        Assert.Equal("Write a chronicle.", version.UserPrompt);
        Assert.Equal(100, version.InputTokens);
        Assert.Equal(200, version.OutputTokens);
        Assert.Equal(0.005m, version.EstimatedCost);
        Assert.Equal(0.004m, version.ActualCost);
        Assert.True(version.WordCount > 0);
        Assert.True(version.GeneratedAt > 0);
    }

    [Fact]
    public void CreateVersion_UpdatesActiveVersionId()
    {
        var manager = new ChronicleVersionManager();

        var v1 = manager.CreateVersion("First draft.", "model-a", VersionStep.Generate, "sys", "user");
        Assert.Equal(v1.VersionId, manager.ActiveVersionId);

        var v2 = manager.CreateVersion("Second draft.", "model-a", VersionStep.CopyEdit, "sys", "user");
        Assert.Equal(v2.VersionId, manager.ActiveVersionId);

        Assert.Equal(2, manager.Versions.Count);
    }

    [Fact]
    public void SetActiveVersion_SwitchesCorrectly()
    {
        var manager = new ChronicleVersionManager();

        var v1 = manager.CreateVersion("First draft.", "model", VersionStep.Generate, "sys", "user");
        var v2 = manager.CreateVersion("Second draft.", "model", VersionStep.CopyEdit, "sys", "user");

        // Active is v2 after creation — switch back to v1
        manager.SetActiveVersion(v1.VersionId);

        Assert.Equal(v1.VersionId, manager.ActiveVersionId);
        Assert.Equal(v1.Content, manager.GetActiveVersion()?.Content);
    }

    [Fact]
    public void SetActiveVersion_ThrowsOnUnknownId()
    {
        var manager = new ChronicleVersionManager();
        manager.CreateVersion("Draft.", "model", VersionStep.Generate, "sys", "user");

        var ex = Assert.Throws<ArgumentException>(() => manager.SetActiveVersion("nonexistent"));
        Assert.Contains("nonexistent", ex.Message);
    }

    [Fact]
    public void LoadVersions_ReplacesExistingVersions()
    {
        var manager = new ChronicleVersionManager();
        manager.CreateVersion("Original.", "model", VersionStep.Generate, "sys", "user");

        var loaded = new List<ChronicleVersion>
        {
            new()
            {
                VersionId = "abc12345",
                GeneratedAt = 1_000_000L,
                Content = "Loaded content.",
                WordCount = 2,
                Model = "claude-haiku",
                Step = VersionStep.Generate,
                SystemPrompt = "sys",
                UserPrompt = "user",
            },
        };

        manager.LoadVersions(loaded, "abc12345");

        Assert.Single(manager.Versions);
        Assert.Equal("abc12345", manager.ActiveVersionId);
        Assert.Equal("Loaded content.", manager.GetActiveVersion()?.Content);
    }

    // =========================================================================
    // ChroniclePipelineOrchestrator tests
    // =========================================================================

    [Fact]
    public async Task RunGenerationAsync_CallsLlmAndReturnsContent()
    {
        const string expectedContent = "The chronicle of Aldric begins here.";
        var mock = new MockLlmClient().Enqueue(expectedContent, inputTokens: 150, outputTokens: 300);
        var orchestrator = new ChroniclePipelineOrchestrator(mock);

        var (content, systemPrompt, userPrompt, inputTokens, outputTokens) =
            await orchestrator.RunGenerationAsync(
                MakeMinimalContext(),
                ChronicleFormat.Story);

        Assert.Equal(expectedContent, content);
        Assert.NotEmpty(systemPrompt);
        Assert.NotEmpty(userPrompt);
        Assert.Equal(150, inputTokens);
        Assert.Equal(300, outputTokens);
        Assert.Single(mock.Requests);
    }

    [Fact]
    public async Task RunGenerationAsync_ThrowsOnLlmError()
    {
        var mock = new MockLlmClient().EnqueueError("Rate limited");
        var orchestrator = new ChroniclePipelineOrchestrator(mock);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => orchestrator.RunGenerationAsync(MakeMinimalContext(), ChronicleFormat.Story));
    }

    [Fact]
    public async Task RunCopyEditAsync_StoryFormat_UsesStorySystemPrompt()
    {
        var mock = new MockLlmClient().Enqueue("Polished story text.", inputTokens: 200, outputTokens: 400);
        var orchestrator = new ChroniclePipelineOrchestrator(mock);

        var (content, inputTokens, outputTokens) = await orchestrator.RunCopyEditAsync(
            text: "Raw draft text.",
            format: ChronicleFormat.Story,
            styleName: "Mythic Epic",
            minWords: 2000,
            maxWords: 4000);

        Assert.Equal("Polished story text.", content);
        Assert.Equal(200, inputTokens);
        Assert.Equal(400, outputTokens);

        var sentRequest = mock.Requests.Single();
        // Story system prompt contains "fiction editor" not "document"
        Assert.Contains("fiction editor", sentRequest.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunCopyEditAsync_DocumentFormat_UsesDocumentSystemPrompt()
    {
        var mock = new MockLlmClient().Enqueue("Polished document text.", inputTokens: 100, outputTokens: 200);
        var orchestrator = new ChroniclePipelineOrchestrator(mock);

        var (content, _, _) = await orchestrator.RunCopyEditAsync(
            text: "Raw document draft.",
            format: ChronicleFormat.Document,
            styleName: "Bureaucratic Report",
            minWords: 400,
            maxWords: 600);

        Assert.Equal("Polished document text.", content);

        var sentRequest = mock.Requests.Single();
        // Document system prompt contains "in-universe document"
        Assert.Contains("in-universe document", sentRequest.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunSummaryAsync_ReturnsSummaryText()
    {
        const string expectedSummary = "Aldric faced the ice and remembered.";
        var mock = new MockLlmClient().Enqueue(expectedSummary, inputTokens: 50, outputTokens: 30);
        var orchestrator = new ChroniclePipelineOrchestrator(mock);

        var (summary, inputTokens, outputTokens) = await orchestrator.RunSummaryAsync(
            "A long chronicle about Aldric and the ancient ice.");

        Assert.Equal(expectedSummary, summary);
        Assert.Equal(50, inputTokens);
        Assert.Equal(30, outputTokens);
    }

    [Fact]
    public async Task RunTitleAsync_ReturnsTitlesJson()
    {
        const string titlesJson = """["The Ice Remembers", "Cold Reckoning", "What Aldric Knew"]""";
        var mock = new MockLlmClient().Enqueue(titlesJson, inputTokens: 60, outputTokens: 40);
        var orchestrator = new ChroniclePipelineOrchestrator(mock);

        var (json, inputTokens, outputTokens) = await orchestrator.RunTitleAsync(
            "Chronicle content here.",
            summary: "Aldric and the ice.");

        Assert.Equal(titlesJson, json);
        Assert.Equal(60, inputTokens);
        Assert.Equal(40, outputTokens);
    }

    [Fact]
    public async Task RunImageRefsAsync_ReturnsImageRefsJson()
    {
        const string refsJson = """{"refs":[{"refId":"ref1","anchorText":"the ice","sceneDescription":"A vast glacier.","size":"large"}]}""";
        var mock = new MockLlmClient().Enqueue(refsJson, inputTokens: 80, outputTokens: 120);
        var orchestrator = new ChroniclePipelineOrchestrator(mock);

        var entities = new List<EntityContext>
        {
            new() { Id = "e1", Name = "Aldric", Kind = "npc", Prominence = "recognized", Status = "active" },
        };

        var (json, inputTokens, outputTokens) = await orchestrator.RunImageRefsAsync(
            "Chronicle with a scene by the ice.",
            entities);

        Assert.Equal(refsJson, json);
        Assert.Equal(80, inputTokens);
        Assert.Equal(120, outputTokens);

        var sentRequest = mock.Requests.Single();
        Assert.Contains("e1", sentRequest.UserPrompt);
        Assert.Contains("Aldric", sentRequest.UserPrompt);
    }

    [Fact]
    public async Task RunGenerationAsync_ReportsProgress()
    {
        var mock = new MockLlmClient().Enqueue("Content.", inputTokens: 10, outputTokens: 20);
        var orchestrator = new ChroniclePipelineOrchestrator(mock);

        var progressReports = new List<double>();
        var progress = new Progress<TheCanonry.Illuminator.Enrichment.TaskProgress>(p =>
        {
            if (p.Fraction.HasValue)
                progressReports.Add(p.Fraction.Value);
        });

        await orchestrator.RunGenerationAsync(
            MakeMinimalContext(),
            ChronicleFormat.Story,
            progress: progress);

        // Allow async progress callbacks to flush
        await Task.Delay(10);
        Assert.NotEmpty(progressReports);
    }
}
