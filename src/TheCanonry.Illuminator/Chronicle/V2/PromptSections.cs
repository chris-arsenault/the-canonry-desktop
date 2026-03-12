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
        var parts = new List<string>();

        var kindLine = entity.Subtype is not null
            ? $"{entity.Name} ({entity.Kind}/{entity.Subtype}"
            : $"{entity.Name} ({entity.Kind}";

        if (entity.Culture is not null)
            kindLine += $", {entity.Culture}";

        kindLine += $", {entity.Prominence}, {entity.Status})";
        parts.Add(kindLine);

        if (entity.Description is not null)
            parts.Add(entity.Description);
        else if (entity.Summary is not null)
            parts.Add(entity.Summary);

        return string.Join("\n", parts);
    }

    /// <summary>
    /// Brief entity description: name, kind, and culture only.
    /// </summary>
    public static string FormatEntityBrief(EntityContext entity)
    {
        var culture = entity.Culture is not null ? $", {entity.Culture}" : "";
        return $"{entity.Name} ({entity.Kind}{culture})";
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
        };

        foreach (var fact in ctx.CanonFacts)
            lines.Add($"- {fact}");

        if (ctx.CanonFacts.Any(f => f.Contains("[FACET:")))
        {
            lines.Add("");
            lines.Add("Note: Facts marked [FACET: ...] are interpretations specific to this constellation — use them as thematic lenses, not literal constraints.");
        }

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

        var lines = new List<string> { "# Relationships" };
        foreach (var r in relationships)
            lines.Add($"- {r.SourceName} --[{r.Kind}]--> {r.TargetName}");

        return string.Join("\n", lines);
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

        var lines = new List<string>
        {
            "# Name Bank",
            "Culture-appropriate names for invented characters:",
        };

        foreach (var (culture, names) in nameBank)
        {
            if (names.Count > 0)
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

        var lines = new List<string>
        {
            "# Narrative Lens",
            "This story exists in the shadow of:",
            FormatEntityFull(lensEntity),
            "",
            "Lens Guidance: This entity is not a character in the story. It is the weight everything else is measured against — the context that gives meaning to what happens. It should be felt, not described.",
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
            Write to fulfill this direction. It takes precedence over any structural defaults.
            """;
    }
}
