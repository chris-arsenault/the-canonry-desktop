using TheCanonry.Illuminator.Catalog;
using TheCanonry.ApiClients.Llm;

namespace TheCanonry.Illuminator.Tests.Catalog;

// ─── Helpers ──────────────────────────────────────────────────────────────────

file static class EntryFactory
{
    private static int _seq;

    public static ImageCatalogEntry MakeEntry(
        string? title = null,
        string? artisticStyleId = null,
        string? compositionStyleId = null,
        string? colorPaletteId = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyList<string>? artisticStyleRanked = null,
        IReadOnlyList<string>? compositionStyleRanked = null,
        IReadOnlyList<string>? colorPaletteRanked = null,
        string? entityName = null)
    {
        var id = $"img-{++_seq:D4}";
        return new ImageCatalogEntry
        {
            ImageId = id,
            EntityId = "ent-1",
            EntityName = entityName ?? "Test Entity",
            EntityKind = "character",
            Culture = "default",
            Model = "test-model",
            Width = 512,
            Height = 512,
            Aspect = "1:1",
            ThumbPath = $"/thumbs/{id}.jpg",
            FullPath = $"/full/{id}.jpg",
            Title = title,
            ArtisticStyleId = artisticStyleId,
            CompositionStyleId = compositionStyleId,
            ColorPaletteId = colorPaletteId,
            Tags = tags,
            ArtisticStyleRanked = artisticStyleRanked,
            CompositionStyleRanked = compositionStyleRanked,
            ColorPaletteRanked = colorPaletteRanked,
        };
    }
}

// ─── CoverageReport Tests ─────────────────────────────────────────────────────

public sealed class CatalogAnalysisTests
{
    // -------------------------------------------------------------------------
    // Test 1: All fields present — zero missing across all tracked fields
    // -------------------------------------------------------------------------

    [Fact]
    public void AnalyzeCoverage_AllFieldsPresent_ReportsZeroMissing()
    {
        var images = new[]
        {
            EntryFactory.MakeEntry(
                title: "Golden Throne",
                artisticStyleId: "oil-painting",
                compositionStyleId: "portrait-closeup",
                colorPaletteId: "warm-earth",
                tags: ["portrait", "regal"]),
        };

        var report = CatalogAnalysis.AnalyzeCoverage(images);

        Assert.Equal(1, report.TotalImages);
        Assert.All(report.Fields, f =>
        {
            Assert.Equal(0, f.Missing);
            Assert.Equal(1, f.Present);
        });
    }

    // -------------------------------------------------------------------------
    // Test 2: Missing title — counted correctly
    // -------------------------------------------------------------------------

    [Fact]
    public void AnalyzeCoverage_MissingTitle_CountedCorrectly()
    {
        var images = new[]
        {
            EntryFactory.MakeEntry(title: null,     artisticStyleId: "oil-painting", compositionStyleId: "portrait-closeup", colorPaletteId: "warm-earth", tags: ["a"]),
            EntryFactory.MakeEntry(title: "Set",    artisticStyleId: "oil-painting", compositionStyleId: "portrait-closeup", colorPaletteId: "warm-earth", tags: ["a"]),
            EntryFactory.MakeEntry(title: "",       artisticStyleId: "oil-painting", compositionStyleId: "portrait-closeup", colorPaletteId: "warm-earth", tags: ["a"]),
        };

        var report = CatalogAnalysis.AnalyzeCoverage(images);

        var titleField = report.Fields.Single(f => f.FieldName == "title");
        Assert.Equal(1, titleField.Present);
        Assert.Equal(2, titleField.Missing);
    }

    // -------------------------------------------------------------------------
    // Test 3: Derivable field counted correctly
    // -------------------------------------------------------------------------

    [Fact]
    public void AnalyzeCoverage_DerivableField_CountedCorrectly()
    {
        // Title is always derivable — all 2 missing entries should be derivable
        var images = new[]
        {
            EntryFactory.MakeEntry(title: null),
            EntryFactory.MakeEntry(title: null),
        };

        var report = CatalogAnalysis.AnalyzeCoverage(images);

        var titleField = report.Fields.Single(f => f.FieldName == "title");
        Assert.Equal(2, titleField.Missing);
        Assert.Equal(2, titleField.Derivable);
        Assert.Equal("entityName + imageId pattern", titleField.DerivableSource);
    }

    // -------------------------------------------------------------------------
    // Test 3b: artisticStyleId derivable when ranked list present
    // -------------------------------------------------------------------------

