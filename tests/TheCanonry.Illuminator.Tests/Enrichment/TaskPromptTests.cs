using TheCanonry.Illuminator.Enrichment.Prompts;

namespace TheCanonry.Illuminator.Tests.Enrichment;

public sealed class TaskPromptTests
{
    // =========================================================================
    // DynamicsPrompts
    // =========================================================================

    [Fact]
    public void DynamicsPrompts_SystemPrompt_MentionsDynamics()
    {
        var prompt = DynamicsPrompts.BuildSystemPrompt();

        Assert.Contains("dynamics", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("world dynamics analyst", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DynamicsPrompts_SystemPrompt_ContainsOutputFormat()
    {
        var prompt = DynamicsPrompts.BuildSystemPrompt();

        Assert.Contains("\"dynamics\"", prompt);
        Assert.Contains("\"eraOverrides\"", prompt);
        Assert.Contains("\"complete\"", prompt);
        Assert.Contains("\"reasoning\"", prompt);
    }

    [Fact]
    public void DynamicsPrompts_UserPrompt_FirstTurn_ContainsTaskInstruction()
    {
        var messages = new List<(string Role, string Content)>
        {
            ("system", "=== WORLD STATE ===\nEntity data here.")
        };

        var prompt = DynamicsPrompts.BuildUserPrompt(messages, isFirstTurn: true);

        Assert.Contains("=== WORLD STATE ===", prompt);
        Assert.Contains("identify the world dynamics", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("macro-level forces", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DynamicsPrompts_UserPrompt_RefinementTurn_ContainsRefinementInstruction()
    {
        var messages = new List<(string Role, string Content)>
        {
            ("assistant", "{ \"dynamics\": [] }")
        };

        var prompt = DynamicsPrompts.BuildUserPrompt(messages, isFirstTurn: false, userFeedback: "Add more conflict dynamics.");

        Assert.Contains("=== YOUR PREVIOUS RESPONSE ===", prompt);
        Assert.Contains("=== USER FEEDBACK ===", prompt);
        Assert.Contains("Add more conflict dynamics.", prompt);
        Assert.Contains("refining", prompt, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // SummaryRevisionPrompts
    // =========================================================================

    [Fact]
    public void SummaryRevisionPrompts_SystemPrompt_MentionsRevision()
    {
        var prompt = SummaryRevisionPrompts.BuildSystemPrompt();

        // The TS source uses "rewriting" and "rewrite" — the task is called "summary revision"
        Assert.Contains("rewriting", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rewrite", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SummaryRevisionPrompts_SystemPrompt_MentionsJsonOutputFormat()
    {
        var prompt = SummaryRevisionPrompts.BuildSystemPrompt();

        Assert.Contains("\"patches\"", prompt);
        Assert.Contains("\"entityId\"", prompt);
        Assert.Contains("\"summary\"", prompt);
        Assert.Contains("\"description\"", prompt);
    }

    [Fact]
    public void SummaryRevisionPrompts_UserPrompt_IncludesEntityData()
    {
        var entities = new List<SummaryRevisionPrompts.RevisionEntity>
        {
            new("e1", "Iron Warden", "artifact", "weapon", "renowned", "forge-born", "active",
                "A massive hammer with a cracked head.",
                new List<(string, string, string)> { ("wielded_by", "Kara", "npc") },
                "A legendary hammer.", "It was forged in the deepest furnace.")
        };

        var prompt = SummaryRevisionPrompts.BuildUserPrompt(
            entities, "forge-born",
            worldDynamicsContext: "The forge wars are escalating.",
            schemaContext: "Kinds: artifact, npc");

        Assert.Contains("Iron Warden", prompt);
        Assert.Contains("forge-born", prompt);
        Assert.Contains("The forge wars are escalating.", prompt);
        Assert.Contains("A massive hammer with a cracked head.", prompt);
        Assert.Contains("wielded_by", prompt);
    }

    [Fact]
    public void SummaryRevisionPrompts_UserPrompt_ContainsTaskInstruction()
    {
        var entities = new List<SummaryRevisionPrompts.RevisionEntity>
        {
            new("e2", "Silent Gate", "location", null, "marginal", "northern", "active",
                null, new List<(string, string, string)>(), "A gate.", "It stands silent.")
        };

        var prompt = SummaryRevisionPrompts.BuildUserPrompt(entities, "northern");

        Assert.Contains("=== YOUR TASK ===", prompt);
        Assert.Contains("Rewrite the", prompt);
        Assert.Contains("Preserve visual thesis", prompt);
    }

    // =========================================================================
    // BackportPrompts
    // =========================================================================

    [Fact]
    public void BackportPrompts_SystemPrompt_MentionsLoreAndBackport()
    {
        var prompt = BackportPrompts.BuildSystemPrompt();

        Assert.Contains("lore", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("chronicle", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("wiki", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BackportPrompts_SystemPrompt_ContainsOutputFormatWithDescriptionArray()
    {
        var prompt = BackportPrompts.BuildSystemPrompt();

        Assert.Contains("\"patches\"", prompt);
        Assert.Contains("\"anchorPhrase\"", prompt);
        Assert.Contains("ARRAY OF STRINGS", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BackportPrompts_UserPrompt_IncludesChronicleText()
    {
        const string chronicleText = "The battle of Iron Keep ended in disaster.";

        var prompt = BackportPrompts.BuildUserPrompt(
            chronicleText,
            perspectiveSynthesisJson: """{"brief":"A story of loss."}""");

        Assert.Contains("=== CHRONICLE TEXT ===", prompt);
        Assert.Contains(chronicleText, prompt);
    }

    [Fact]
    public void BackportPrompts_UserPrompt_WithCustomInstructions_IncludesCriticalBlock()
    {
        var prompt = BackportPrompts.BuildUserPrompt(
            "Chronicle text here.",
            customInstructions: "Focus only on status changes.");

        Assert.Contains("CRITICAL", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Focus only on status changes.", prompt);
    }

    [Fact]
    public void BackportPrompts_UserPrompt_WithEntities_SeparatesCastAndLens()
    {
        var entities = new List<BackportPrompts.BackportEntityContext>
        {
            new()
            {
                Id = "e1", Name = "Iron Warden", Kind = "npc", Summary = "A legendary warrior.",
                Prominence = "renowned", IsPrimary = true, IsLens = false,
            },
            new()
            {
                Id = "e2", Name = "The Northern Council", Kind = "faction", Summary = "A governing body.",
                Prominence = "recognized", IsPrimary = false, IsLens = true,
            },
        };

        var prompt = BackportPrompts.BuildUserPrompt(
            entities,
            "The chronicle text.",
            perspectiveSynthesisJson: null);

        Assert.Contains("=== CAST (1 entities) ===", prompt);
        Assert.Contains("=== NARRATIVE LENS (1 entity) ===", prompt);
        Assert.Contains("Iron Warden", prompt);
        Assert.Contains("The Northern Council", prompt);
        Assert.Contains("higher bar", prompt);
    }

    [Fact]
    public void BackportPrompts_UserPrompt_WithEntities_FormatsEntityBlocks()
    {
        var entities = new List<BackportPrompts.BackportEntityContext>
        {
            new()
            {
                Id = "e1", Name = "Iron Warden", Kind = "npc", Subtype = "warrior",
                Culture = "northern", Status = "active", Prominence = "renowned",
                Summary = "A legendary warrior of the north.",
                Description = "Paragraph one.\n\nParagraph two.",
                VisualThesis = "A hulking figure clad in frost-scarred iron plate.",
                Relationships = [new BackportPrompts.BackportRelationship("allied_with", "Kara", "npc")],
                IsPrimary = true, IsLens = false,
            },
        };

        var prompt = BackportPrompts.BuildUserPrompt(entities, "The chronicle text.");

        Assert.Contains("npc / warrior", prompt);
        Assert.Contains("Culture: northern", prompt);
        Assert.Contains("Prominence: renowned", prompt);
        Assert.Contains("Visual Thesis (DO NOT CONTRADICT)", prompt);
        Assert.Contains("frost-scarred iron plate", prompt);
        Assert.Contains("allied_with → Kara (npc)", prompt);
        // Numbered paragraph descriptions
        Assert.Contains("[1]", prompt);
        Assert.Contains("Paragraph one.", prompt);
    }

    [Fact]
    public void BackportPrompts_UserPrompt_WithEntities_PerEntityTaskBlocks()
    {
        var entities = new List<BackportPrompts.BackportEntityContext>
        {
            new()
            {
                Id = "e1", Name = "Iron Warden", Kind = "npc", Summary = "A warrior.",
                Prominence = "renowned", IsPrimary = true, IsLens = false,
            },
            new()
            {
                Id = "e2", Name = "Northern Council", Kind = "faction", Summary = "A council.",
                Prominence = "recognized", IsPrimary = false, IsLens = true,
            },
        };

        var prompt = BackportPrompts.BuildUserPrompt(entities, "Chronicle text.");

        Assert.Contains("=== YOUR TASK ===", prompt);
        Assert.Contains("Process each entity below independently", prompt);
        Assert.Contains("Preserve visual thesis", prompt);
        // Both entities should have task blocks
        Assert.Contains("e1", prompt);
        Assert.Contains("e2", prompt);
    }

    // =========================================================================
    // FactCoveragePrompts
    // =========================================================================

    [Fact]
    public void FactCoveragePrompts_SystemPrompt_MentionsCoverageLevels()
    {
        var prompt = FactCoveragePrompts.BuildSystemPrompt();

        Assert.Contains("missing", prompt);
        Assert.Contains("mentioned", prompt);
        Assert.Contains("prevalent", prompt);
        Assert.Contains("integral", prompt);
    }

    [Fact]
    public void FactCoveragePrompts_SystemPrompt_MentionsLiteraryAnalyst()
    {
        var prompt = FactCoveragePrompts.BuildSystemPrompt();

        Assert.Contains("literary analyst", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("factId", prompt);
        Assert.Contains("rating", prompt);
        Assert.Contains("evidence", prompt);
    }

    [Fact]
    public void FactCoveragePrompts_UserPrompt_IncludesFacts()
    {
        var facts = new List<(string Id, string Text)>
        {
            ("f1", "The Iron Tower was built in the third era."),
            ("f2", "Merchants pay tribute to the council.")
        };
        const string narrative = "The merchants gathered beneath the tower's shadow.";

        var prompt = FactCoveragePrompts.BuildUserPrompt(facts, narrative);

        Assert.Contains("=== FACTS TO ASSESS ===", prompt);
        Assert.Contains("f1", prompt);
        Assert.Contains("The Iron Tower was built in the third era.", prompt);
        Assert.Contains("f2", prompt);
        Assert.Contains("=== NARRATIVE TEXT ===", prompt);
        Assert.Contains(narrative, prompt);
    }

    [Fact]
    public void FactCoveragePrompts_UserPrompt_NumbersFacts()
    {
        var facts = new List<(string Id, string Text)>
        {
            ("fact-a", "First fact."),
            ("fact-b", "Second fact."),
            ("fact-c", "Third fact.")
        };

        var prompt = FactCoveragePrompts.BuildUserPrompt(facts, "Narrative.");

        Assert.Contains("[1]", prompt);
        Assert.Contains("[2]", prompt);
        Assert.Contains("[3]", prompt);
    }

    // =========================================================================
    // ToneRankingPrompts
    // =========================================================================

    [Fact]
    public void ToneRankingPrompts_SystemPrompt_MentionsToneAndRank()
    {
        var prompt = ToneRankingPrompts.BuildSystemPrompt();

        Assert.Contains("tone", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Rank", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToneRankingPrompts_SystemPrompt_ContainsAllSevenTones()
    {
        var prompt = ToneRankingPrompts.BuildSystemPrompt();

        Assert.Contains("witty", prompt);
        Assert.Contains("weary", prompt);
        Assert.Contains("elegiac", prompt);
        Assert.Contains("cantankerous", prompt);
        Assert.Contains("rueful", prompt);
        Assert.Contains("conspiratorial", prompt);
        Assert.Contains("bemused", prompt);
    }

    [Fact]
    public void ToneRankingPrompts_UserPrompt_IncludesSummaryAndFormat()
    {
        var prompt = ToneRankingPrompts.BuildUserPrompt(
            summary: "A tale of two factions at war over a sacred artifact.",
            format: "chronicle",
            narrativeStyleName: "Austere Record",
            brief: "Conflict breeds unlikely alliances.");

        Assert.Contains("A tale of two factions at war", prompt);
        Assert.Contains("Format: chronicle", prompt);
        Assert.Contains("Style: Austere Record", prompt);
        Assert.Contains("Perspective brief: Conflict breeds unlikely alliances.", prompt);
    }

    [Fact]
    public void ToneRankingPrompts_UserPrompt_ContainsOutputFormat()
    {
        var prompt = ToneRankingPrompts.BuildSystemPrompt();

        Assert.Contains("\"ranking\"", prompt);
        Assert.Contains("\"rationales\"", prompt);
    }

    // =========================================================================
    // MotifVariationPrompts
    // =========================================================================

    [Fact]
    public void MotifVariationPrompts_VarySystemPrompt_MentionsOverusedAndVariation()
    {
        var prompt = MotifVariationPrompts.BuildVarySystemPrompt("over the years");

        Assert.Contains("overused", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("over the years", prompt);
        Assert.Contains("Vary aggressively", prompt);
    }

    [Fact]
    public void MotifVariationPrompts_VaryUserPrompt_IncludesAnnotations()
    {
        var instances = new List<MotifVariationPrompts.MotifInstance>
        {
            new(0, "Kara the Warden", "Over the years, she had watched empires crumble.", "Over the years"),
            new(1, "The Silent Gate", "Over the years, vines had swallowed the hinges.", "Over the years")
        };

        var prompt = MotifVariationPrompts.BuildVaryUserPrompt(instances);

        Assert.Contains("[0]", prompt);
        Assert.Contains("Kara the Warden", prompt);
        Assert.Contains("she had watched empires crumble", prompt);
        Assert.Contains("[1]", prompt);
        Assert.Contains("The Silent Gate", prompt);
        Assert.Contains("---", prompt); // separator between instances
    }

    [Fact]
    public void MotifVariationPrompts_WeaveSystemPrompt_MentionsWeaveAndMotif()
    {
        var prompt = MotifVariationPrompts.BuildWeaveSystemPrompt("the long silence");

        Assert.Contains("the long silence", prompt);
        Assert.Contains("motif", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("organic", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MotifVariationPrompts_WeaveUserPrompt_IncludesSentences()
    {
        var instances = new List<MotifVariationPrompts.MotifWeaveInstance>
        {
            new(0, "Kara", "She stood at the threshold.", "Context before. She stood at the threshold. Context after.")
        };

        var prompt = MotifVariationPrompts.BuildWeaveUserPrompt(instances);

        Assert.Contains("[0]", prompt);
        Assert.Contains("Kara", prompt);
        Assert.Contains("She stood at the threshold.", prompt);
    }

    // =========================================================================
    // PaletteExpansionPrompts
    // =========================================================================

    [Fact]
    public void PaletteExpansionPrompts_SystemPrompt_MentionsTraitAndPalette()
    {
        var prompt = PaletteExpansionPrompts.BuildSystemPrompt();

        Assert.Contains("trait", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("palette", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Silhouette", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PaletteExpansionPrompts_UserPrompt_IncludesEntityKind()
    {
        var subtypes = new List<string> { "merchant", "soldier", "priest" };
        var eras = new List<PaletteExpansionPrompts.EraContext>
        {
            new("era-1", "Age of Commerce", "Trade dominated all decisions.")
        };

        var prompt = PaletteExpansionPrompts.BuildUserPrompt("npc", "A harsh arctic world.", subtypes, eras);

        Assert.Contains("npc", prompt);
        Assert.Contains("merchant", prompt);
        Assert.Contains("soldier", prompt);
        Assert.Contains("priest", prompt);
        Assert.Contains("Age of Commerce", prompt);
        Assert.Contains("era-1", prompt);
    }

    [Fact]
    public void PaletteExpansionPrompts_UserPrompt_ThrowsWhenNoSubtypes()
    {
        Assert.Throws<ArgumentException>(() =>
            PaletteExpansionPrompts.BuildUserPrompt("npc", "World context.", [], []));
    }

    [Fact]
    public void PaletteExpansionPrompts_UserPrompt_IncludesCultureContext()
    {
        var subtypes = new List<string> { "warrior" };
        var cultures = new List<PaletteExpansionPrompts.CultureContext>
        {
            new("Nightshelf", "Guild culture.", new Dictionary<string, string> { ["armor"] = "black iron" })
        };

        var prompt = PaletteExpansionPrompts.BuildUserPrompt("npc", "World.", subtypes, [], cultures);

        Assert.Contains("Nightshelf", prompt);
        Assert.Contains("black iron", prompt);
    }

    // =========================================================================
    // EntityTagImageStylesPrompts
    // =========================================================================

    [Fact]
    public void EntityTagImageStylesPrompts_SystemPrompt_MentionsStyle()
    {
        var prompt = EntityTagImageStylesPrompts.BuildSystemPrompt();

        Assert.Contains("style", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("visual art director", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EntityTagImageStylesPrompts_UserPrompt_IncludesEntityData()
    {
        var entities = new List<EntityTagImageStylesPrompts.EntityInput>
        {
            new("e1", "npc", "merchant", "A stooped figure with a merchant's ledger.", ["scarred hands"])
        };
        var artisticStyles = new List<EntityTagImageStylesPrompts.StyleEntry>
        {
            new("s1", "Ink Wash", "Traditional"),
            new("s2", "Oil Paint", "Classic")
        };
        var compositionStyles = new List<EntityTagImageStylesPrompts.CompositionEntry>
        {
            new("c1", "Portrait", "character"),
            new("c2", "Landscape", "location")
        };
        var palettes = new List<EntityTagImageStylesPrompts.PaletteEntry>
        {
            new("p1", "Arctic Dusk", "Cool blues and purples.", "Cool")
        };

        var prompt = EntityTagImageStylesPrompts.BuildUserPrompt(entities, artisticStyles, compositionStyles, palettes);

        Assert.Contains("e1", prompt);
        Assert.Contains("npc/merchant", prompt);
        Assert.Contains("A stooped figure with a merchant's ledger.", prompt);
        Assert.Contains("scarred hands", prompt);
        Assert.Contains("Ink Wash", prompt);
        Assert.Contains("s1", prompt);
        Assert.Contains("Arctic Dusk", prompt);
    }

    [Fact]
    public void EntityTagImageStylesPrompts_UserPrompt_ContainsCoverageRules()
    {
        var entities = new List<EntityTagImageStylesPrompts.EntityInput>
        {
            new("e1", "npc", "warrior", "A tall figure with a spear.", [])
        };
        var artisticStyles = new List<EntityTagImageStylesPrompts.StyleEntry> { new("s1", "Watercolor", "Soft") };
        var compositionStyles = new List<EntityTagImageStylesPrompts.CompositionEntry> { new("c1", "Portrait", "character") };
        var palettes = new List<EntityTagImageStylesPrompts.PaletteEntry> { new("p1", "Warm Sunset", "Warm tones.", "Warm") };

        var prompt = EntityTagImageStylesPrompts.BuildUserPrompt(entities, artisticStyles, compositionStyles, palettes);

        Assert.Contains("COVERAGE RULES", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MAXIMUM VISUAL VARIETY", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("artisticStyleIds", prompt);
        Assert.Contains("compositionStyleIds", prompt);
        Assert.Contains("colorPaletteIds", prompt);
    }
}
