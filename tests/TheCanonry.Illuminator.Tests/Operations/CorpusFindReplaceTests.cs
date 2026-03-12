namespace TheCanonry.Illuminator.Tests.Operations;

using TheCanonry.Illuminator.Operations;
using TheCanonry.Persistence.Entities;

// ─── Helpers ──────────────────────────────────────────────────────────────────

file static class CorpusFactory
{
    private static int _chronicleSeq;
    private static int _pageSeq;

    public static PersistedEntity MakeEntity(
        string id,
        string name,
        string description = "") => new()
    {
        Id              = id,
        SimulationRunId = "sim-1",
        Kind            = "character",
        Subtype         = "npc",
        Name            = name,
        Description     = description,
        Summary         = "",
        Status          = "active",
        Prominence      = 0.5,
        Culture         = "default",
        EraId           = "era-1",
        CoordX          = 0,
        CoordY          = 0,
        CoordZ          = 0,
        CreatedAtTick   = 1,
        UpdatedAtTick   = 1,
    };

    public static Chronicle MakeChronicle(
        string content,
        string? title   = null,
        string? summary = null) => new()
    {
        Id              = ++_chronicleSeq,
        SimulationRunId = "sim-1",
        EntityIds       = "[]",
        Format          = "narrative",
        Content         = content,
        Title           = title,
        Summary         = summary,
    };

    public static StaticPageEntity MakePage(
        string content,
        string title    = "Page") => new()
    {
        PageId          = $"page-{++_pageSeq}",
        ProjectId       = "proj-1",
        SimulationRunId = "sim-1",
        Title           = title,
        Slug            = title.ToLowerInvariant(),
        Content         = content,
        Status          = "draft",
    };
}

// ─── CorpusFindReplace.Search Tests ───────────────────────────────────────────

public sealed class CorpusFindReplaceSearchTests
{
    // -------------------------------------------------------------------------
    // Test 1: Finds matches in entity descriptions
    // -------------------------------------------------------------------------

    [Fact]
    public void Search_FindsMatchesInEntityDescriptions()
    {
        var entities = new[]
        {
            CorpusFactory.MakeEntity("e1", "Alpha", description: "The ancient Thornwood keep."),
            CorpusFactory.MakeEntity("e2", "Beta",  description: "Nothing relevant here."),
        };

        var matches = CorpusFindReplace.Search(
            query:        "Thornwood",
            caseSensitive: false,
            contexts:     [CorpusFindReplace.SearchContext.EntityDescriptions],
            entities:     entities);

        Assert.Single(matches);
        Assert.Equal(CorpusFindReplace.SearchContext.EntityDescriptions, matches[0].Context);
        Assert.Equal("e1", matches[0].SourceId);
        Assert.Equal("Description", matches[0].Field);
    }

    // -------------------------------------------------------------------------
    // Test 2: Finds multiple occurrences in the same text
    // -------------------------------------------------------------------------

    [Fact]
    public void Search_FindsMultipleOccurrencesInSameText()
    {
        var entities = new[]
        {
            CorpusFactory.MakeEntity("e1", "Ent",
                description: "Thornwood is old. Near Thornwood there is a lake. Thornwood endures."),
        };

        var matches = CorpusFindReplace.Search(
            query:         "Thornwood",
            caseSensitive: false,
            contexts:      [CorpusFindReplace.SearchContext.EntityDescriptions],
            entities:      entities);

        Assert.Equal(3, matches.Count);
        // Positions should be strictly ascending
        for (var i = 1; i < matches.Count; i++)
            Assert.True(matches[i].Position > matches[i - 1].Position);
    }

    // -------------------------------------------------------------------------
    // Test 3: Respects case sensitivity toggle — case-sensitive misses lowercase
    // -------------------------------------------------------------------------

    [Fact]
    public void Search_CaseSensitive_DoesNotMatchDifferentCase()
    {
        var entities = new[]
        {
            CorpusFactory.MakeEntity("e1", "Ent",
                description: "the thornwood keep was here."),
        };

        var matches = CorpusFindReplace.Search(
            query:         "Thornwood",
            caseSensitive: true,
            contexts:      [CorpusFindReplace.SearchContext.EntityDescriptions],
            entities:      entities);

        Assert.Empty(matches);
    }