    [Fact]
    public void AnalyzeCoverage_StyleMissingButRankedPresent_IsDerivable()
    {
        var images = new[]
        {
            EntryFactory.MakeEntry(
                artisticStyleId: null,
                artisticStyleRanked: ["oil-painting", "watercolor"]),
        };

        var report = CatalogAnalysis.AnalyzeCoverage(images);

        var styleField = report.Fields.Single(f => f.FieldName == "artisticStyleId");
        Assert.Equal(1, styleField.Missing);
        Assert.Equal(1, styleField.Derivable);
    }

    // -------------------------------------------------------------------------
    // Test 4: DeterministicFill — fills missing title from entity name
    // -------------------------------------------------------------------------

    [Fact]
    public void DeterministicFill_FillsMissingTitleFromEntityName()
    {
        var images = new[]
        {
            EntryFactory.MakeEntry(entityName: "Lady Thornwood"),
        };

        var filled = CatalogDeterministicFill.Fill(images);

        var entry = Assert.Single(filled);
        Assert.NotNull(entry.Title);
        Assert.Contains("Lady Thornwood", entry.Title);
        Assert.Contains(images[0].ImageId, entry.Title);
    }

    // -------------------------------------------------------------------------
    // Test 5: DeterministicFill — fills style from ranked list
    // -------------------------------------------------------------------------

    [Fact]
    public void DeterministicFill_FillsStyleFromRankedList()
    {
        var images = new[]
        {
            EntryFactory.MakeEntry(
                artisticStyleRanked: ["oil-painting", "watercolor"],
                compositionStyleRanked: ["portrait-closeup", "portrait-bust"],
                colorPaletteRanked: ["warm-earth", "cool-blue"]),
        };

        var filled = CatalogDeterministicFill.Fill(images);

        Assert.Equal("oil-painting", filled[0].ArtisticStyleId);
        Assert.Equal("portrait-closeup", filled[0].CompositionStyleId);
        Assert.Equal("warm-earth", filled[0].ColorPaletteId);
    }

    // -------------------------------------------------------------------------
    // Test 6: DeterministicFill — does not overwrite existing values
    // -------------------------------------------------------------------------

    [Fact]
    public void DeterministicFill_DoesNotOverwriteExistingValues()
    {
        var images = new[]
        {
            EntryFactory.MakeEntry(
                title: "Already Titled",
                artisticStyleId: "ink-wash",
                artisticStyleRanked: ["oil-painting", "watercolor"]),
        };

        var filled = CatalogDeterministicFill.Fill(images);

        Assert.Equal("Already Titled", filled[0].Title);
        Assert.Equal("ink-wash", filled[0].ArtisticStyleId);
    }

    // -------------------------------------------------------------------------
    // Test 7: Similarity — identical titles return 1.0
    // -------------------------------------------------------------------------

    [Fact]
    public void Similarity_IdenticalTitles_ReturnOnePointZero()
    {
        var score = CatalogSimilarity.ComputeSimilarity("throne of thorns", "throne of thorns");
        Assert.Equal(1.0, score, precision: 10);
    }

    // -------------------------------------------------------------------------
    // Test 8: Similarity — completely different titles are below threshold and not reported
    // -------------------------------------------------------------------------

    [Fact]
    public void Similarity_DifferentTitles_BelowThreshold_NotReported()
    {
        var images = new[]
        {
            EntryFactory.MakeEntry(title: "aaa"),
            EntryFactory.MakeEntry(title: "zzz"),
        };

        // Levenshtein("aaa", "zzz") = 3, max = 3, similarity = 0.0
        var pairs = CatalogSimilarity.FindSimilarTitles(images, threshold: 0.5);
        Assert.Empty(pairs);
    }

    // -------------------------------------------------------------------------
    // Test 9: Similarity — near-identical titles above threshold are reported
    // -------------------------------------------------------------------------

    [Fact]
    public void Similarity_NearIdenticalTitles_AboveThreshold_AreReported()
    {
        var images = new[]
        {
            EntryFactory.MakeEntry(title: "throne of thorns"),
            EntryFactory.MakeEntry(title: "throne of thorn"),
        };

        var pairs = CatalogSimilarity.FindSimilarTitles(images, threshold: 0.5);
        Assert.Single(pairs);
        Assert.True(pairs[0].Score >= 0.5);
    }

    // -------------------------------------------------------------------------
    // Test 10: Similarity — empty images returns empty result
    // -------------------------------------------------------------------------

    [Fact]
    public void Similarity_EmptyImages_ReturnsEmpty()
    {
        var pairs = CatalogSimilarity.FindSimilarTitles(Array.Empty<ImageCatalogEntry>());
        Assert.Empty(pairs);
    }
}

// ─── LLM Fill Tests ───────────────────────────────────────────────────────────

