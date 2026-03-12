using TheCanonry.Illuminator.Chronicle;
using TheCanonry.Illuminator.Chronicle.PerspectiveSynthesis;
using TheCanonry.Illuminator.Chronicle.V2;

namespace TheCanonry.Illuminator.Tests.Chronicle;

public sealed class V2PromptBuilderTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    private static ChronicleGenerationContext MakeContext(
        string worldName = "Eld",
        string? narrativeDirection = null,
        string? eraName = null) => new()
    {
        WorldName = worldName,
        WorldDescription = "A world of mist and old stone.",
        Tone = "melancholy",
        CanonFacts = [],
        Entities = [],
        Relationships = [],
        FocalEraName = eraName,
        NarrativeDirection = narrativeDirection,
    };

    private static EntityContext MakeEntity(string id, string name, string kind = "npc", string? culture = null) =>
        new()
        {
            Id = id,
            Name = name,
            Kind = kind,
            Prominence = "recognized",
            Status = "active",
            Culture = culture,
        };

    // =========================================================================
    // Test 1: Story system prompt contains key phrases
    // =========================================================================

    [Fact]
    public void StorySystemPrompt_ContainsKeyPhrases()
    {
        var prompt = StoryPromptBuilder.GetSystemPrompt();

        Assert.Contains("expert fantasy author", prompt);
        Assert.Contains("CRAFT", prompt);
        Assert.Contains("WORLD DATA", prompt);
        Assert.Contains("STORY BIBLE", prompt);
        Assert.Contains("CRITICAL", prompt);
    }

    // =========================================================================
    // Test 2: Document system prompt contains key phrases
    // =========================================================================

    [Fact]
    public void DocumentSystemPrompt_ContainsKeyPhrases()
    {
        var prompt = DocumentPromptBuilder.GetSystemPrompt();

        Assert.Contains("in-universe document", prompt);
        Assert.Contains("Document Instructions", prompt);
        Assert.Contains("CRAFT", prompt);
        Assert.Contains("WORLD DATA", prompt);
    }

    // =========================================================================
    // Test 3: Story user prompt includes task section with word count range
    // =========================================================================

    [Fact]
    public void StoryUserPrompt_IncludesTaskSectionWithWordCount()
    {
        var ctx = new StoryPromptContext
        {
            GenerationContext = MakeContext(),
            PrimaryEntities = [MakeEntity("e1", "Rael")],
            SupportingEntities = [],
            MinWords = 2000,
            MaxWords = 4000,
            MinScenes = 3,
            MaxScenes = 6,
        };

        var prompt = StoryPromptBuilder.BuildUserPrompt(ctx);

        Assert.Contains("2000-4000 word", prompt);
        Assert.Contains("3-6 distinct scenes", prompt);
        Assert.Contains("# Task", prompt);
    }

    // =========================================================================
    // Test 4: Document user prompt includes document-specific task section
    // =========================================================================

    [Fact]
    public void DocumentUserPrompt_IncludesDocumentTaskSection()
    {
        var ctx = new DocumentPromptContext
        {
            GenerationContext = MakeContext(),
            PrimaryEntities = [MakeEntity("e1", "Lord Caern")],
            SupportingEntities = [],
            StyleName = "court record",
            MinWords = 400,
            MaxWords = 600,
        };

        var prompt = DocumentPromptBuilder.BuildUserPrompt(ctx);

        Assert.Contains("400-600 word court record", prompt);
        Assert.Contains("# Task", prompt);
        Assert.Contains("document feel authentic", prompt);
    }

    // =========================================================================
    // Test 5: Copy-edit story system prompt contains editing instructions
    // =========================================================================

    [Fact]
    public void CopyEditStorySystemPrompt_ContainsEditingInstructions()
    {
        var prompt = CopyEditPromptBuilder.GetStorySystemPrompt();

        Assert.Contains("senior fiction editor", prompt);
        Assert.Contains("burnish", prompt);
        Assert.Contains("Output only the edited text", prompt);
        Assert.Contains("Smooth the seams", prompt);
    }

    // =========================================================================
    // Test 6: PromptSections.BuildNarrativeVoiceSection returns empty for null input
    // =========================================================================

    [Fact]
    public void BuildNarrativeVoiceSection_ReturnsEmpty_ForNullInput()
    {
        var result = PromptSections.BuildNarrativeVoiceSection(null);
        Assert.Equal(string.Empty, result);
    }

    // =========================================================================
    // Additional tests
    // =========================================================================

    [Fact]
    public void BuildNarrativeVoiceSection_ReturnsEmpty_ForEmptyDictionary()
    {
        var result = PromptSections.BuildNarrativeVoiceSection(new Dictionary<string, string>());
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void BuildNarrativeVoiceSection_FormatsBoldKeys()
    {
        var voice = new Dictionary<string, string>
        {
            ["mood"] = "melancholy and restrained",
            ["pacing"] = "slow, deliberate",
        };

        var result = PromptSections.BuildNarrativeVoiceSection(voice);

        Assert.Contains("**mood**: melancholy and restrained", result);
        Assert.Contains("**pacing**: slow, deliberate", result);
        Assert.Contains("# Story Bible: Tone & Atmosphere", result);
    }

    [Fact]
    public void ChroniclePrompts_DispatchesToStoryBuilder_ForStoryFormat()
    {
        var ctx = MakeContext();
        var prompt = ChroniclePrompts.BuildGenerationSystemPrompt(ctx, "story");
        Assert.Contains("expert fantasy author", prompt);
    }

    [Fact]
    public void ChroniclePrompts_DispatchesToDocumentBuilder_ForDocumentFormat()
    {
        var ctx = MakeContext();
        var prompt = ChroniclePrompts.BuildGenerationSystemPrompt(ctx, "document");
        Assert.Contains("in-universe document", prompt);
    }

    [Fact]
    public void ChroniclePrompts_FallsBackToStory_ForUnknownFormat()
    {
        var ctx = MakeContext();
        var prompt = ChroniclePrompts.BuildGenerationSystemPrompt(ctx, "unknown");
        Assert.Contains("expert fantasy author", prompt);
    }

    [Fact]
    public void StoryUserPrompt_IncludesNarrativeDirectionSection_WhenSet()
    {
        var ctx = new StoryPromptContext
        {
            GenerationContext = MakeContext(narrativeDirection: "A betrayal seen through the eyes of its witness"),
            PrimaryEntities = [MakeEntity("e1", "Sylva")],
            SupportingEntities = [],
        };

        var prompt = StoryPromptBuilder.BuildUserPrompt(ctx);

        Assert.Contains("# Narrative Direction", prompt);
        Assert.Contains("A betrayal seen through the eyes of its witness", prompt);
    }

    [Fact]
    public void CopyEditUserPrompt_IncludesFormatAndLengthContext()
    {
        var prompt = CopyEditPromptBuilder.BuildUserPrompt(
            text: "The soldier stood at the gate.",
            styleName: "short story",
            minWords: 1000,
            maxWords: 2000);

        Assert.Contains("short story (1000-2000 words)", prompt);
        Assert.Contains("The soldier stood at the gate.", prompt);
    }

    [Fact]
    public void CopyEditDocumentSystemPrompt_ContainsDocumentEditingInstructions()
    {
        var prompt = CopyEditPromptBuilder.GetDocumentSystemPrompt();

        Assert.Contains("senior editor", prompt);
        Assert.Contains("in-universe document", prompt);
        Assert.Contains("Output only the edited text", prompt);
        Assert.Contains("Smooth the seams", prompt);
    }

    [Fact]
    public void BuildDataSection_ReturnsEmpty_ForEmptyRelationships()
    {
        var result = PromptSections.BuildDataSection([]);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void BuildDataSection_FormatsRelationshipsCorrectly()
    {
        var relationships = new List<RelationshipContext>
        {
            new() { SourceId = "a", TargetId = "b", Kind = "rivals", SourceName = "Rael", TargetName = "Mora" },
        };

        var result = PromptSections.BuildDataSection(relationships);

        Assert.Contains("Rael --[rivals]--> Mora", result);
        Assert.Contains("# Relationships", result);
    }

    [Fact]
    public void BuildTemporalSection_ReturnsEmpty_WhenAllNull()
    {
        var result = PromptSections.BuildTemporalSection(null, null, null);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void BuildEntityDirectivesSection_ReturnsEmpty_ForNullInput()
    {
        var result = PromptSections.BuildEntityDirectivesSection(null);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void BuildNarrativeLensSection_ReturnsEmpty_ForNullInput()
    {
        var result = PromptSections.BuildNarrativeLensSection(null);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatEntityFull_IncludesKindAndProminence()
    {
        var entity = MakeEntity("e1", "Thal", "place", "northern");
        entity = entity with { Prominence = "mythic", Status = "historical" };

        var result = PromptSections.FormatEntityFull(entity);

        Assert.Contains("Thal", result);
        Assert.Contains("place", result);
        Assert.Contains("northern", result);
        Assert.Contains("mythic", result);
    }
}