    // -------------------------------------------------------------------------
    // Test 4: Respects case sensitivity toggle — case-insensitive matches
    // -------------------------------------------------------------------------

    [Fact]
    public void Search_CaseInsensitive_MatchesDifferentCase()
    {
        var entities = new[]
        {
            CorpusFactory.MakeEntity("e1", "Ent",
                description: "the thornwood keep was here."),
        };

        var matches = CorpusFindReplace.Search(
            query:         "Thornwood",
            caseSensitive: false,
            contexts:      [CorpusFindReplace.SearchContext.EntityDescriptions],
            entities:      entities);

        Assert.Single(matches);
        Assert.Equal("thornwood", matches[0].MatchedText);
    }

    // -------------------------------------------------------------------------
    // Test 5: Finds matches in chronicle content
    // -------------------------------------------------------------------------

    [Fact]
    public void Search_FindsMatchesInChronicleContent()
    {
        var chronicles = new[]
        {
            CorpusFactory.MakeChronicle(
                content: "The saga of Thornwood began long ago.",
                title:   "The Thornwood Saga"),
        };

        var matches = CorpusFindReplace.Search(
            query:         "Thornwood",
            caseSensitive: false,
            contexts:      [CorpusFindReplace.SearchContext.ChronicleContent],
            chronicles:    chronicles);

        Assert.Single(matches);
        Assert.Equal(CorpusFindReplace.SearchContext.ChronicleContent, matches[0].Context);
        Assert.Equal("Content", matches[0].Field);
    }

    // -------------------------------------------------------------------------
    // Test 6: Chronicle title context works independently from content context
    // -------------------------------------------------------------------------

    [Fact]
    public void Search_ChronicleTitle_SearchedOnlyWhenContextSelected()
    {
        var chronicles = new[]
        {
            CorpusFactory.MakeChronicle(
                content: "No match here.",
                title:   "The Thornwood Saga"),
        };

        var noTitleMatch = CorpusFindReplace.Search(
            query:         "Thornwood",
            caseSensitive: false,
            contexts:      [CorpusFindReplace.SearchContext.ChronicleContent],
            chronicles:    chronicles);

        var titleMatch = CorpusFindReplace.Search(
            query:         "Thornwood",
            caseSensitive: false,
            contexts:      [CorpusFindReplace.SearchContext.ChronicleTitles],
            chronicles:    chronicles);

        Assert.Empty(noTitleMatch);
        Assert.Single(titleMatch);
        Assert.Equal("Title", titleMatch[0].Field);
    }

    // -------------------------------------------------------------------------
    // Test 7: Finds matches in static page content
    // -------------------------------------------------------------------------

    [Fact]
    public void Search_FindsMatchesInStaticPageContent()
    {
        var pages = new[]
        {
            CorpusFactory.MakePage(content: "Thornwood was settled in the second era.", title: "History"),
        };

        var matches = CorpusFindReplace.Search(
            query:         "Thornwood",
            caseSensitive: false,
            contexts:      [CorpusFindReplace.SearchContext.StaticPageContent],
            staticPages:   pages);

        Assert.Single(matches);
        Assert.Equal(CorpusFindReplace.SearchContext.StaticPageContent, matches[0].Context);
    }

    // -------------------------------------------------------------------------
    // Test 8: Context window is ≤ ContextRadius characters
    // -------------------------------------------------------------------------

    [Fact]
    public void Search_ContextWindow_WithinRadius()
    {
        var longPrefix = new string('x', 200);
        var longSuffix = new string('y', 200);
        var entities = new[]
        {
            CorpusFactory.MakeEntity("e1", "E", description: $"{longPrefix}Thornwood{longSuffix}"),
        };

        var matches = CorpusFindReplace.Search(
            query:         "Thornwood",
            caseSensitive: false,
            contexts:      [CorpusFindReplace.SearchContext.EntityDescriptions],
            entities:      entities);

        var m = Assert.Single(matches);
        Assert.True(m.ContextBefore.Length <= CorpusFindReplace.ContextRadius);
        Assert.True(m.ContextAfter.Length  <= CorpusFindReplace.ContextRadius);
    }

