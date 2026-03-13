using System.Text.Json;
using System.Text.Json.Serialization;
using TheCanonry.ApiClients.Llm;
using TheCanonry.Illuminator.Enrichment;

namespace TheCanonry.Illuminator.Chronicle.PerspectiveSynthesis;

/// <summary>
/// Orchestrates perspective synthesis: constellation analysis → LLM call →
/// facet enforcement → faceted fact assembly.
/// </summary>
public sealed class PerspectiveSynthesizer
{
    private readonly ILlmClient _llm;

    public PerspectiveSynthesizer(ILlmClient llm)
    {
        _llm = llm;
    }

    /// <summary>
    /// Synthesize a perspective for the given constellation of entities.
    /// </summary>
    public async Task<PerspectiveSynthesisResult> SynthesizeAsync(
        PerspectiveSynthesisInput input,
        CancellationToken ct = default)
    {
        // Step 1: Separate generation constraints — always included verbatim
        var generationConstraints = input.FactsWithMetadata
            .Where(f => f.Type == FactType.GenerationConstraint && !f.Disabled)
            .ToList();

        // Step 2: Assembled tone is the core tone fragment
        var assembledTone = input.ToneFragments.Core;

        // Step 3: Resolve world dynamics (needed for result + prompt)
        var resolvedWorldDynamics = PerspectivePrompts.ResolveWorldDynamics(input);

        // Step 4: Build prompts
        var systemPrompt = PerspectivePrompts.SystemPrompt;
        var userPrompt = PerspectivePrompts.BuildUserPrompt(input);

        // Step 5: Call LLM
        var request = new LlmRequest
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            Model = LlmModel.Sonnet46,
            ThinkingBudget = 4096,
            MaxTokens = 1024,
            Temperature = 0.7,
        };

        var response = await _llm.CompleteAsync(request, ct);

        if (response.Error is not null)
            throw new InvalidOperationException($"Perspective synthesis LLM call failed: {response.Error}");

        // Step 6: Parse JSON response
        var synthesis = ParseSynthesisResponse(response.Text);

        // Step 7: Enforce required facts
        var enforcedSynthesis = EnforceFacetRequirements(synthesis, input);

        // Step 8: Build faceted facts
        var facetedFacts = BuildFacetedFacts(enforcedSynthesis, input.FactsWithMetadata, generationConstraints);

        // Step 9: Compute cost (input: $3/MTok, output: $15/MTok for Sonnet 3.5)
        var actualCost = ComputeCost(response.Usage.InputTokens, response.Usage.OutputTokens);

