using TheCanonry.Illuminator.Chronicle.PerspectiveSynthesis;

namespace TheCanonry.Illuminator.Chronicle.V2;

/// <summary>
/// Static methods that build individual prompt sections for V2 chronicle generation.
/// Each method returns a string (empty if section not applicable).
/// </summary>
public static class PromptSections
{
    /// <summary>
    /// Full entity description including kind, subtype, culture, prominence, and description.
    /// </summary>
    public static string FormatEntityFull(EntityContext entity)
    {
        var kindLabel = entity.Subtype is not null
            ? $"{entity.Kind}/{entity.Subtype}"
            : entity.Kind;

        var lines = new List<string>
        {
            $"Kind: {kindLabel}",
            $"Prominence: {entity.Prominence}",
        };

        if (entity.Culture is not null)
            lines.Add($"Culture: {entity.Culture}");

        lines.Add("");
        lines.Add(entity.Description ?? entity.Summary ?? "(no description available)");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Brief entity description: name, kind, and culture only.
    /// </summary>
    public static string FormatEntityBrief(EntityContext entity)
    {
        var briefKindLabel = entity.Subtype is not null
            ? $"{entity.Kind}/{entity.Subtype}"
            : entity.Kind;
        var cultureSuffix = entity.Culture is not null ? $", Culture: {entity.Culture}" : "";
        var desc = entity.Description ?? entity.Summary ?? "(no description available)";
        return $"### {entity.Name} ({briefKindLabel})\nProminence: {entity.Prominence}{cultureSuffix}\n{desc}";
    }

    /// <summary>
    /// World section: name, description, and canon facts.
    /// Includes a note about [FACET: ...] markers if any facts contain them.
    /// </summary>
    public static string BuildWorldSection(ChronicleGenerationContext ctx)
    {
        if (ctx.CanonFacts.Count == 0)
            return $"# World: {ctx.WorldName}\n{ctx.WorldDescription}";

        var lines = new List<string>
        {
            $"# World: {ctx.WorldName}",
            ctx.WorldDescription,
            "",
            "Canon Facts:",
            "(Facts marked with [FACET: ...] include a lens specific to this chronicle. Prioritize the facet - it shows how the universal truth applies to these particular entities and circumstances. The base fact provides context; the facet is your guide.)",
        };

        foreach (var fact in ctx.CanonFacts)
            lines.Add($"- {fact}");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Relationships section listing directed connections between entities.
    /// Format: "- SourceName --[kind]--> TargetName"
    /// </summary>
    public static string BuildDataSection(IReadOnlyList<RelationshipContext> relationships)
    {
        if (relationships.Count == 0)
            return string.Empty;

        var collapsed = CollapseBidirectionalRelationships(relationships);
        var lines = new List<string> { "# Relationships" };
        foreach (var (rel, isMutual) in collapsed)
        {
            lines.Add(isMutual
                ? $"- {rel.SourceName} <--[{rel.Kind}]--> {rel.TargetName} (mutual)"
                : $"- {rel.SourceName} --[{rel.Kind}]--> {rel.TargetName}");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Collapse bidirectional relationships (A→B and B→A of same kind) into single entries.
    /// </summary>
    private static IReadOnlyList<(RelationshipContext Rel, bool IsMutual)> CollapseBidirectionalRelationships(
        IReadOnlyList<RelationshipContext> relationships)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<(RelationshipContext, bool)>();
        var relSet = new HashSet<string>(
            relationships.Select(r => $"{r.SourceId}|{r.TargetId}|{r.Kind}"),
            StringComparer.Ordinal);

        foreach (var r in relationships)
        {
            var key = string.Compare(r.SourceId, r.TargetId, StringComparison.Ordinal) <= 0
                ? $"{r.SourceId}|{r.TargetId}|{r.Kind}"
                : $"{r.TargetId}|{r.SourceId}|{r.Kind}";

            if (!seen.Add(key))
                continue;

            var reverseKey = $"{r.TargetId}|{r.SourceId}|{r.Kind}";
            var isMutual = relSet.Contains(reverseKey);
            result.Add((r, isMutual));
        }

        return result;
    }

    /// <summary>
    /// Historical context section: era name, description, and temporal narrative.
    /// </summary>
    public static string BuildTemporalSection(string? eraName, string? eraDescription, string? temporalNarrative)
    {
        if (eraName is null && eraDescription is null && temporalNarrative is null)
            return string.Empty;

        var lines = new List<string> { "# Historical Context" };

        if (eraName is not null)
        {
            lines.Add(eraDescription is not null
                ? $"{eraName}: {eraDescription}"
                : eraName);
        }

        if (temporalNarrative is not null)
        {
            if (eraName is not null)
                lines.Add("");
            lines.Add(temporalNarrative);
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Name bank section: culture-appropriate names for invented characters.
    /// Excludes cultures already represented by primary entities.
    /// </summary>
    public static string BuildNameBankSection(
        IReadOnlyDictionary<string, IReadOnlyList<string>>? nameBank,
        IReadOnlyList<EntityContext> entities)
    {
        if (nameBank is null || nameBank.Count == 0)
            return string.Empty;

        var entityCultures = new HashSet<string>(
            entities.Where(e => e.Culture is not null).Select(e => e.Culture!),
            StringComparer.Ordinal);

        var lines = new List<string>
        {
            "# Name Bank",
            "Culture-appropriate names for invented characters:",
        };

        // Entity-represented cultures first
        foreach (var culture in entityCultures)
        {
            if (nameBank.TryGetValue(culture, out var names) && names.Count > 0)
                lines.Add($"- {culture}: {string.Join(", ", names)}");
        }

        // Then remaining cultures
        foreach (var (culture, names) in nameBank)
        {
            if (!entityCultures.Contains(culture) && names.Count > 0)
                lines.Add($"- {culture}: {string.Join(", ", names)}");
        }

        return lines.Count > 2 ? string.Join("\n", lines) : string.Empty;
    }

    /// <summary>
    /// Narrative voice section: Story Bible tone and atmosphere notes.
    /// Format: "**key**: value"
    /// </summary>
    public static string BuildNarrativeVoiceSection(IReadOnlyDictionary<string, string>? narrativeVoice)
    {
        if (narrativeVoice is null || narrativeVoice.Count == 0)
            return string.Empty;

        var lines = new List<string>
        {
            "# Story Bible: Tone & Atmosphere",
            "Reference notes on emotional texture — draw on these:",
        };

        foreach (var (key, value) in narrativeVoice)
            lines.Add($"**{key}**: {value}");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Entity directives section: Story Bible character notes.
    /// Format: "**entityName**: directive"
    /// </summary>
    public static string BuildEntityDirectivesSection(IReadOnlyList<EntityDirective>? directives)
    {
        if (directives is null || directives.Count == 0)
            return string.Empty;

        var lines = new List<string>
        {
            "# Story Bible: Character Notes",
            "Background on relationships and history — bring alive through specificity, don't explain:",
        };

        foreach (var d in directives)
            lines.Add($"**{d.EntityName}**: {d.Directive}");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Narrative lens section: a contextual entity that shapes the story without being a character.
    /// </summary>
    public static string BuildNarrativeLensSection(EntityContext? lensEntity)
    {
        if (lensEntity is null)
            return string.Empty;

        var kindLabel = lensEntity.Subtype is not null
            ? $"{lensEntity.Kind}/{lensEntity.Subtype}"
            : lensEntity.Kind;
        var lensCultureSuffix = lensEntity.Culture is not null ? $", Culture: {lensEntity.Culture}" : "";

        var lines = new List<string>
        {
            "# Narrative Lens",
            "This story exists in the shadow of:",
            "",
            $"## {lensEntity.Name} ({kindLabel})",
            $"Prominence: {lensEntity.Prominence}{lensCultureSuffix}",
            "",
            lensEntity.Description ?? lensEntity.Summary ?? "(no description available)",
            "",
            "Lens Guidance: This entity is NOT a character in the story. It is the context — the constraint, the backdrop, the thing everyone knows but no one can change. It should be felt in characters' choices, in what is possible and impossible, in what goes unsaid. Reference it naturally, never explain it to the reader as if they don't know it.",
        };

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Narrative direction section: specific narrative purpose for this chronicle.
    /// </summary>
    public static string BuildNarrativeDirectionSection(string? narrativeDirection)
    {
        if (narrativeDirection is null)
            return string.Empty;

        return $"""
            # Narrative Direction
            This chronicle has a specific narrative purpose:
            "{narrativeDirection}"
            Write to fulfill this direction. Every structural and tonal choice should serve it.
            """;
    }
}