    // -------------------------------------------------------------------------
    // Test 9: Empty query returns no matches
    // -------------------------------------------------------------------------

    [Fact]
    public void Search_EmptyQuery_ReturnsEmpty()
    {
        var entities = new[]
        {
            CorpusFactory.MakeEntity("e1", "E", description: "Some text."),
        };

        var matches = CorpusFindReplace.Search(
            query:         "",
            caseSensitive: false,
            contexts:      [CorpusFindReplace.SearchContext.EntityDescriptions],
            entities:      entities);

        Assert.Empty(matches);
    }

    // -------------------------------------------------------------------------
    // Test 10: Each match gets a unique ID
    // -------------------------------------------------------------------------

    [Fact]
    public void Search_EachMatchGetsUniqueId()
    {
        var entities = new[]
        {
            CorpusFactory.MakeEntity("e1", "E",
                description: "alpha alpha alpha"),
        };

        var matches = CorpusFindReplace.Search(
            query:         "alpha",
            caseSensitive: false,
            contexts:      [CorpusFindReplace.SearchContext.EntityDescriptions],
            entities:      entities);

        Assert.Equal(3, matches.Count);
        var ids = matches.Select(m => m.Id).ToHashSet();
        Assert.Equal(3, ids.Count);
    }
}

// ─── CorpusFindReplace.ApplyReplacements Tests ────────────────────────────────

public sealed class CorpusFindReplaceApplyTests
{
    // -------------------------------------------------------------------------
    // Test 11: ApplyReplacements replaces at correct single position
    // -------------------------------------------------------------------------

    [Fact]
    public void ApplyReplacements_SinglePosition_ReplacesCorrectly()
    {
        const string text = "The Thornwood keep stood.";
        var result = CorpusFindReplace.ApplyReplacements(text, "Ironvale", [4], "Thornwood".Length);
        Assert.Equal("The Ironvale keep stood.", result);
    }

    // -------------------------------------------------------------------------
    // Test 12: ApplyReplacements handles multiple positions in reverse order
    // -------------------------------------------------------------------------

    [Fact]
    public void ApplyReplacements_MultiplePositions_AllReplacedCorrectly()
    {
        const string text = "alpha and alpha.";
        // "alpha" at positions 0 and 10
        var result = CorpusFindReplace.ApplyReplacements(text, "beta", [0, 10], "alpha".Length);
        Assert.Equal("beta and beta.", result);
    }

    // -------------------------------------------------------------------------
    // Test 13: ApplyReplacements with empty positions returns original text
    // -------------------------------------------------------------------------

    [Fact]
    public void ApplyReplacements_EmptyPositions_ReturnsOriginalText()
    {
        const string text = "Unchanged text.";
        var result = CorpusFindReplace.ApplyReplacements(text, "anything", [], 5);
        Assert.Equal(text, result);
    }

    // -------------------------------------------------------------------------
    // Test 14: ApplyReplacements with replacement longer than original
    // -------------------------------------------------------------------------

    [Fact]
    public void ApplyReplacements_LongerReplacement_ProducesCorrectResult()
    {
        const string text = "Short here.";
        // "Short" is 5 chars at position 0; replace with "Considerably longer"
        var result = CorpusFindReplace.ApplyReplacements(text, "Considerably longer", [0], 5);
        Assert.Equal("Considerably longer here.", result);
    }
}

// ─── EntityCoverageAnalysis Tests ─────────────────────────────────────────────

public sealed class EntityCoverageAnalysisTests
{
    // -------------------------------------------------------------------------
    // Test 15: Analyze computes correct chronicle backref counts
    // -------------------------------------------------------------------------

    [Fact]
    public void Analyze_ComputesCorrectBackrefCounts()
    {
        var entities = new[]
        {
            CorpusFactory.MakeEntity("e1", "Alpha"),
            CorpusFactory.MakeEntity("e2", "Beta"),
        };

        var chronicles = new[]
        {
            new Chronicle
            {
                Id              = 1,
                SimulationRunId = "sim-1",
                EntityIds       = """["e1","e2"]""",
                Format          = "narrative",
                Content         = "Content one.",
            },
            new Chronicle
            {
                Id              = 2,
                SimulationRunId = "sim-1",
                EntityIds       = """["e1"]""",
                Format          = "narrative",
                Content         = "Content two.",
            },
        };

        var coverage = EntityCoverageAnalysis.Analyze(entities, chronicles);

        var e1 = Assert.Single(coverage, c => c.EntityId == "e1");
        var e2 = Assert.Single(coverage, c => c.EntityId == "e2");

        Assert.Equal(2, e1.ChronicleBackrefs);
        Assert.Equal(1, e2.ChronicleBackrefs);
    }

