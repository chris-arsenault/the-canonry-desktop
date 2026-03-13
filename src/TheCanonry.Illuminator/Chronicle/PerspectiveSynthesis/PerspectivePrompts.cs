using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Chronicle.PerspectiveSynthesis;

/// <summary>
/// Prompt builders for perspective synthesis LLM calls.
/// </summary>
public static class PerspectivePrompts
{
    // JSON example for the output format instruction — kept as a constant to avoid
    // interpolated raw string brace-escaping issues.
    private const string OutputFormatExample =
        """{"brief": "...", "facets": [{"factId": "fact-id", "interpretation": "1-2 sentences max"}], "suggestedMotifs": ["phrase one", "phrase two"], "narrativeVoice": {"KEY": "concrete prose guidance"}, "entityDirectives": [{"entityId": "id", "entityName": "Name", "directive": "1-3 sentences"}], "temporalNarrative": "2-4 sentences (optional)"}""";

    /// <summary>
    /// The system prompt for the perspective synthesis LLM call.
    /// </summary>
    public static readonly string SystemPrompt =
        """
        You are a perspective consultant for a fantasy chronicle series. Your job is to help each chronicle feel like a STORY about PEOPLE, not a document about a world.

        You will receive:
        1. NARRATIVE STYLE: What kind of story this is and how it should be written — READ THIS CAREFULLY, it determines the emotional register
        2. BASELINE MATERIAL: Core tone, world facts, cultural identities, entity portrayal guidelines, and world dynamics
        3. CONSTELLATION ANALYSIS: What cultures, entity types, themes, and dynamics are present
        4. ENTITIES: The specific characters, places, etc. in this chronicle

        Your task:
        - MATCH THE NARRATIVE STYLE. Read the prose guidance carefully — it tells you what this story needs. Do not impose dimensions the style doesn't call for.
        - Consider the NARRATIVE STYLE, CULTURAL IDENTITIES, and WORLD DYNAMICS when faceting
        - FACET the world facts - for each selected fact, provide a short interpretation showing how it applies to THIS chronicle
        - Keep interpretations to 1-2 sentences - they will be appended to the original fact
        - Synthesize a NARRATIVE VOICE with 3-5 keys. Choose dimensions that serve THIS style — let the prose guidance inform what matters.
        - Generate ENTITY DIRECTIVES for each entity. What matters about them for THIS story type? Let the style guide you.
        - If any facts are marked REQUIRED, they MUST appear in facets

        Your goal: Help the author write fiction that matches the narrative style's intent.

        IMPORTANT: Output ONLY valid JSON. No markdown, no explanation, no commentary.
        """;

