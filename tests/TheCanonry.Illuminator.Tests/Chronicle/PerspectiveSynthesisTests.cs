using TheCanonry.Illuminator.Chronicle;
using TheCanonry.Illuminator.Chronicle.PerspectiveSynthesis;
using TheCanonry.Illuminator.Tests.Enrichment;

namespace TheCanonry.Illuminator.Tests.Chronicle;

public sealed class PerspectiveSynthesisTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

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

    private static RelationshipContext MakeRel(string sourceId, string targetId, string kind) =>
        new()
        {
            SourceId = sourceId,
            TargetId = targetId,
            SourceName = sourceId,
            TargetName = targetId,
            Kind = kind,
        };

    private static PerspectiveSynthesisInput MakeMinimalInput(
        IReadOnlyList<EntityContext>? entities = null,
        IReadOnlyList<CanonFactWithMetadata>? facts = null,
        NarrativeStyle? style = null) =>
        new()
        {
            Constellation = ConstellationAnalyzer.Analyze(
                entities ?? [],
                []),
            Entities = entities ?? [],
            FactsWithMetadata = facts ?? [],
            ToneFragments = new ToneFragments("Mythic and melancholic"),
            NarrativeStyle = style,
        };

    private static string MakeValidSynthesisJson(
        string brief = "A chronicle brief.",
        IEnumerable<(string FactId, string Interp)>? facets = null,
        IEnumerable<(string EntityId, string EntityName, string Directive)>? directives = null)
    {
        var facetJson = facets is null
            ? "[]"
            : "[" + string.Join(",", facets.Select(f =>
                $"{{\"factId\":\"{f.FactId}\",\"interpretation\":\"{f.Interp}\"}}")) + "]";

        var directiveJson = directives is null
            ? "[]"
            : "[" + string.Join(",", directives.Select(d =>
                $"{{\"entityId\":\"{d.EntityId}\",\"entityName\":\"{d.EntityName}\",\"directive\":\"{d.Directive}\"}}")) + "]";

        return $$"""
            {
              "brief": "{{brief}}",
              "facets": {{facetJson}},
              "suggestedMotifs": ["motif one", "motif two"],
              "narrativeVoice": {"TONE": "mythic and cold"},
              "entityDirectives": {{directiveJson}}
            }
            """;
    }

    // =========================================================================
    // ConstellationAnalyzer tests
    // =========================================================================

    [Fact]
    public void Analyze_SingleCulture_ReturnsSingleBalance()
    {
        var entities = new[]
        {
            MakeEntity("e1", "Aldric", culture: "northern"),
            MakeEntity("e2", "Bran", culture: "northern"),
            MakeEntity("e3", "Cael", culture: "northern"),
        };

        var result = ConstellationAnalyzer.Analyze(entities, []);

        Assert.Equal(CultureBalance.Single, result.CultureBalance);
        Assert.Equal("northern", result.DominantCulture);
    }

    [Fact]
    public void Analyze_MixedCultures_ReturnsMixedBalance()
    {
        var entities = new[]
        {
            MakeEntity("e1", "Aldric", culture: "northern"),
            MakeEntity("e2", "Senna", culture: "southern"),
            MakeEntity("e3", "Tova", culture: "eastern"),
        };

        var result = ConstellationAnalyzer.Analyze(entities, []);

        Assert.Equal(CultureBalance.Mixed, result.CultureBalance);
        Assert.Null(result.DominantCulture);
    }

    [Fact]
    public void Analyze_DominantCulture_ReturnsDominantBalance()
    {
        var entities = new[]
        {
            MakeEntity("e1", "Aldric", culture: "northern"),
            MakeEntity("e2", "Bran", culture: "northern"),
            MakeEntity("e3", "Cael", culture: "northern"),
            MakeEntity("e4", "Senna", culture: "southern"),
        };

        // 3/4 northern = 75%, which is > 50% but not > 80%
        var result = ConstellationAnalyzer.Analyze(entities, []);

        Assert.Equal(CultureBalance.Dominant, result.CultureBalance);
        Assert.Equal("northern", result.DominantCulture);
    }

    [Fact]
    public void Analyze_KindFocusClassification_CharacterWhenNpcDominates()
    {
        var entities = new[]
        {
            MakeEntity("e1", "Aldric", kind: "npc"),
            MakeEntity("e2", "Bran", kind: "npc"),
            MakeEntity("e3", "Cael", kind: "npc"),
            MakeEntity("e4", "Ruins", kind: "location"),
        };

        var result = ConstellationAnalyzer.Analyze(entities, []);

        Assert.Equal(KindFocus.Character, result.KindFocus);
        Assert.Equal("npc", result.DominantKind);
    }

    [Fact]
    public void Analyze_KindFocusClassification_MixedWhenNoSingleKindDominates()
    {
        var entities = new[]
        {
            MakeEntity("e1", "Aldric", kind: "npc"),
            MakeEntity("e2", "Ruins", kind: "location"),
        };

        var result = ConstellationAnalyzer.Analyze(entities, []);

        Assert.Equal(KindFocus.Mixed, result.KindFocus);
    }

    [Fact]
    public void Analyze_RelationshipKindCounting_AggregatesCorrectly()
    {
        var entities = new[]
        {
            MakeEntity("e1", "Aldric"),
            MakeEntity("e2", "Bran"),
            MakeEntity("e3", "Cael"),
        };
        var relationships = new[]
        {
            MakeRel("e1", "e2", "ally"),
            MakeRel("e2", "e3", "ally"),
            MakeRel("e1", "e3", "rival"),
        };

        var result = ConstellationAnalyzer.Analyze(entities, relationships);

        Assert.Equal(2, result.RelationshipKinds["ally"]);
        Assert.Equal(1, result.RelationshipKinds["rival"]);
    }

    [Fact]
    public void Analyze_EmptyEntities_ReturnsSafeDefaults()
    {
        var result = ConstellationAnalyzer.Analyze([], []);

        Assert.Equal(CultureBalance.Mixed, result.CultureBalance);
        Assert.Null(result.DominantCulture);
        Assert.Equal(KindFocus.Mixed, result.KindFocus);
        Assert.Equal(SpatialSpread.Tight, result.SpatialSpread);
        Assert.NotNull(result.FocusSummary);
    }

    // =========================================================================
    // PerspectivePrompts tests
    // =========================================================================

    [Fact]
    public void SystemPrompt_ContainsKeyPhrases()
    {
        var prompt = PerspectivePrompts.SystemPrompt;

        Assert.Contains("perspective consultant", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NARRATIVE STYLE", prompt);
        Assert.Contains("FACET", prompt);
        Assert.Contains("ENTITY DIRECTIVES", prompt);
        Assert.Contains("Output ONLY valid JSON", prompt);
    }

    [Fact]
    public void BuildUserPrompt_IncludesFactIdsAndEntityNames()
    {
        var entities = new[]
        {
            MakeEntity("e1", "Aldric", culture: "northern"),
            MakeEntity("e2", "Senna", culture: "southern"),
        };
        var facts = new[]
        {
            new CanonFactWithMetadata("fact-1", "The ice remembers."),
            new CanonFactWithMetadata("fact-2", "Scholars guard the archives."),
        };
        var input = MakeMinimalInput(entities, facts);

        var prompt = PerspectivePrompts.BuildUserPrompt(input);

        Assert.Contains("fact-1", prompt);
        Assert.Contains("fact-2", prompt);
        Assert.Contains("Aldric", prompt);
        Assert.Contains("Senna", prompt);
        Assert.Contains("The ice remembers.", prompt);
    }

    [Fact]
    public void BuildUserPrompt_RequiredFacts_MarkedInPrompt()
    {
        var facts = new[]
        {
            new CanonFactWithMetadata("fact-1", "The ice remembers.", Required: true),
            new CanonFactWithMetadata("fact-2", "Scholars guard the archives."),
        };
        var input = MakeMinimalInput(facts: facts);

        var prompt = PerspectivePrompts.BuildUserPrompt(input);

        Assert.Contains("REQUIRED", prompt);
        Assert.Contains("fact-1", prompt);
    }

    [Fact]
    public void BuildUserPrompt_GenerationConstraints_ExcludedFromWorldFacts()
    {
        var facts = new[]
        {
            new CanonFactWithMetadata("fact-1", "The ice remembers.", Type: FactType.WorldTruth),
            new CanonFactWithMetadata("con-1", "Max 800 words.", Type: FactType.GenerationConstraint),
        };
        var input = MakeMinimalInput(facts: facts);

        var prompt = PerspectivePrompts.BuildUserPrompt(input);

        Assert.Contains("fact-1", prompt);
        // Generation constraint should NOT appear in the WORLD FACTS section
        Assert.DoesNotContain("con-1", prompt);
    }

    [Fact]
    public void BuildUserPrompt_NarrativeStyle_IncludedInPrompt()
    {
        var style = new NarrativeStyle("s1", "Mythic Epic", ProseGuidance: "Write with gravitas.");
        var input = MakeMinimalInput(style: style);

        var prompt = PerspectivePrompts.BuildUserPrompt(input);

        Assert.Contains("Mythic Epic", prompt);
        Assert.Contains("Write with gravitas.", prompt);
    }

    // =========================================================================
    // PerspectiveSynthesizer tests
    // =========================================================================

    [Fact]
    public async Task Synthesize_RequiredFactEnforcement_AddsMissingFacets()
    {
        // LLM returns facets that omit the required fact
        var json = MakeValidSynthesisJson(
            facets: [("fact-2", "Secondary fact interpretation.")]);

        var mock = new MockLlmClient().Enqueue(json, inputTokens: 100, outputTokens: 200);

        var facts = new[]
        {
            new CanonFactWithMetadata("fact-1", "The ice remembers.", Required: true),
            new CanonFactWithMetadata("fact-2", "Scholars guard archives."),
        };
        var input = MakeMinimalInput(facts: facts);
        var synthesizer = new PerspectiveSynthesizer(mock);

        var result = await synthesizer.SynthesizeAsync(input);

        // fact-1 must appear in facets even though LLM omitted it
        var facetIds = result.Synthesis.Facets.Select(f => f.FactId).ToList();
        Assert.Contains("fact-1", facetIds);
    }

    [Fact]
    public async Task Synthesize_RequiredFact_AppearsFirstInFacets()
    {
        var json = MakeValidSynthesisJson(
            facets:
            [
                ("fact-2", "Second fact."),
                ("fact-3", "Third fact."),
            ]);

        var mock = new MockLlmClient().Enqueue(json, inputTokens: 100, outputTokens: 200);

        var facts = new[]
        {
            new CanonFactWithMetadata("fact-1", "The ice remembers.", Required: true),
            new CanonFactWithMetadata("fact-2", "Scholars guard archives."),
            new CanonFactWithMetadata("fact-3", "Magic costs blood."),
        };
        var input = MakeMinimalInput(facts: facts);
        var synthesizer = new PerspectiveSynthesizer(mock);

        var result = await synthesizer.SynthesizeAsync(input);

        // Required fact should be first
        Assert.Equal("fact-1", result.Synthesis.Facets[0].FactId);
    }

    [Fact]
    public async Task Synthesize_GenerationConstraints_ExcludedFromLlm_IncludedInOutput()
    {
        var json = MakeValidSynthesisJson(facets: [("fact-1", "Ice remembers interpretation.")]);
        var mock = new MockLlmClient().Enqueue(json, inputTokens: 100, outputTokens: 200);

        var facts = new[]
        {
            new CanonFactWithMetadata("fact-1", "The ice remembers.", Type: FactType.WorldTruth),
            new CanonFactWithMetadata("con-1", "Max 800 words.", Type: FactType.GenerationConstraint),
        };
        var input = MakeMinimalInput(facts: facts);
        var synthesizer = new PerspectiveSynthesizer(mock);

        var result = await synthesizer.SynthesizeAsync(input);

        // Constraint must NOT appear in LLM system/user prompts
        var sentRequest = mock.Requests.Single();
        Assert.DoesNotContain("con-1", sentRequest.UserPrompt);

        // But "Max 800 words." must appear in the output faceted facts
        Assert.Contains(result.FacetedFacts, f => f.Contains("Max 800 words."));
    }

    [Fact]
    public async Task Synthesize_FacetedFacts_FormatIsFactTextPlusFacetLabel()
    {
        var json = MakeValidSynthesisJson(facets: [("fact-1", "Manifests as cold dread.")]);
        var mock = new MockLlmClient().Enqueue(json, inputTokens: 100, outputTokens: 200);

        var facts = new[]
        {
            new CanonFactWithMetadata("fact-1", "The ice remembers."),
        };
        var input = MakeMinimalInput(facts: facts);
        var synthesizer = new PerspectiveSynthesizer(mock);

        var result = await synthesizer.SynthesizeAsync(input);

        Assert.Single(result.FacetedFacts);
        Assert.Equal("The ice remembers. [FACET: Manifests as cold dread.]", result.FacetedFacts[0]);
    }

    [Fact]
    public async Task Synthesize_TokenUsage_AggregatedCorrectly()
    {
        var json = MakeValidSynthesisJson();
        var mock = new MockLlmClient().Enqueue(json, inputTokens: 150, outputTokens: 300);

        var input = MakeMinimalInput();
        var synthesizer = new PerspectiveSynthesizer(mock);

        var result = await synthesizer.SynthesizeAsync(input);

        Assert.Equal(150, result.InputTokens);
        Assert.Equal(300, result.OutputTokens);
        Assert.True(result.ActualCost > 0m);
    }

    [Fact]
    public async Task Synthesize_AssembledTone_IsCoreToneFragment()
    {
        var json = MakeValidSynthesisJson();
        var mock = new MockLlmClient().Enqueue(json);

        var input = MakeMinimalInput();
        var synthesizer = new PerspectiveSynthesizer(mock);

        var result = await synthesizer.SynthesizeAsync(input);

        Assert.Equal("Mythic and melancholic", result.AssembledTone);
    }

    [Fact]
    public async Task Synthesize_LlmError_ThrowsInvalidOperationException()
    {
        var mock = new MockLlmClient().EnqueueError("Rate limited");
        var input = MakeMinimalInput();
        var synthesizer = new PerspectiveSynthesizer(mock);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => synthesizer.SynthesizeAsync(input));
    }

    [Fact]
    public async Task Synthesize_DisabledFact_ExcludedFromPromptAndOutput()
    {
        var json = MakeValidSynthesisJson();
        var mock = new MockLlmClient().Enqueue(json);

        var facts = new[]
        {
            new CanonFactWithMetadata("fact-1", "The ice remembers."),
            new CanonFactWithMetadata("fact-disabled", "Hidden truth.", Disabled: true),
        };
        var input = MakeMinimalInput(facts: facts);
        var synthesizer = new PerspectiveSynthesizer(mock);

        var result = await synthesizer.SynthesizeAsync(input);

        var sentRequest = mock.Requests.Single();
        Assert.DoesNotContain("fact-disabled", sentRequest.UserPrompt);
        Assert.DoesNotContain("Hidden truth.", sentRequest.UserPrompt);
        Assert.DoesNotContain("Hidden truth.", string.Join('\n', result.FacetedFacts));
    }
}