    // -------------------------------------------------------------------------
    // Test 16: Analyze computes correct event counts
    // -------------------------------------------------------------------------

    [Fact]
    public void Analyze_ComputesCorrectEventCounts()
    {
        var entities = new[]
        {
            CorpusFactory.MakeEntity("e1", "Alpha"),
        };

        var events = new[]
        {
            new NarrativeEventEntity { Id = 1, SimulationRunId = "sim-1", SubjectId = "e1", Tick = 1, EraId = "era-1", EventKind = "created", Significance = 0.5, Action = "created", Description = "desc" },
            new NarrativeEventEntity { Id = 2, SimulationRunId = "sim-1", SubjectId = "e1", Tick = 2, EraId = "era-1", EventKind = "evolved", Significance = 0.5, Action = "evolved", Description = "desc" },
            new NarrativeEventEntity { Id = 3, SimulationRunId = "sim-1", SubjectId = "e2", Tick = 3, EraId = "era-1", EventKind = "created", Significance = 0.5, Action = "created", Description = "desc" },
        };

        var coverage = EntityCoverageAnalysis.Analyze(entities, [], events);

        var e1 = Assert.Single(coverage, c => c.EntityId == "e1");
        Assert.Equal(2, e1.EventCount);
    }

    // -------------------------------------------------------------------------
    // Test 17: Analyze HasDescription is true only for non-empty descriptions
    // -------------------------------------------------------------------------

    [Fact]
    public void Analyze_HasDescription_TrueOnlyForNonEmpty()
    {
        var entities = new[]
        {
            CorpusFactory.MakeEntity("e1", "WithDesc", description: "Some description."),
            CorpusFactory.MakeEntity("e2", "NoDesc",   description: ""),
        };

        var coverage = EntityCoverageAnalysis.Analyze(entities, []);

        Assert.True(Assert.Single(coverage, c => c.EntityId == "e1").HasDescription);
        Assert.False(Assert.Single(coverage, c => c.EntityId == "e2").HasDescription);
    }

    // -------------------------------------------------------------------------
    // Test 18: ExpectedForProminence returns correct values
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0.05, 0)]   // forgotten
    [InlineData(0.15, 0)]   // marginal
    [InlineData(0.30, 1)]   // recognized (boundary)
    [InlineData(0.50, 1)]   // recognized
    [InlineData(0.60, 2)]   // renowned (boundary)
    [InlineData(0.75, 2)]   // renowned
    [InlineData(0.80, 3)]   // mythic (boundary)
    [InlineData(0.95, 3)]   // mythic
    public void ExpectedForProminence_ReturnsCorrectValues(double prominence, int expected)
    {
        Assert.Equal(expected, EntityCoverageAnalysis.ExpectedForProminence(prominence));
    }

    // -------------------------------------------------------------------------
    // Test 19: ProminenceLabel returns correct labels
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0.05, "forgotten")]
    [InlineData(0.15, "marginal")]
    [InlineData(0.30, "recognized")]
    [InlineData(0.50, "recognized")]
    [InlineData(0.65, "renowned")]
    [InlineData(0.85, "mythic")]
    public void ProminenceLabel_ReturnsCorrectLabel(double value, string expectedLabel)
    {
        Assert.Equal(expectedLabel, EntityCoverageAnalysis.ProminenceLabel(value));
    }

    // -------------------------------------------------------------------------
    // Test 20: Analyze with empty inputs returns empty list
    // -------------------------------------------------------------------------

    [Fact]
    public void Analyze_EmptyInputs_ReturnsEmpty()
    {
        var coverage = EntityCoverageAnalysis.Analyze([], []);
        Assert.Empty(coverage);
    }
}
