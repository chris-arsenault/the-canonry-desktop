using TheCanonry.Illuminator.Enrichment.Prompts;
using TheCanonry.Schema.Primitives;
using TheCanonry.Schema.World;

namespace TheCanonry.Illuminator.Tests.Enrichment;

public sealed class DescriptionPromptTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    private static Entity MakeEntity(string name = "Iron Warden") =>
        new(
            new Schema.Ids.EntityId("e1"),
            FrameworkPrimitives.EntityKinds.Era,
            "warrior",
            name,
            new Schema.Ids.CultureId("northern"),
            new Schema.Ids.EraId("era1"),
            new SemanticCoordinates(0.5, 0.5, 0.0),
            new Schema.World.ExecutionContext(1, ExecutionSource.Seed, "test", true, ""),
            tick: 1);

    private static IReadOnlyList<DescriptionPrompts.EraTemporalInfo> MakeEras() =>
    [
        new("era-dawn", "The Dawn Age", "The first stirrings of civilization.", 1, 0, 100),
        new("era-expansion", "The Age of Expansion", "Cultures spread across the continent.", 2, 101, 250),
        new("era-ice", "The Clever Ice Age", "The glaciers returned with purpose.", 3, 251, 400),
    ];

    // =========================================================================
    // BuildWorldTimeline — natural language timeline
    // =========================================================================

    [Fact]
    public void BuildWorldTimeline_FocalInMiddle_ProducesPastPresentFuture()
    {
        var eras = MakeEras();
        var result = DescriptionPrompts.BuildWorldTimeline(eras, "era-expansion");

        Assert.Contains("The world passed through the Dawn Age.", result);
        Assert.Contains("It now exists in the Age of Expansion.", result);
        Assert.Contains("the Clever Ice Age lies ahead.", result);
    }

    [Fact]
    public void BuildWorldTimeline_FocalAtStart_NoPastSection()
    {
        var eras = MakeEras();
        var result = DescriptionPrompts.BuildWorldTimeline(eras, "era-dawn");

        Assert.DoesNotContain("passed through", result);
        Assert.Contains("It now exists in the Dawn Age.", result);
        Assert.Contains("lie ahead", result);
    }

    [Fact]
    public void BuildWorldTimeline_FocalAtEnd_NoFutureSection()
    {
        var eras = MakeEras();
        var result = DescriptionPrompts.BuildWorldTimeline(eras, "era-ice");

        Assert.Contains("passed through", result);
        Assert.Contains("It now exists in the Clever Ice Age.", result);
        Assert.DoesNotContain("ahead", result);
    }

    [Fact]
    public void BuildWorldTimeline_SingleEra_OnlyPresent()
    {
        var eras = new List<DescriptionPrompts.EraTemporalInfo>
        {
            new("era-only", "The Only Age", null, 1, 0, 100),
        };
        var result = DescriptionPrompts.BuildWorldTimeline(eras, "era-only");

        Assert.Equal("It now exists in the Only Age.", result);
    }

    [Fact]
    public void BuildWorldTimeline_HandleThePrefix_Deduplicates()
    {
        var eras = new List<DescriptionPrompts.EraTemporalInfo>
        {
            new("era-a", "The Great Age", null, 1, 0, 50),
            new("era-b", "Iron Age", null, 2, 51, 100),
        };
        var result = DescriptionPrompts.BuildWorldTimeline(eras, "era-b");

        // "The Great Age" → "the Great Age" (not "the The Great Age")
        Assert.Contains("the Great Age", result);
        Assert.DoesNotContain("the The", result);
    }

    [Fact]
    public void BuildWorldTimeline_MultipleFuture_PluralLie()
    {
        var eras = MakeEras();
        var result = DescriptionPrompts.BuildWorldTimeline(eras, "era-dawn");

        // Two future eras → "lie" not "lies"
        Assert.Contains("lie ahead", result);
    }

    [Fact]
    public void BuildWorldTimeline_SingleFuture_SingularLies()
    {
        var eras = new List<DescriptionPrompts.EraTemporalInfo>
        {
            new("era-a", "First Age", null, 1, 0, 50),
            new("era-b", "Second Age", null, 2, 51, 100),
        };
        var result = DescriptionPrompts.BuildWorldTimeline(eras, "era-a");

        Assert.Contains("lies ahead", result);
    }

    [Fact]
    public void BuildWorldTimeline_NonexistentFocal_ReturnsEmpty()
    {
        var eras = MakeEras();
        Assert.Equal("", DescriptionPrompts.BuildWorldTimeline(eras, "nonexistent"));
    }

    // =========================================================================
    // BuildHistoricalContext — era name, summary, and world timeline
    // =========================================================================

    [Fact]
    public void BuildHistoricalContext_NullFocalEra_ReturnsEmpty()
    {
        Assert.Equal("", DescriptionPrompts.BuildHistoricalContext(null, MakeEras()));
    }

    [Fact]
    public void BuildHistoricalContext_NullAllEras_ReturnsEmpty()
    {
        var era = MakeEras()[1];
        Assert.Equal("", DescriptionPrompts.BuildHistoricalContext(era, null));
    }

    [Fact]
    public void BuildHistoricalContext_IncludesEraNameAndSummary()
    {
        var eras = MakeEras();
        var focal = eras[1]; // The Age of Expansion
        var result = DescriptionPrompts.BuildHistoricalContext(focal, eras);

        Assert.Contains("HISTORICAL CONTEXT:", result);
        Assert.Contains("Era: The Age of Expansion", result);
        Assert.Contains("Cultures spread across the continent.", result);
    }

    [Fact]
    public void BuildHistoricalContext_IncludesWorldTimeline()
    {
        var eras = MakeEras();
        var focal = eras[1];
        var result = DescriptionPrompts.BuildHistoricalContext(focal, eras);

        Assert.Contains("The world passed through", result);
        Assert.Contains("It now exists in", result);
    }

    // =========================================================================
    // BuildNarrativePrompt — historical context prepended
    // =========================================================================

    [Fact]
    public void BuildNarrativePrompt_WithEras_PrependsHistoricalContext()
    {
        var entity = MakeEntity();
        var eras = MakeEras();
        var focal = eras[1];

        var (_, userPrompt) = DescriptionPrompts.BuildNarrativePrompt(
            entity, "Entity context here.",
            focalEra: focal, allEras: eras);

        Assert.Contains("HISTORICAL CONTEXT:", userPrompt);
        Assert.Contains("---", userPrompt);
        Assert.Contains("Entity context here.", userPrompt);
    }

    [Fact]
    public void BuildNarrativePrompt_WithoutEras_NoHistoricalContext()
    {
        var entity = MakeEntity();

        var (_, userPrompt) = DescriptionPrompts.BuildNarrativePrompt(entity, "Entity context here.");

        Assert.DoesNotContain("HISTORICAL CONTEXT:", userPrompt);
        Assert.Equal("Entity context here.", userPrompt);
    }

    [Fact]
    public void BuildNarrativePrompt_WithNarrativeHint_IncludesHintInSystem()
    {
        var entity = MakeEntity();

        var (systemPrompt, _) = DescriptionPrompts.BuildNarrativePrompt(
            entity, "Entity context.",
            narrativeHint: "Born during the Iron Wars.");

        Assert.Contains("NARRATIVE HINT", systemPrompt);
        Assert.Contains("Born during the Iron Wars.", systemPrompt);
    }

    [Fact]
    public void BuildNarrativePrompt_LockedSummary_OutputsDescriptionAndAliasesOnly()
    {
        var entity = MakeEntity();

        var (systemPrompt, _) = DescriptionPrompts.BuildNarrativePrompt(
            entity, "Context.", lockedSummary: true);

        Assert.Contains("description, aliases", systemPrompt);
        Assert.DoesNotContain("summary, description, aliases", systemPrompt);
    }

    [Fact]
    public void BuildNarrativePrompt_UnlockedSummary_OutputsSummaryDescriptionAliases()
    {
        var entity = MakeEntity();

        var (systemPrompt, _) = DescriptionPrompts.BuildNarrativePrompt(entity, "Context.");

        Assert.Contains("summary, description, aliases", systemPrompt);
    }

    // =========================================================================
    // BuildVisualThesisPrompt — cultural visual identity and framing
    // =========================================================================

    [Fact]
    public void BuildVisualThesisPrompt_OutputsOneSentenceInstruction()
    {
        var entity = MakeEntity();
        var (system, _) = DescriptionPrompts.BuildVisualThesisPrompt(
            entity, "A tall warrior.", "Per-kind guidance here.");

        Assert.Contains("ONE sentence", system);
        Assert.Contains("Shape only", system);
        Assert.Contains("Per-kind guidance here.", system);
    }

    [Fact]
    public void BuildVisualThesisPrompt_WithVisualAvoid_IncludesAvoidSection()
    {
        var entity = MakeEntity();
        var (system, _) = DescriptionPrompts.BuildVisualThesisPrompt(
            entity, "Description.", "Guidance.",
            visualAvoid: "No glowing eyes or auras.");

        Assert.Contains("AVOID: No glowing eyes or auras.", system);
    }

    [Fact]
    public void BuildVisualThesisPrompt_WithCulturalVisualIdentity_IncludesInUserPrompt()
    {
        var entity = MakeEntity();
        var (_, user) = DescriptionPrompts.BuildVisualThesisPrompt(
            entity, "Description.", "Guidance.",
            culturalVisualIdentity: "Northern culture favors heavy furs and carved bone.");

        Assert.Contains("Northern culture favors heavy furs", user);
        Assert.Contains("Culture: northern", user);
    }

    [Fact]
    public void BuildVisualThesisPrompt_WithFraming_PrependedToUserPrompt()
    {
        var entity = MakeEntity();
        var (_, user) = DescriptionPrompts.BuildVisualThesisPrompt(
            entity, "Description.", "Guidance.",
            visualThesisFraming: "Focus on what identifies this entity at first glance.");

        Assert.StartsWith("Focus on what identifies", user);
    }

    [Fact]
    public void BuildVisualThesisPrompt_UserPromptContainsDescriptionSection()
    {
        var entity = MakeEntity();
        var (_, user) = DescriptionPrompts.BuildVisualThesisPrompt(
            entity, "A tall warrior in iron plate.", "Guidance.");

        Assert.Contains("DESCRIPTION (extract visual elements from this):", user);
        Assert.Contains("A tall warrior in iron plate.", user);
        Assert.Contains("Generate the visual thesis.", user);
    }

    // =========================================================================
    // BuildVisualTraitsPrompt — subtype, trait guidance, cultural identity
    // =========================================================================

    [Fact]
    public void BuildVisualTraitsPrompt_BasicOutput_Contains2to4TraitsInstruction()
    {
        var entity = MakeEntity();
        var (system, user) = DescriptionPrompts.BuildVisualTraitsPrompt(
            entity, "A hulking figure.", "Description text.", "Per-kind guidance.");

        Assert.Contains("2-4 traits", system);
        Assert.Contains("THESIS (the primary silhouette", user);
        Assert.Contains("A hulking figure.", user);
        Assert.Contains("Generate 2-4 visual traits", user);
    }

    [Fact]
    public void BuildVisualTraitsPrompt_WithSubtype_IncludesSubtypeSection()
    {
        var entity = MakeEntity();
        var (system, _) = DescriptionPrompts.BuildVisualTraitsPrompt(
            entity, "Thesis.", "Desc.", "Guidance.",
            subtype: "berserker");

        Assert.Contains("SUBTYPE: berserker", system);
    }

    [Fact]
    public void BuildVisualTraitsPrompt_WithTraitGuidance_IncludesRequiredDirections()
    {
        var entity = MakeEntity();
        var guidance = new DescriptionPrompts.TraitGuidance(
        [
            new DescriptionPrompts.TraitGuidanceCategory(
                "Texture", "Surface qualities", ["rough stone", "polished metal"]),
            new DescriptionPrompts.TraitGuidanceCategory(
                "Posture", "How they carry themselves", ["hunched", "upright"]),
        ]);

        var (system, _) = DescriptionPrompts.BuildVisualTraitsPrompt(
            entity, "Thesis.", "Desc.", "Guidance.",
            traitGuidance: guidance);

        Assert.Contains("REQUIRED DIRECTIONS (address at least one):", system);
        Assert.Contains("Texture: Surface qualities (e.g., rough stone, polished metal)", system);
        Assert.Contains("Posture: How they carry themselves (e.g., hunched, upright)", system);
    }

    [Fact]
    public void BuildVisualTraitsPrompt_WithCulturalVisualIdentity_IncludesInUserPrompt()
    {
        var entity = MakeEntity();
        var (_, user) = DescriptionPrompts.BuildVisualTraitsPrompt(
            entity, "Thesis.", "Desc.", "Guidance.",
            culturalVisualIdentity: "Iron-forged people with ritual scarring.");

        Assert.Contains("Iron-forged people with ritual scarring.", user);
    }

    [Fact]
    public void BuildVisualTraitsPrompt_WithFraming_PrependedToUserPrompt()
    {
        var entity = MakeEntity();
        var (_, user) = DescriptionPrompts.BuildVisualTraitsPrompt(
            entity, "Thesis.", "Desc.", "Guidance.",
            visualTraitsFraming: "Think about what makes this entity unique in a crowd.");

        Assert.StartsWith("Think about what makes", user);
    }

    [Fact]
    public void BuildVisualTraitsPrompt_EmptyTraitGuidance_NoRequiredDirections()
    {
        var entity = MakeEntity();
        var guidance = new DescriptionPrompts.TraitGuidance([]);

        var (system, _) = DescriptionPrompts.BuildVisualTraitsPrompt(
            entity, "Thesis.", "Desc.", "Guidance.",
            traitGuidance: guidance);

        Assert.DoesNotContain("REQUIRED DIRECTIONS", system);
    }
}
