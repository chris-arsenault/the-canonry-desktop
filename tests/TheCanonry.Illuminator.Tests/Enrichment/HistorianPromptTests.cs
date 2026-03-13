using TheCanonry.Illuminator.Enrichment.Prompts;

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
    public void BuildEditionUserPrompt_ContainsWordBudget()
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

        var prompt = HistorianPrompts.BuildEditionUserPrompt(ctx);

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
        Assert.Empty(digest.SuperlativeClaims);
        Assert.Equal(0, digest.TotalNotes);
        Assert.Equal(0, digest.LengthHistogram.Total);
        Assert.Equal(0, digest.TangentCount);
    }

    [Fact]
    public void BuildCorpusVoiceDigest_ComputesLengthHistogram()
    {
        // Short: ≤35 words, Medium: 36-70 words, Long: 71+ words
        var shortNote = string.Join(" ", Enumerable.Repeat("word", 20));   // 20 words → short
        var mediumNote = string.Join(" ", Enumerable.Repeat("word", 50));  // 50 words → medium
        var longNote = string.Join(" ", Enumerable.Repeat("word", 80));    // 80 words → long

        var digest = HistorianContextBuilder.BuildCorpusVoiceDigest([shortNote, mediumNote, longNote]);

        Assert.Equal(3, digest.TotalNotes);
        Assert.Equal(1, digest.LengthHistogram.Short);
        Assert.Equal(1, digest.LengthHistogram.Medium);
        Assert.Equal(1, digest.LengthHistogram.Long);
        Assert.Equal(3, digest.LengthHistogram.Total);
    }

    [Fact]
    public void BuildCorpusVoiceDigest_CountsTangents()
    {
        var annotations = new List<string> { "Note one.", "Note two.", "Tangent note." };
        var noteTypes = new List<string> { "commentary", "tangent", "tangent" };

        var digest = HistorianContextBuilder.BuildCorpusVoiceDigest(annotations, noteTypes, targetCount: 5);

        Assert.Equal(2, digest.TangentCount);
        Assert.Equal(5, digest.TargetCount);
    }

    [Fact]
    public void BuildCorpusVoiceDigest_DetectsSuperlativeClaims()
    {
        var annotations = new List<string>
        {
            "This is the most important discovery in the archive.",
            "Also the most important discovery in the archive, they say.",
            "The only surviving record of the eastern campaign.",
        };

        var digest = HistorianContextBuilder.BuildCorpusVoiceDigest(annotations);

        Assert.NotEmpty(digest.SuperlativeClaims);
        // "the most important discovery in the archive" appears twice → [repeated]
        Assert.Contains(digest.SuperlativeClaims, c => c.StartsWith("[repeated]"));
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

    // =========================================================================
    // ComputeNoteRange — entity mode boundary tests
    // =========================================================================

    [Theory]
    [InlineData(100, 1, 1)]    // <150 words
    [InlineData(200, 1, 3)]    // 150–299 words
    [InlineData(400, 2, 4)]    // 300–599 words
    [InlineData(800, 3, 6)]    // 600–1199 words
    [InlineData(2000, 4, 8)]   // 1200+ words
    public void ComputeNoteRange_EntityMode_CorrectBoundaries(int wordCount, int expectedMin, int expectedMax)
    {
        var (min, max) = HistorianPrompts.ComputeNoteRange("entity", wordCount);
        Assert.Equal(expectedMin, min);
        Assert.Equal(expectedMax, max);
    }

    // =========================================================================
    // ComputeNoteRange — chronicle mode boundary tests
    // =========================================================================

    [Theory]
    [InlineData(200, 1, 2)]    // <300 words
    [InlineData(500, 2, 3)]    // 300–799 words
    [InlineData(1000, 3, 5)]   // 800–1499 words
    [InlineData(2000, 5, 8)]   // 1500–2999 words
    [InlineData(5000, 8, 13)]  // 3000+ words
    public void ComputeNoteRange_ChronicleMode_CorrectBoundaries(int wordCount, int expectedMin, int expectedMax)
    {
        var (min, max) = HistorianPrompts.ComputeNoteRange("chronicle", wordCount);
        Assert.Equal(expectedMin, min);
        Assert.Equal(expectedMax, max);
    }

    // =========================================================================
    // Review system prompt — 3 mode framings
    // =========================================================================

    [Fact]
    public void BuildReviewSystemPrompt_EntityMode_ContainsEntityFraming()
    {
        var ctx = new HistorianReviewContext(
            SourceText: "A description of the entity.",
            Tone: "scholarly",
            NoteType: "entity",
            RelationshipSummary: "",
            CanonFactsSummary: "",
            WorldDynamics: null,
            PreviousNotesSummary: "");

        var prompt = HistorianPrompts.BuildReviewSystemPrompt(ctx);

        Assert.Contains("definitive encyclopedia entry", prompt);
        Assert.Contains("your voice lives", prompt);
        Assert.Contains("pointing at the page", prompt);
    }

    [Fact]
    public void BuildReviewSystemPrompt_DocumentMode_ContainsDocumentFraming()
    {
        var ctx = new HistorianReviewContext(
            SourceText: "An official decree from the council.",
            Tone: "forensic",
            NoteType: "chronicle",
            RelationshipSummary: "",
            CanonFactsSummary: "",
            WorldDynamics: null,
            PreviousNotesSummary: "");

        var prompt = HistorianPrompts.BuildReviewSystemPrompt(ctx, chronicleFormat: "document");

        Assert.Contains("primary-source documents", prompt);
        Assert.Contains("institutional texts", prompt);
        Assert.Contains("functionaries", prompt);
    }

    [Fact]
    public void BuildReviewSystemPrompt_StoryMode_ContainsChroniclerFraming()
    {
        var ctx = new HistorianReviewContext(
            SourceText: "A chronicle of the war.",
            Tone: "weary",
            NoteType: "chronicle",
            RelationshipSummary: "",
            CanonFactsSummary: "",
            WorldDynamics: null,
            PreviousNotesSummary: "");

        var prompt = HistorianPrompts.BuildReviewSystemPrompt(ctx, chronicleFormat: "story");

        Assert.Contains("written by other chroniclers", prompt);
        Assert.Contains("scholarly editor", prompt);
    }

    // =========================================================================
    // Review system prompt — dynamic note range (not hardcoded)
    // =========================================================================

    [Fact]
    public void BuildReviewSystemPrompt_DynamicNoteRange_NotHardcoded()
    {
        // A short text (~5 words) → entity mode → (1,1) range
        var ctx = new HistorianReviewContext(
            SourceText: "A short entity description.",
            Tone: "scholarly",
            NoteType: "entity",
            RelationshipSummary: "",
            CanonFactsSummary: "",
            WorldDynamics: null,
            PreviousNotesSummary: "");

        var prompt = HistorianPrompts.BuildReviewSystemPrompt(ctx);

        Assert.Contains("1–1", prompt);
        // A longer chronicle text should produce different range
        var longText = string.Join(" ", Enumerable.Repeat("word", 2000));
        var ctxLong = new HistorianReviewContext(
            SourceText: longText,
            Tone: "scholarly",
            NoteType: "chronicle",
            RelationshipSummary: "",
            CanonFactsSummary: "",
            WorldDynamics: null,
            PreviousNotesSummary: "");

        var promptLong = HistorianPrompts.BuildReviewSystemPrompt(ctxLong);

        Assert.Contains("5–8", promptLong);
    }

    // =========================================================================
    // Review system prompt — all 9 tones produce text
    // =========================================================================

    [Theory]
    [InlineData("scholarly")]
    [InlineData("witty")]
    [InlineData("weary")]
    [InlineData("forensic")]
    [InlineData("elegiac")]
    [InlineData("cantankerous")]
    [InlineData("rueful")]
    [InlineData("conspiratorial")]
    [InlineData("bemused")]
    public void BuildReviewSystemPrompt_AllNineTones_ProduceText(string tone)
    {
        var ctx = new HistorianReviewContext(
            SourceText: "Some text to annotate.",
            Tone: tone,
            NoteType: "entity",
            RelationshipSummary: "",
            CanonFactsSummary: "",
            WorldDynamics: null,
            PreviousNotesSummary: "");

        var prompt = HistorianPrompts.BuildReviewSystemPrompt(ctx);

        Assert.True(prompt.Length > 500, $"Expected review prompt for tone '{tone}' to be substantial");
        Assert.Contains("How You Feel Today", prompt);
    }

    // =========================================================================
    // Edition/Prep/Chronology — only 6 tones (no rueful/conspiratorial/bemused)
    // =========================================================================

    [Theory]
    [InlineData("scholarly")]
    [InlineData("witty")]
    [InlineData("weary")]
    [InlineData("forensic")]
    [InlineData("elegiac")]
    [InlineData("cantankerous")]
    public void BuildEditionSystemPrompt_SixTones_ProduceText(string tone)
    {
        var entity = new EntitySnapshot("e1", "Test", "person", "", "recognized", "", null);
        var ctx = new HistorianEditionContext(
            Entity: entity, Tone: tone, WordBudget: 150,
            EntitySummary: "", RelationshipSummary: "",
            ChronicleSourcesSummary: "", WorldContext: null,
            PreviousAnnotationsSummary: "");

        var prompt = HistorianPrompts.BuildEditionSystemPrompt(ctx);

        Assert.True(prompt.Length > 200, $"Expected edition prompt for tone '{tone}' to produce text");
    }

    [Theory]
    [InlineData("scholarly")]
    [InlineData("witty")]
    [InlineData("weary")]
    [InlineData("forensic")]
    [InlineData("elegiac")]
    [InlineData("cantankerous")]
    public void BuildPrepSystemPrompt_SixTones_ProduceText(string tone)
    {
        var prompt = HistorianPrompts.BuildPrepSystemPrompt(tone);

        Assert.True(prompt.Length > 200, $"Expected prep prompt for tone '{tone}' to produce text");
    }

    // =========================================================================
    // BuildVoiceDigestSection — adaptive guidance
    // =========================================================================

    [Fact]
    public void BuildVoiceDigestSection_NullDigest_ReturnsNull()
    {
        Assert.Null(HistorianPrompts.BuildVoiceDigestSection(null));
    }

    [Fact]
    public void BuildVoiceDigestSection_EmptyDigest_ReturnsNull()
    {
        var digest = new CorpusVoiceDigest(0, new CorpusLengthHistogram(0, 0, 0, 0), [], [], 0, 0);
        Assert.Null(HistorianPrompts.BuildVoiceDigestSection(digest));
    }

    [Fact]
    public void BuildVoiceDigestSection_DominantMedium_WarnsAboutClustering()
    {
        // 80% medium → should warn about clustering
        var hist = new CorpusLengthHistogram(Short: 1, Medium: 8, Long: 1, Total: 10);
        var digest = new CorpusVoiceDigest(10, hist, [], [], 0, 0);

        var result = HistorianPrompts.BuildVoiceDigestSection(digest);

        Assert.NotNull(result);
        Assert.Contains("clustering in the medium range", result);
    }

    [Fact]
    public void BuildVoiceDigestSection_DominantShort_WarnsAboutShort()
    {
        var hist = new CorpusLengthHistogram(Short: 8, Medium: 1, Long: 1, Total: 10);
        var digest = new CorpusVoiceDigest(10, hist, [], [], 0, 0);

        var result = HistorianPrompts.BuildVoiceDigestSection(digest);

        Assert.NotNull(result);
        Assert.Contains("running short", result);
    }

    [Fact]
    public void BuildVoiceDigestSection_DominantLong_WarnsAboutLong()
    {
        var hist = new CorpusLengthHistogram(Short: 1, Medium: 1, Long: 8, Total: 10);
        var digest = new CorpusVoiceDigest(10, hist, [], [], 0, 0);

        var result = HistorianPrompts.BuildVoiceDigestSection(digest);

        Assert.NotNull(result);
        Assert.Contains("running long", result);
    }

    [Fact]
    public void BuildVoiceDigestSection_WithTangents_ShowsTangentBudget()
    {
        var hist = new CorpusLengthHistogram(3, 3, 4, 10);
        var digest = new CorpusVoiceDigest(10, hist, [], [], TangentCount: 3, TargetCount: 5);

        var result = HistorianPrompts.BuildVoiceDigestSection(digest);

        Assert.NotNull(result);
        Assert.Contains("PERSONAL TANGENT BUDGET", result);
        Assert.Contains("3 personal tangents", result);
        Assert.Contains("5 annotation sessions", result);
    }

    [Fact]
    public void BuildVoiceDigestSection_WithOverusedOpenings_ShowsOpenings()
    {
        var hist = new CorpusLengthHistogram(3, 3, 4, 10);
        var digest = new CorpusVoiceDigest(
            10, hist, [],
            OverusedOpenings: ["\"this is a very...\" (x4)"],
            TangentCount: 0, TargetCount: 0);

        var result = HistorianPrompts.BuildVoiceDigestSection(digest);

        Assert.NotNull(result);
        Assert.Contains("OVERUSED ANNOTATION OPENINGS", result);
        Assert.Contains("this is a very", result);
    }

    [Fact]
    public void BuildVoiceDigestSection_ContainsCorpusVoiceDigestHeader()
    {
        var hist = new CorpusLengthHistogram(3, 3, 4, 10);
        var digest = new CorpusVoiceDigest(10, hist, [], [], 0, 0);

        var result = HistorianPrompts.BuildVoiceDigestSection(digest);

        Assert.NotNull(result);
        Assert.Contains("=== CORPUS VOICE DIGEST ===", result);
    }

    // =========================================================================
    // BuildFactCoverageGuidanceSection
    // =========================================================================

    [Fact]
    public void BuildFactCoverageGuidanceSection_EmptyTargets_ReturnsNull()
    {
        Assert.Null(HistorianPrompts.BuildFactCoverageGuidanceSection([], (3, 8)));
    }

    [Fact]
    public void BuildFactCoverageGuidanceSection_SurfaceTargets_ShowsRequired()
    {
        var targets = new List<FactGuidanceTarget>
        {
            new("f1", "surface", "passage about the iron tower", null),
        };

        var result = HistorianPrompts.BuildFactCoverageGuidanceSection(targets, (3, 8));

        Assert.NotNull(result);
        Assert.Contains("=== FACT COVERAGE GUIDANCE ===", result);
        Assert.Contains("REQUIRED", result);
        Assert.Contains("f1", result);
        Assert.Contains("passage about the iron tower", result);
    }

    [Fact]
    public void BuildFactCoverageGuidanceSection_ConnectTargets_ShowsFactText()
    {
        var targets = new List<FactGuidanceTarget>
        {
            new("f2", "connect", null, "The council controls all trade routes."),
        };

        var result = HistorianPrompts.BuildFactCoverageGuidanceSection(targets, (3, 8));

        Assert.NotNull(result);
        Assert.Contains("REQUIRED", result);
        Assert.Contains("f2", result);
        Assert.Contains("The council controls all trade routes.", result);
    }

    [Fact]
    public void BuildFactCoverageGuidanceSection_SmallNoteRange_LimitsRequired()
    {
        // noteRange.Max <= 4 → maxRequired = 1 → only first target is required
        var targets = new List<FactGuidanceTarget>
        {
            new("f1", "surface", "evidence 1", null),
            new("f2", "connect", null, "Fact text 2"),
        };

        var result = HistorianPrompts.BuildFactCoverageGuidanceSection(targets, (1, 3));

        Assert.NotNull(result);
        Assert.Contains("REQUIRED", result);
        Assert.Contains("OPTIONAL", result);
        Assert.Contains("f1", result);
        Assert.Contains("f2", result);
    }

    // =========================================================================
    // Review entity user prompt
    // =========================================================================

    [Fact]
    public void BuildReviewEntityUserPrompt_ContainsEntityIdentity()
    {
        var ctx = new HistorianReviewContext(
            SourceText: "A fearsome warrior.",
            Tone: "weary",
            NoteType: "entity",
            RelationshipSummary: "  - allied_with → Kara",
            CanonFactsSummary: "",
            WorldDynamics: null,
            PreviousNotesSummary: "")
        {
            EntityName = "Iron Warden",
            EntityKind = "npc",
            EntitySubtype = "warrior",
            EntityCulture = "northern",
            EntityProminence = "renowned",
            Summary = "A legendary guardian of the northern gates.",
        };

        var prompt = HistorianPrompts.BuildReviewEntityUserPrompt(ctx);

        Assert.Contains("=== ENTITY ===", prompt);
        Assert.Contains("Iron Warden", prompt);
        Assert.Contains("npc / warrior", prompt);
        Assert.Contains("northern", prompt);
        Assert.Contains("renowned", prompt);
        Assert.Contains("=== SUMMARY (for context) ===", prompt);
        Assert.Contains("legendary guardian", prompt);
        Assert.Contains("=== RELATIONSHIPS ===", prompt);
        Assert.Contains("allied_with", prompt);
        Assert.Contains("=== DESCRIPTION TO ANNOTATE ===", prompt);
        Assert.Contains("A fearsome warrior.", prompt);
        Assert.Contains("=== YOUR TASK ===", prompt);
    }

    [Fact]
    public void BuildReviewEntityUserPrompt_WithNeighborSummaries_ShowsRelatedEntities()
    {
        var ctx = new HistorianReviewContext(
            SourceText: "Text.",
            Tone: "scholarly",
            NoteType: "entity",
            RelationshipSummary: "",
            CanonFactsSummary: "",
            WorldDynamics: null,
            PreviousNotesSummary: "")
        {
            EntityName = "Test",
            EntityKind = "npc",
            NeighborSummaries = [new NeighborSummary("Kara", "npc", "A warrior queen.")],
        };

        var prompt = HistorianPrompts.BuildReviewEntityUserPrompt(ctx);

        Assert.Contains("=== RELATED ENTITIES", prompt);
        Assert.Contains("[npc] Kara: A warrior queen.", prompt);
    }

    // =========================================================================
    // Review chronicle user prompt
    // =========================================================================

    [Fact]
    public void BuildReviewChronicleUserPrompt_ContainsChronicleIdentity()
    {
        var ctx = new HistorianReviewContext(
            SourceText: "The chronicle text.",
            Tone: "weary",
            NoteType: "chronicle",
            RelationshipSummary: "",
            CanonFactsSummary: "",
            WorldDynamics: null,
            PreviousNotesSummary: "")
        {
            ChronicleTitle = "The Siege of Iron Keep",
            ChronicleFormat = "story",
            NarrativeStyleId = "austere-record",
        };

        var prompt = HistorianPrompts.BuildReviewChronicleUserPrompt(ctx);

        Assert.Contains("=== CHRONICLE ===", prompt);
        Assert.Contains("The Siege of Iron Keep", prompt);
        Assert.Contains("Format: story", prompt);
        Assert.Contains("Style: austere-record", prompt);
        Assert.Contains("=== NARRATIVE TO ANNOTATE ===", prompt);
    }

    [Fact]
    public void BuildReviewChronicleUserPrompt_DocumentFormat_UsesDocumentHeading()
    {
        var ctx = new HistorianReviewContext(
            SourceText: "An official decree.",
            Tone: "forensic",
            NoteType: "chronicle",
            RelationshipSummary: "",
            CanonFactsSummary: "",
            WorldDynamics: null,
            PreviousNotesSummary: "")
        {
            ChronicleTitle = "Council Decree 47",
            ChronicleFormat = "document",
        };

        var prompt = HistorianPrompts.BuildReviewChronicleUserPrompt(ctx);

        Assert.Contains("=== DOCUMENT TO ANNOTATE ===", prompt);
        Assert.Contains("primary source", prompt);
    }

    [Fact]
    public void BuildReviewChronicleUserPrompt_WithCast_ShowsCastSection()
    {
        var ctx = new HistorianReviewContext(
            SourceText: "The chronicle.",
            Tone: "witty",
            NoteType: "chronicle",
            RelationshipSummary: "",
            CanonFactsSummary: "",
            WorldDynamics: null,
            PreviousNotesSummary: "")
        {
            ChronicleTitle = "Test",
            ChronicleFormat = "story",
            Cast = [new CastEntry("Aldric", "primary", "npc"), new CastEntry("Iron Keep", "backdrop", "location")],
        };

        var prompt = HistorianPrompts.BuildReviewChronicleUserPrompt(ctx);

        Assert.Contains("=== CAST ===", prompt);
        Assert.Contains("Aldric (npc)", prompt);
        Assert.Contains("Iron Keep (location)", prompt);
    }

    [Fact]
    public void BuildReviewChronicleUserPrompt_WithTemporalContext_ShowsFocalEraAndNarrative()
    {
        var ctx = new HistorianReviewContext(
            SourceText: "A chronicle of the era.",
            Tone: "weary",
            NoteType: "chronicle",
            RelationshipSummary: "",
            CanonFactsSummary: "",
            WorldDynamics: null,
            PreviousNotesSummary: "")
        {
            ChronicleTitle = "Test",
            ChronicleFormat = "story",
            FocalEra = new FocalEraInfo("The Iron Age", "An era of metal and ambition."),
            TemporalNarrative = "Trade pressures bear on the northern territories.",
        };

        var prompt = HistorianPrompts.BuildReviewChronicleUserPrompt(ctx);

        Assert.Contains("=== TEMPORAL CONTEXT ===", prompt);
        Assert.Contains("Focal Era: The Iron Age", prompt);
        Assert.Contains("An era of metal and ambition.", prompt);
        Assert.Contains("Temporal Narrative", prompt);
        Assert.Contains("Trade pressures", prompt);
    }

    [Fact]
    public void BuildReviewChronicleUserPrompt_WithFactCoverageGuidance_ShowsGuidance()
    {
        var ctx = new HistorianReviewContext(
            SourceText: string.Join(" ", Enumerable.Repeat("word", 1000)),
            Tone: "scholarly",
            NoteType: "chronicle",
            RelationshipSummary: "",
            CanonFactsSummary: "",
            WorldDynamics: null,
            PreviousNotesSummary: "")
        {
            ChronicleTitle = "Test",
            ChronicleFormat = "story",
            FactCoverageGuidance = [new FactGuidanceTarget("f1", "surface", "evidence here", null)],
        };

        var prompt = HistorianPrompts.BuildReviewChronicleUserPrompt(ctx);

        Assert.Contains("=== FACT COVERAGE GUIDANCE ===", prompt);
        Assert.Contains("f1", prompt);
    }

    // =========================================================================
    // BuildReviewUserPrompt — dispatches to correct mode
    // =========================================================================

    [Fact]
    public void BuildReviewUserPrompt_EntityMode_DispatchesToEntityPrompt()
    {
        var ctx = new HistorianReviewContext(
            SourceText: "Entity text.",
            Tone: "scholarly",
            NoteType: "entity",
            RelationshipSummary: "",
            CanonFactsSummary: "",
            WorldDynamics: null,
            PreviousNotesSummary: "")
        { EntityName = "Test Entity", EntityKind = "npc" };

        var prompt = HistorianPrompts.BuildReviewUserPrompt(ctx);

        Assert.Contains("=== ENTITY ===", prompt);
        Assert.Contains("=== DESCRIPTION TO ANNOTATE ===", prompt);
    }

    [Fact]
    public void BuildReviewUserPrompt_ChronicleMode_DispatchesToChroniclePrompt()
    {
        var ctx = new HistorianReviewContext(
            SourceText: "Chronicle text.",
            Tone: "scholarly",
            NoteType: "chronicle",
            RelationshipSummary: "",
            CanonFactsSummary: "",
            WorldDynamics: null,
            PreviousNotesSummary: "")
        { ChronicleTitle = "Test Chronicle", ChronicleFormat = "story" };

        var prompt = HistorianPrompts.BuildReviewUserPrompt(ctx);

        Assert.Contains("=== CHRONICLE ===", prompt);
        Assert.Contains("=== NARRATIVE TO ANNOTATE ===", prompt);
    }

    // =========================================================================
    // Review system prompt — all 10 rules present
    // =========================================================================

    [Fact]
    public void BuildReviewSystemPrompt_ContainsAllTenRules()
    {
        var ctx = new HistorianReviewContext(
            SourceText: "Some text for review.",
            Tone: "weary",
            NoteType: "chronicle",
            RelationshipSummary: "",
            CanonFactsSummary: "",
            WorldDynamics: null,
            PreviousNotesSummary: "");

        var prompt = HistorianPrompts.BuildReviewSystemPrompt(ctx);

        // Key phrases from each rule
        Assert.Contains("EXACT substring", prompt);                  // Rule about anchor phrases
        Assert.Contains("20 to 100 words", prompt);                  // Annotation word range
        Assert.Contains("major", prompt);                            // Annotation weight
        Assert.Contains("minor", prompt);                            // Annotation weight
        Assert.Contains("three consecutive notes are the same length", prompt); // Brevity rule
    }

    // =========================================================================
    // Review system prompt — entity note types (all 6)
    // =========================================================================

    [Fact]
    public void BuildReviewSystemPrompt_EntityMode_HasAllSixNoteTypes()
    {
        var ctx = new HistorianReviewContext(
            SourceText: "Entity description text.",
            Tone: "scholarly",
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

    // =========================================================================
    // Review system prompt — document note types (all 6)
    // =========================================================================

    [Fact]
    public void BuildReviewSystemPrompt_DocumentMode_HasAllSixNoteTypes()
    {
        var ctx = new HistorianReviewContext(
            SourceText: "An official decree from the council.",
            Tone: "forensic",
            NoteType: "chronicle",
            RelationshipSummary: "",
            CanonFactsSummary: "",
            WorldDynamics: null,
            PreviousNotesSummary: "");

        var prompt = HistorianPrompts.BuildReviewSystemPrompt(ctx, chronicleFormat: "document");

        Assert.Contains("commentary", prompt);
        Assert.Contains("correction", prompt);
        Assert.Contains("tangent", prompt);
        Assert.Contains("skepticism", prompt);
        Assert.Contains("pedantic", prompt);
        Assert.Contains("temporal", prompt);
    }
}