public sealed class CatalogLlmFillTests
{
    // -------------------------------------------------------------------------
    // Test 11: FillFromTextAsync — valid LLM response populates fields
    // -------------------------------------------------------------------------

    [Fact]
    public async Task FillFromText_ValidResponse_PopulatesFields()
    {
        var entry = EntryFactory.MakeEntry(entityName: "Thornwood");
        var images = new[] { entry };

        var response = $$"""
            [
              {
                "id": "{{entry.ImageId}}",
                "artisticStyleId": "oil-painting",
                "compositionStyleId": "portrait-closeup",
                "colorPaletteId": "warm-earth",
                "tags": ["regal", "dark"]
              }
            ]
            """;

        var mock = new MockLlmClient();
        mock.Enqueue(response);

        var fill = new CatalogLlmFill(mock);
        var result = await fill.FillFromTextAsync(
            images,
            availableStyles: ["oil-painting", "portrait-closeup"],
            availablePalettes: ["warm-earth"]);

        var updated = result.Single(r => r.ImageId == entry.ImageId);
        Assert.Equal("oil-painting", updated.ArtisticStyleId);
        Assert.Equal("portrait-closeup", updated.CompositionStyleId);
        Assert.Equal("warm-earth", updated.ColorPaletteId);
        Assert.Contains("regal", updated.Tags!);
    }

    // -------------------------------------------------------------------------
    // Test 12: FillFromTextAsync — LLM error leaves entry unchanged
    // -------------------------------------------------------------------------

    [Fact]
    public async Task FillFromText_LlmError_LeavesEntryUnchanged()
    {
        var entry = EntryFactory.MakeEntry(entityName: "Thornwood");
        var images = new[] { entry };

        var mock = new MockLlmClient();
        mock.EnqueueError("rate limit exceeded");

        var fill = new CatalogLlmFill(mock);
        var result = await fill.FillFromTextAsync(
            images,
            availableStyles: ["oil-painting"],
            availablePalettes: ["warm-earth"]);

        var unchanged = result.Single(r => r.ImageId == entry.ImageId);
        Assert.Null(unchanged.ArtisticStyleId);
    }

    // -------------------------------------------------------------------------
    // Test 13: FillTitlesAsync — valid response sets title on entry
    // -------------------------------------------------------------------------

    [Fact]
    public async Task FillTitles_ValidResponse_SetsTitleOnEntry()
    {
        var entry = EntryFactory.MakeEntry(entityName: "Thornwood");
        var images = new[] { entry };

        var response = $$"""
            [
              { "id": "{{entry.ImageId}}", "title": "The Last Bargain" }
            ]
            """;

        var mock = new MockLlmClient();
        mock.Enqueue(response);

        var fill = new CatalogLlmFill(mock);
        var result = await fill.FillTitlesAsync(images);

        var updated = result.Single(r => r.ImageId == entry.ImageId);
        Assert.Equal("The Last Bargain", updated.Title);
    }

    // -------------------------------------------------------------------------
    // Test 14: FillTitlesAsync — skips entries that already have a title
    // -------------------------------------------------------------------------

    [Fact]
    public async Task FillTitles_SkipsEntriesWithExistingTitle()
    {
        var entry = EntryFactory.MakeEntry(title: "Already Set", entityName: "Thornwood");
        var images = new[] { entry };

        // No responses queued — would throw if called
        var mock = new MockLlmClient();
        var fill = new CatalogLlmFill(mock);
        var result = await fill.FillTitlesAsync(images);

        Assert.Equal("Already Set", result[0].Title);
        Assert.Empty(mock.Requests);
    }
}

// ─── MockLlmClient (local copy scoped to this test file) ─────────────────────

file sealed class MockLlmClient : ILlmClient
{
    private readonly Queue<LlmResponse> _responses = new();
    private readonly List<LlmRequest> _requests = [];

    public IReadOnlyList<LlmRequest> Requests => _requests;

    public MockLlmClient Enqueue(string text) =>
        Enqueue(new LlmResponse { Text = text, Usage = new TokenUsage(10, 20) });

    public MockLlmClient EnqueueError(string error) =>
        Enqueue(new LlmResponse { Text = "", Usage = new TokenUsage(0, 0), Error = error });

    public MockLlmClient Enqueue(LlmResponse response)
    {
        _responses.Enqueue(response);
        return this;
    }

    public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
    {
        _requests.Add(request);
        if (_responses.Count == 0)
            throw new InvalidOperationException("MockLlmClient: no more responses queued");
        return Task.FromResult(_responses.Dequeue());
    }

    public IAsyncEnumerable<LlmChunk> StreamAsync(LlmRequest request, CancellationToken ct = default)
        => throw new NotSupportedException();
}