        return new PerspectiveSynthesisResult
        {
            Synthesis = enforcedSynthesis,
            AssembledTone = assembledTone,
            FacetedFacts = facetedFacts,
            ResolvedWorldDynamics = resolvedWorldDynamics,
            InputTokens = response.Usage.InputTokens,
            OutputTokens = response.Usage.OutputTokens,
            ActualCost = actualCost,
        };
    }

    // =========================================================================
    // Response parsing
    // =========================================================================

    private static PerspectiveSynthesis ParseSynthesisResponse(string rawText)
    {
        if (!LlmResponseParser.TryExtractJson<RawSynthesisResponse>(rawText, out var parsed) || parsed is null)
            throw new JsonException("Perspective synthesis: failed to parse LLM JSON response");

        if (string.IsNullOrWhiteSpace(parsed.Brief))
            throw new JsonException("Perspective synthesis: missing 'brief' in LLM response");

        return new PerspectiveSynthesis
        {
            Brief = parsed.Brief,
            Facets = NormalizeFacets(parsed.Facets),
            SuggestedMotifs = NormalizeMotifs(parsed.SuggestedMotifs),
            NarrativeVoice = NormalizeNarrativeVoice(parsed.NarrativeVoice),
            EntityDirectives = NormalizeEntityDirectives(parsed.EntityDirectives),
            TemporalNarrative = string.IsNullOrWhiteSpace(parsed.TemporalNarrative)
                ? null
                : parsed.TemporalNarrative,
        };
    }

    private static IReadOnlyList<FactFacet> NormalizeFacets(IReadOnlyList<RawFacet>? raw)
    {
        if (raw is null) return [];
        return raw
            .Where(f => !string.IsNullOrEmpty(f.FactId) && !string.IsNullOrEmpty(f.Interpretation))
            .Select(f => new FactFacet(f.FactId!, f.Interpretation!))
            .ToList();
    }

    private static IReadOnlyList<string> NormalizeMotifs(IReadOnlyList<string>? raw)
        => raw?.Where(m => !string.IsNullOrEmpty(m)).ToList() ?? [];

    private static IReadOnlyDictionary<string, string> NormalizeNarrativeVoice(
        IReadOnlyDictionary<string, string>? raw)
        => raw?.Where(kv => !string.IsNullOrEmpty(kv.Value))
               .ToDictionary(kv => kv.Key, kv => kv.Value)
           ?? new Dictionary<string, string>();

    private static IReadOnlyList<EntityDirective> NormalizeEntityDirectives(
        IReadOnlyList<RawEntityDirective>? raw)
    {
        if (raw is null) return [];
        return raw
            .Where(d => !string.IsNullOrEmpty(d.EntityId) && !string.IsNullOrEmpty(d.Directive))
            .Select(d => new EntityDirective(d.EntityId!, d.EntityName ?? "", d.Directive!))
            .ToList();
    }

    // =========================================================================
    // Facet requirement enforcement
    // =========================================================================

    private static PerspectiveSynthesis EnforceFacetRequirements(
        PerspectiveSynthesis synthesis,
        PerspectiveSynthesisInput input)
    {
        var worldTruthFacts = input.FactsWithMetadata
            .Where(f => f.Type != FactType.GenerationConstraint && !f.Disabled)
            .ToList();

        var requiredIds = worldTruthFacts
            .Where(f => f.Required)
            .Select(f => f.Id)
            .ToList();

        if (requiredIds.Count == 0)
            return synthesis;

        // Build lookup of facets already returned by LLM
        var facetsById = new Dictionary<string, string>(StringComparer.Ordinal);
        var orderedFacetIds = new List<string>();
        foreach (var facet in synthesis.Facets)
        {
            if (string.IsNullOrEmpty(facet.FactId) || string.IsNullOrEmpty(facet.Interpretation))
                continue;
            if (facetsById.TryAdd(facet.FactId, facet.Interpretation))
                orderedFacetIds.Add(facet.FactId);
        }

        var fallback = BuildFallbackInterpretation(input);
        var finalFacets = new List<FactFacet>(synthesis.Facets.Count + requiredIds.Count);
        var addedIds = new HashSet<string>(StringComparer.Ordinal);

        // Required facts first
        foreach (var requiredId in requiredIds)
        {
            var interpretation = facetsById.TryGetValue(requiredId, out var existing)
                ? existing
                : fallback;
            finalFacets.Add(new FactFacet(requiredId, interpretation));
            addedIds.Add(requiredId);
        }

        // Then remaining facets in original order
        foreach (var facetId in orderedFacetIds)
        {
            if (addedIds.Contains(facetId)) continue;
            if (!facetsById.TryGetValue(facetId, out var interpretation)) continue;
            finalFacets.Add(new FactFacet(facetId, interpretation));
            addedIds.Add(facetId);
        }

        return synthesis with { Facets = finalFacets };
    }

    private static string BuildFallbackInterpretation(PerspectiveSynthesisInput input)
    {
        var focus = input.Constellation.FocusSummary is { Length: > 0 } summary
            ? $"In this {summary} chronicle, "
            : "In this chronicle, ";
        var era = input.FocalEra?.Name is { Length: > 0 } eraName
            ? $"set in the {eraName} era, "
            : "";
        return $"{focus}{era}this truth is foregrounded, shaping the stakes and how the entities interpret events.";
    }

    // =========================================================================
    // Faceted fact assembly
    // =========================================================================

    private static IReadOnlyList<string> BuildFacetedFacts(
        PerspectiveSynthesis synthesis,
        IReadOnlyList<CanonFactWithMetadata> factsWithMetadata,
        IReadOnlyList<CanonFactWithMetadata> generationConstraints)
    {
        var factMap = factsWithMetadata.ToDictionary(f => f.Id, f => f, StringComparer.Ordinal);

        var facetedWorldTruths = synthesis.Facets
            .Where(f => !string.IsNullOrEmpty(f.FactId) && !string.IsNullOrEmpty(f.Interpretation))
            .Select(f =>
            {
                if (factMap.TryGetValue(f.FactId, out var baseFact))
                    return $"{baseFact.Text} [FACET: {f.Interpretation}]";
                return f.Interpretation;
            })
            .ToList();

        var constraintTexts = generationConstraints.Select(c => c.Text).ToList();

        return [.. facetedWorldTruths, .. constraintTexts];
    }

    // =========================================================================
    // Cost calculation
    // =========================================================================

    private static decimal ComputeCost(int inputTokens, int outputTokens)
    {
        var inputCost = LlmModel.InputCostPerMTok(LlmModel.Sonnet46);
        var outputCost = LlmModel.OutputCostPerMTok(LlmModel.Sonnet46);
        return (inputTokens * inputCost / 1_000_000m)
             + (outputTokens * outputCost / 1_000_000m);
    }

    // =========================================================================
    // Raw JSON deserialization shapes (instantiated by System.Text.Json)
    // =========================================================================

#pragma warning disable CA1812 // These classes are instantiated by JsonSerializer.Deserialize<T>()
    private sealed class RawSynthesisResponse
    {
        [JsonPropertyName("brief")]
        public string? Brief { get; set; }

        [JsonPropertyName("facets")]
        public List<RawFacet>? Facets { get; set; }

        [JsonPropertyName("suggestedMotifs")]
        public List<string>? SuggestedMotifs { get; set; }

        [JsonPropertyName("narrativeVoice")]
        public Dictionary<string, string>? NarrativeVoice { get; set; }

        [JsonPropertyName("entityDirectives")]
        public List<RawEntityDirective>? EntityDirectives { get; set; }

        [JsonPropertyName("temporalNarrative")]
        public string? TemporalNarrative { get; set; }
    }

    private sealed class RawFacet
    {
        [JsonPropertyName("factId")]
        public string? FactId { get; set; }

        [JsonPropertyName("interpretation")]
        public string? Interpretation { get; set; }
    }

    private sealed class RawEntityDirective
    {
        [JsonPropertyName("entityId")]
        public string? EntityId { get; set; }

        [JsonPropertyName("entityName")]
        public string? EntityName { get; set; }

        [JsonPropertyName("directive")]
        public string? Directive { get; set; }
    }
#pragma warning restore CA1812
}
