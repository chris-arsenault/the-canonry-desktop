using TheCanonry.Illuminator.Enrichment.Prompts;
using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Tests.Enrichment;

public sealed class HistorianPromptTests
{
    // =========================================================================
    // ComputeWordBudget — base words by prominence level
    // =========================================================================

    [Theory]
    [InlineData("forgotten", 80)]
    [InlineData("marginal", 100)]
    [InlineData("recognized", 150)]
    [InlineData("renowned", 200)]
    [InlineData("mythic", 300)]
    public void ComputeWordBudget_BaseWordsByProminence_NoRevisions(string prominence, int expectedBase)
    {
        // With 0 revisions, dampening = max(0.60, 0.85^0) = max(0.60, 1.0) = 1.0
        // so budget = base * 1.0 = base exactly
        var budget = HistorianContextBuilder.ComputeWordBudget(prominence, revisionCount: 0);
        Assert.Equal(expectedBase, budget);
    }

    // =========================================================================
    // ComputeWordBudget — revision dampening reduces budget
    // =========================================================================

    [Fact]
    public void ComputeWordBudget_RevisionDampening_ReducesBudget()
    {
        // recognized = 150 base
        // 1 revision: 150 * max(0.60, 0.85^1) = 150 * 0.85 = 127.5 → ceil = 128
        var budgetWithRevision = HistorianContextBuilder.ComputeWordBudget("recognized", revisionCount: 1);
        var budgetWithoutRevision = HistorianContextBuilder.ComputeWordBudget("recognized", revisionCount: 0);

        Assert.True(budgetWithRevision < budgetWithoutRevision,
            $"Expected budget with revision ({budgetWithRevision}) to be less than without ({budgetWithoutRevision})");
        Assert.Equal(128, budgetWithRevision); // ceil(150 * 0.85)
    }

    [Fact]
    public void ComputeWordBudget_MultipleRevisions_DampeningIsCompounding()
    {
        // 3 revisions: 150 * 0.85^3 = 150 * 0.614125 = 92.12 → ceil = 93
        var budget3 = HistorianContextBuilder.ComputeWordBudget("recognized", revisionCount: 3);
        var budget1 = HistorianContextBuilder.ComputeWordBudget("recognized", revisionCount: 1);

        Assert.True(budget3 < budget1,
            $"Expected budget with 3 revisions ({budget3}) to be less than with 1 ({budget1})");
    }

    // =========================================================================
    // ComputeWordBudget — dampening floors at 60%
    // =========================================================================

    [Fact]
    public void ComputeWordBudget_DampeningFloorAt60Percent()
    {
        // Very high revision count: 0.85^100 ≈ 0 → floor kicks in → 60% of base
        // For recognized: floor = ceil(150 * 0.60) = 90
        var budgetHighRevision = HistorianContextBuilder.ComputeWordBudget("recognized", revisionCount: 100);
        var expectedFloor = (int)Math.Ceiling(150 * 0.60);

        Assert.Equal(expectedFloor, budgetHighRevision);
    }

    [Fact]
    public void ComputeWordBudget_FloorAppliesAcrossAllProminenceLevels()
    {
        // All prominence levels should floor at 60% of their base
        var prominenceFloors = new Dictionary<string, int>
        {
            ["forgotten"] = (int)Math.Ceiling(80 * 0.60),
            ["marginal"] = (int)Math.Ceiling(100 * 0.60),
            ["recognized"] = (int)Math.Ceiling(150 * 0.60),
            ["renowned"] = (int)Math.Ceiling(200 * 0.60),
            ["mythic"] = (int)Math.Ceiling(300 * 0.60),
        };

        foreach (var (prominence, expectedFloor) in prominenceFloors)
        {
            var actual = HistorianContextBuilder.ComputeWordBudget(prominence, revisionCount: 1000);
            Assert.Equal(expectedFloor, actual);
        }
    }

    // =========================================================================
    // BuildEditionSystemPrompt — contains tone and word budget
    // =========================================================================