    /// <summary>
    /// Build the user prompt for the perspective synthesis LLM call.
    /// </summary>
    public static string BuildUserPrompt(PerspectiveSynthesisInput input)
    {
        var sections = BuildSections(input);

        var narrativeDirectionBlock = input.NarrativeDirection is { Length: > 0 } nd
            ? $"""

               === NARRATIVE DIRECTION (PRIMARY CONSTRAINT) ===
               The author has specified this concrete direction for the chronicle:
               "{nd}"

               Your perspective brief, entity directives, motifs, and fact facets must all serve this specific narrative. Treat this as the organizing thesis — every synthesis decision should support it.

               """
            : "";

        var constellationBlock = BuildConstellationBlock(
            input.Constellation,
            sections.PresentCultureIds,
            input.FocalEra,
            sections.EntitySummaries);

        var requiredFactsLine = sections.RequiredFactIds.Count > 0
            ? $"Required facts: {string.Join(", ", sections.RequiredFactIds)}"
            : "Required facts: none";

        return $"""
            === NARRATIVE STYLE ===
            {sections.NarrativeStyleDisplay}
            {narrativeDirectionBlock}
            === BASELINE MATERIAL ===

            CORE TONE (applies to all chronicles):
            {input.ToneFragments.Core}

            WORLD FACTS (truths about this world):
            {(sections.WorldTruthFactsDisplay.Length > 0 ? sections.WorldTruthFactsDisplay : "No world facts provided.")}

            CULTURAL IDENTITIES (for cultures present in this chronicle):
            {sections.CulturalIdentitiesDisplay}

            ENTITY PORTRAYAL GUIDELINES (per entity kind):
            {sections.ProseHintsDisplay}

            WORLD DYNAMICS (narrative context about inter-group forces and behaviors):
            {sections.WorldDynamicsDisplay}

            {constellationBlock}

            === FACT SELECTION ===
            {sections.FactSelectionLine}
            {requiredFactsLine}

            === YOUR TASK ===

            Based on the narrative style, constellation, and cultural identities above, create a perspective for this chronicle.

            CRITICAL: Match the narrative style. The prose guidance tells you what this story needs — follow it.

            Provide a JSON object with:

            1. "brief": A perspective brief (100-150 words) describing what matters emotionally for THIS story type. Let the style's prose guidance inform what to focus on.

            2. "facets": {sections.FacetSelectionInstruction} facts most relevant to this chronicle. REQUIRED facts (if any) must be included; if required facts exceed the default range, include all required and do not add optional facts. For each, provide a 1-2 sentence interpretation explaining how this fact specifically manifests or matters for the entities and story type in this chronicle. The original fact will be included; your interpretation adds the specific lens.

            3. "suggestedMotifs": 2-3 short phrases that might echo through this chronicle — appropriate to the style.

            4. "narrativeVoice": 3-5 atmospheric anchors appropriate to THIS narrative style. Choose dimensions that serve the story type. Do NOT provide ending or closure guidance.

            5. "entityDirectives": For each entity, 1-2 sentences on what matters about them for THIS story. Let the style guide what to focus on. Include entityId, entityName, and directive.

            6. "temporalNarrative" (optional): If WORLD DYNAMICS were provided above, synthesize them into 2-4 sentences of story-specific stakes for THIS chronicle. What conditions shape what's possible? What pressures bear on these characters? If no world dynamics are provided, omit this field entirely.

            Output format:
            """ + OutputFormatExample;
    }

    // =========================================================================
    // Internal section builders
    // =========================================================================

    private sealed record PromptSections(
        string NarrativeStyleDisplay,
        string WorldTruthFactsDisplay,
        IReadOnlyList<string> RequiredFactIds,
        IReadOnlyList<string> PresentCultureIds,
        string CulturalIdentitiesDisplay,
        string ProseHintsDisplay,
        string EntitySummaries,
        string WorldDynamicsDisplay,
        IReadOnlyList<ResolvedWorldDynamic> ResolvedWorldDynamics,
        string FactSelectionLine,
        string FacetSelectionInstruction);

    private static PromptSections BuildSections(PerspectiveSynthesisInput input)
    {
        var narrativeStyleDisplay = BuildNarrativeStyleDisplay(input.NarrativeStyle);

        var worldTruthFacts = input.FactsWithMetadata
            .Where(f => f.Type != FactType.GenerationConstraint && !f.Disabled)
            .ToList();
        var requiredFacts = worldTruthFacts
            .Where(f => f.Required)
            .ToList();
        var requiredFactIds = requiredFacts.Select(f => f.Id).ToList();
        var worldTruthFactsDisplay = string.Join('\n',
            worldTruthFacts.Select(f => $"- [{f.Id}]{(f.Required ? " (REQUIRED)" : "")}: {f.Text}"));

        var presentCultureIds = input.Constellation.Cultures.Keys.ToList();
        var culturalIdentitiesDisplay = BuildCulturalIdentitiesDisplay(
            input.CulturalIdentities, presentCultureIds);

        var entityKinds = new HashSet<string>(
            input.Entities.Select(e => e.Kind), StringComparer.OrdinalIgnoreCase);
        var proseHintsDisplay = BuildProseHintsDisplay(input.ProseHints, entityKinds);

        var entitySummaries = BuildEntitySummaries(input.Entities, input.RoleAssignments);

        var resolvedWorldDynamics = ResolveWorldDynamics(input);
        var worldDynamicsDisplay = resolvedWorldDynamics.Count > 0
            ? string.Join('\n', resolvedWorldDynamics.Select(d => $"- {d.Text}"))
            : "No world dynamics declared.";

        var (factSelectionLine, facetSelectionInstruction) =
            ComputeFactSelectionDisplay(input.FactSelection, requiredFacts.Count);

        return new PromptSections(
            NarrativeStyleDisplay: narrativeStyleDisplay,
            WorldTruthFactsDisplay: worldTruthFactsDisplay,
            RequiredFactIds: requiredFactIds,
            PresentCultureIds: presentCultureIds,
            CulturalIdentitiesDisplay: culturalIdentitiesDisplay,
            ProseHintsDisplay: proseHintsDisplay,
            EntitySummaries: entitySummaries,
            WorldDynamicsDisplay: worldDynamicsDisplay,
            ResolvedWorldDynamics: resolvedWorldDynamics,
            FactSelectionLine: factSelectionLine,
            FacetSelectionInstruction: facetSelectionInstruction);
    }

    private static string BuildNarrativeStyleDisplay(NarrativeStyle? style)
    {
        if (style is null)
            return "No specific narrative style.";

        var parts = new List<string>(8)
        {
            $"Name: {style.Name}",
        };

        if (style.Format is { Length: > 0 })
            parts.Add($"Format: {style.Format}");

        if (style.Description is { Length: > 0 })
            parts.Add($"Description: {style.Description}");

        if (style.Tags is { Count: > 0 })
            parts.Add($"Tags: {string.Join(", ", style.Tags)}");

        if (style.ProseGuidance is { Length: > 0 })
            parts.Add($"\nProse guidance:\n{style.ProseGuidance}");

        if (style.CraftPosture is { Length: > 0 })
            parts.Add($"\nCraft posture (density and restraint):\n{style.CraftPosture}");

        return string.Join('\n', parts);
    }

    private static string BuildCulturalIdentitiesDisplay(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? identities,
        IReadOnlyList<string> presentCultureIds)
    {
        if (identities is null || presentCultureIds.Count == 0)
            return "No cultural identities provided.";

        var sections = presentCultureIds
            .Where(id => identities.ContainsKey(id))
            .Select(cultureId =>
            {
                var traits = identities[cultureId];
                var traitLines = string.Join('\n',
                    traits.Select(kv => $"  {kv.Key}: {kv.Value}"));
                return $"## {cultureId}\n{traitLines}";
            })
            .ToList();

        return sections.Count > 0
            ? string.Join("\n\n", sections)
            : "No cultural identities provided.";
    }

    private static string BuildProseHintsDisplay(
        IReadOnlyDictionary<string, string>? proseHints,
        IReadOnlySet<string> entityKinds)
    {
        if (proseHints is null || proseHints.Count == 0)
            return "No entity portrayal guidelines provided.";

        var lines = proseHints
            .Where(kv => entityKinds.Contains(kv.Key))
            .Select(kv => $"  {kv.Key}: {kv.Value}")
            .ToList();

        return lines.Count > 0
            ? string.Join('\n', lines)
            : "No entity portrayal guidelines provided.";
    }

    private static string BuildEntitySummaries(
        IReadOnlyList<EntityContext> entities,
        IReadOnlyList<ChronicleRoleAssignment>? roleAssignments)
    {
        var roleByEntityId = new Dictionary<string, ChronicleRoleAssignment>(StringComparer.Ordinal);
        if (roleAssignments is not null)
        {
            foreach (var ra in roleAssignments)
                roleByEntityId[ra.EntityId] = ra;
        }

        return string.Join('\n', entities.Take(10).Select(e =>
        {
            var tags = e.Tags is { Count: > 0 }
                ? $" [{string.Join(", ", e.Tags.Select(kv => $"{kv.Key}={kv.Value}"))}]"
                : "";
            var roleLabel = roleByEntityId.TryGetValue(e.Id, out var ra)
                ? $", role: {ra.Role}{(ra.IsPrimary ? " (primary)" : "")}"
                : "";
            return $"- {e.Name} ({e.Kind}, {e.Culture ?? "unknown"}, {e.Prominence}{roleLabel}){tags}: {e.Summary ?? "(no summary)"}";
        }));
    }