    [Fact]
    public void BuildEditionSystemPrompt_ContainsToneDescription()
    {
        var entity = new EntitySnapshot("e1", "Test Entity", "person", "scholar", "renowned", "northern", "A description.");
        var ctx = new HistorianEditionContext(
            Entity: entity,
            Tone: "weary",
            WordBudget: 200,
            EntitySummary: "",
            RelationshipSummary: "",
            ChronicleSourcesSummary: "",
            WorldContext: null,
            PreviousAnnotationsSummary: "");

        var prompt = HistorianPrompts.BuildEditionSystemPrompt(ctx);

        // The weary tone has a distinctive phrase
        Assert.Contains("tired", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("history rhymes with itself", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildEditionSystemPrompt_ContainsWordBudget()
    {
        var entity = new EntitySnapshot("e1", "Hero", "person", "", "mythic", "eastern", "Epic tale.");
        var ctx = new HistorianEditionContext(
            Entity: entity,
            Tone: "scholarly",
            WordBudget: 300,
            EntitySummary: "",
            RelationshipSummary: "",
            ChronicleSourcesSummary: "",
            WorldContext: null,
            PreviousAnnotationsSummary: "");

        var prompt = HistorianPrompts.BuildEditionSystemPrompt(ctx);

        Assert.Contains("300 words", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildEditionSystemPrompt_ContainsOutputFormat()
    {
        var entity = new EntitySnapshot("e1", "Entity", "place", "", "marginal", "", null);
        var ctx = new HistorianEditionContext(
            Entity: entity,
            Tone: "forensic",
            WordBudget: 100,
            EntitySummary: "",
            RelationshipSummary: "",
            ChronicleSourcesSummary: "",
            WorldContext: null,
            PreviousAnnotationsSummary: "");

        var prompt = HistorianPrompts.BuildEditionSystemPrompt(ctx);

        Assert.Contains("patches", prompt);
        Assert.Contains("entityId", prompt);
        Assert.Contains("description", prompt);
    }

    // =========================================================================
    // BuildReviewSystemPrompt — contains note types and annotation constraints
    // =========================================================================

    [Fact]
    public void BuildReviewSystemPrompt_ContainsAllNoteTypes()
    {
        var ctx = new HistorianReviewContext(
            SourceText: "The battle was fierce.",
            Tone: "cantankerous",
            NoteType: "entity",
            RelationshipSummary: "",
            CanonFactsSummary: "",
            WorldDynamics: null,
            PreviousNotesSummary: "");

        var prompt = HistorianPrompts.BuildReviewSystemPrompt(ctx);

        Assert.Contains("commentary", prompt);
        Assert.Contains("correction", prompt);
        Assert.Contains("tangent", prompt);
        Assert.Contains("skepticism", prompt);
        Assert.Contains("pedantic", prompt);
        Assert.Contains("temporal", prompt);
    }

    [Fact]
    public void BuildReviewSystemPrompt_ContainsAnnotationConstraints()
    {
        var ctx = new HistorianReviewContext(
            SourceText: "Some source text.",
            Tone: "weary",
            NoteType: "chronicle",
            RelationshipSummary: "",
            CanonFactsSummary: "",
            WorldDynamics: null,
            PreviousNotesSummary: "");

        var prompt = HistorianPrompts.BuildReviewSystemPrompt(ctx);

        // Annotation word range
        Assert.Contains("20 to 100 words", prompt);
        // Note weight
        Assert.Contains("major", prompt);
        Assert.Contains("minor", prompt);
        // Anchor phrase exactness
        Assert.Contains("EXACT", prompt);
    }

    [Fact]
    public void BuildReviewSystemPrompt_ContainsToneDescription()
    {
        var ctx = new HistorianReviewContext(
            SourceText: "text",
            Tone: "elegiac",
            NoteType: "entity",
            RelationshipSummary: "",
            CanonFactsSummary: "",
            WorldDynamics: null,
            PreviousNotesSummary: "");

        var prompt = HistorianPrompts.BuildReviewSystemPrompt(ctx);

        // Elegiac tone distinctive phrase
        Assert.Contains("heaviness", prompt, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // BuildCorpusVoiceDigest — detects repeated openings
    // =========================================================================

    [Fact]
    public void BuildCorpusVoiceDigest_DetectsOverusedOpenings()
    {
        var annotations = new List<string>
        {
            "This is a very interesting observation about the entity",
            "This is a very different note about something else entirely",
            "This is a very long annotation that goes on for quite some time about history",
            "Something entirely different that does not share the opening",
            "Another note with a unique beginning here",
        };

        var digest = HistorianContextBuilder.BuildCorpusVoiceDigest(annotations);

        // "this is a very" appears 3 times — should be detected
        Assert.NotEmpty(digest.OverusedOpenings);
        Assert.Contains(digest.OverusedOpenings, o => o.Contains("this is a very", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildCorpusVoiceDigest_EmptyAnnotations_ReturnsEmptyDigest()
    {
        var digest = HistorianContextBuilder.BuildCorpusVoiceDigest([]);

        Assert.Empty(digest.OverusedOpenings);
        Assert.Empty(digest.OverusedSuperlatives);
        Assert.Equal(0, digest.AverageLength);
    }

    [Fact]
    public void BuildCorpusVoiceDigest_ComputesLengthStats()
    {
        var annotations = new List<string>
        {
            "Short note here",          // 3 words
            "A somewhat longer note that has more words in it than the short one",   // 14 words
            "The longest note in the batch with many words and extensive detail about the subject at hand", // 17 words
        };

        var digest = HistorianContextBuilder.BuildCorpusVoiceDigest(annotations);

        Assert.True(digest.MinLength <= digest.MaxLength);
        Assert.True(digest.AverageLength >= digest.MinLength);
        Assert.True(digest.AverageLength <= digest.MaxLength);
    }

    // =========================================================================
    // BuildChronologySystemPrompt
    // =========================================================================

    [Fact]
    public void BuildChronologySystemPrompt_ContainsOutputFormatAndRules()
    {
        var prompt = HistorianPrompts.BuildChronologySystemPrompt();

        Assert.Contains("chronology", prompt);
        Assert.Contains("chronicleId", prompt);
        Assert.Contains("year", prompt);
        Assert.Contains("reasoning", prompt);
    }

    // =========================================================================
    // BuildPrepSystemPrompt
    // =========================================================================

    [Fact]
    public void BuildPrepSystemPrompt_ContainsToneAndTaskDescription()
    {
        var prompt = HistorianPrompts.BuildPrepSystemPrompt("witty");

        // Witty tone distinctive phrase
        Assert.Contains("sharp", prompt, StringComparison.OrdinalIgnoreCase);
        // Task description
        Assert.Contains("300-500 words", prompt);
        Assert.Contains("private", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildPrepUserPrompt_TruncatesLongContent()
    {
        // Build a content string with more than 3000 words
        var words = Enumerable.Repeat("word", 4000);
        var longContent = string.Join(" ", words);

        var prompt = HistorianPrompts.BuildPrepUserPrompt(longContent, summary: null);

        Assert.Contains("truncated for brevity", prompt);
    }

    [Fact]
    public void BuildPrepUserPrompt_ShortContent_NotTruncated()
    {
        var content = "A short chronicle with only a few words.";
        var prompt = HistorianPrompts.BuildPrepUserPrompt(content, summary: "A summary.");

        Assert.DoesNotContain("truncated", prompt);
        Assert.Contains("A summary.", prompt);
        Assert.Contains(content, prompt);
    }

    // =========================================================================
    // BuildEditionUserPrompt — contains entity data
    // =========================================================================

    [Fact]
    public void BuildEditionUserPrompt_ContainsEntityNameAndTask()
    {
        var entity = new EntitySnapshot("e42", "The Iron Warden", "artifact", "weapon", "renowned", "forge-born", "A legendary blade.");
        var ctx = new HistorianEditionContext(
            Entity: entity,
            Tone: "forensic",
            WordBudget: 200,
            EntitySummary: "Canonical summary text here.",
            RelationshipSummary: "",
            ChronicleSourcesSummary: "",
            WorldContext: null,
            PreviousAnnotationsSummary: "");

        var prompt = HistorianPrompts.BuildEditionUserPrompt(ctx);

        Assert.Contains("The Iron Warden", prompt);
        Assert.Contains("200 words", prompt);
        Assert.Contains("e42", prompt);
    }
}