    private static string BuildConstellationBlock(
        EntityConstellation constellation,
        IReadOnlyList<string> presentCultureIds,
        EraContext? focalEra,
        string entitySummaries)
    {
        var kindsDisplay = string.Join(", ",
            constellation.Kinds.Select(kv => $"{kv.Key}({kv.Value})"));

        var tagsDisplay = constellation.ProminentTags.Count > 0
            ? string.Join(", ", constellation.ProminentTags)
            : "none identified";

        var relDisplay = constellation.RelationshipKinds.Count > 0
            ? string.Join(", ", constellation.RelationshipKinds.Select(kv => $"{kv.Key}({kv.Value})"))
            : "none";

        var cultureDisplay = string.Join(", ", presentCultureIds);
        if (string.IsNullOrEmpty(cultureDisplay))
            cultureDisplay = "mixed/unknown";

        var dominantSuffix = constellation.DominantCulture is not null
            ? $" (dominant: {constellation.DominantCulture})"
            : "";

        return $"""
            === THIS CHRONICLE'S CONSTELLATION ===

            Summary: {constellation.FocusSummary}
            Cultures present: {cultureDisplay}
            Culture balance: {constellation.CultureBalance}{dominantSuffix}
            Entity types: {kindsDisplay}
            Prominent themes: {tagsDisplay}
            Relationship kinds: {relDisplay}
            Era: {focalEra?.Name ?? "unspecified"}

            ENTITIES IN THIS CHRONICLE:
            {entitySummaries}
            """;
    }

    // =========================================================================
    // Fact selection
    // =========================================================================

    public static (string FactSelectionLine, string FacetSelectionInstruction)
        ComputeFactSelectionDisplay(FactSelectionConfig? factSelection, int requiredFactCount)
    {
        var requestedMin = factSelection?.MinCount;
        var requestedMax = factSelection?.MaxCount;
        var hasCustomRange =
            (requestedMin is > 0) || (requestedMax is > 0);

        if (!hasCustomRange)
        {
            return (
                "Fact selection target: default (4-6). Required facts must still be included.",
                "Select 4-6");
        }

        var effectiveMin = Math.Max(requestedMin ?? 4, requiredFactCount);
        var effectiveMax = Math.Max(requestedMax ?? effectiveMin, effectiveMin);

        if (effectiveMin == effectiveMax)
        {
            return (
                $"Fact selection target: exactly {effectiveMin} (required facts count toward this).",
                $"Select exactly {effectiveMin}");
        }

        return (
            $"Fact selection target: {effectiveMin}-{effectiveMax} (required facts count toward this).",
            $"Select {effectiveMin}-{effectiveMax}");
    }

    // =========================================================================
    // World dynamics resolution
    // =========================================================================

    internal static IReadOnlyList<ResolvedWorldDynamic> ResolveWorldDynamics(
        PerspectiveSynthesisInput input)
    {
        if (input.WorldDynamics is not { Count: > 0 })
            return Array.Empty<ResolvedWorldDynamic>();

        var presentCultureIds = input.Constellation.Cultures.Keys.ToHashSet(StringComparer.Ordinal);
        var cultureTokenSet = new HashSet<string>(
            presentCultureIds.Select(NormalizeToken), StringComparer.Ordinal);
        var entityKinds = new HashSet<string>(
            input.Entities.Select(e => e.Kind), StringComparer.OrdinalIgnoreCase);
        var kindTokenSet = new HashSet<string>(
            entityKinds.Select(NormalizeToken), StringComparer.Ordinal);
        var focalEraId = input.FocalEra?.Id;

        return input.WorldDynamics
            .Where(d =>
            {
                var cultures = d.Cultures ?? (IReadOnlyList<string>)Array.Empty<string>();
                var kinds = d.Kinds ?? (IReadOnlyList<string>)Array.Empty<string>();

                var cultureMatch = cultures.Count == 0
                    || cultures.Contains("*")
                    || cultures.Any(c => presentCultureIds.Contains(c) || cultureTokenSet.Contains(NormalizeToken(c)));

                var kindMatch = kinds.Count == 0
                    || kinds.Contains("*")
                    || kinds.Any(k => entityKinds.Contains(k) || kindTokenSet.Contains(NormalizeToken(k)));

                return cultureMatch && kindMatch;
            })
            .Select(d =>
            {
                var text = d.Text;
                if (focalEraId is not null && d.EraOverrides?.TryGetValue(focalEraId, out var ov) == true)
                    text = ov.Replace ? ov.Text : $"{d.Text} {ov.Text}";
                return new ResolvedWorldDynamic(d.Id, text);
            })
            .ToList();
    }

    private static string NormalizeToken(string value) =>
        new(value.ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());
}
