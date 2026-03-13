using TheCanonry.Schema.World;

namespace TheCanonry.Illuminator.Enrichment.Prompts;

/// <summary>
/// Prompt builders for the 3-step description chain:
/// Narrative -> Visual Thesis -> Visual Traits.
/// Matches TS descriptionTask.ts.
/// </summary>
public static class DescriptionPrompts
{
    // =========================================================================
    // Era Timeline Helpers (same format as chronicle prompts)
    // =========================================================================

    /// <summary>
    /// Era info for building historical context and world timeline.
    /// Matches TS <c>EraTemporalInfo</c>.
    /// </summary>
    public sealed record EraTemporalInfo(
        string Id,
        string Name,
        string? Summary,
        int Order,
        int StartTick,
        int EndTick);

    /// <summary>
    /// Trait guidance for visual trait diversity (palette-assigned categories).
    /// Matches TS <c>TraitGuidance</c>.
    /// </summary>
    public sealed record TraitGuidance(
        IReadOnlyList<TraitGuidanceCategory> AssignedCategories);

    public sealed record TraitGuidanceCategory(
        string Category,
        string Description,
        IReadOnlyList<string> Examples);

    /// <summary>
    /// Add "the" article to an era name, handling names that already start with "The".
    /// </summary>
    private static string WithArticle(string name)
    {
        if (name.StartsWith("The ", StringComparison.Ordinal))
            return "the " + name[4..];
        return "the " + name;
    }

    /// <summary>
    /// Build a natural language world timeline.
    /// E.g., "The world passed through the Dawn Age, then the Age of Expansion. It now exists in the Clever Ice Age."
    /// </summary>
    public static string BuildWorldTimeline(IReadOnlyList<EraTemporalInfo> eras, string focalEraId)
    {
        var sorted = eras.OrderBy(e => e.Order).ToList();
        var focalIndex = sorted.FindIndex(e => e.Id == focalEraId);

        if (focalIndex == -1) return "";

        var past = sorted.Take(focalIndex).ToList();
        var current = sorted[focalIndex];
        var future = sorted.Skip(focalIndex + 1).ToList();

        var parts = new List<string>();

        if (past.Count > 0)
        {
            var pastNames = string.Join(", then ", past.Select(e => WithArticle(e.Name)));
            parts.Add($"The world passed through {pastNames}.");
        }

        parts.Add($"It now exists in {WithArticle(current.Name)}.");

        if (future.Count > 0)
        {
            var futureNames = string.Join(", then ", future.Select(e => WithArticle(e.Name)));
            parts.Add($"{futureNames} {(future.Count == 1 ? "lies" : "lie")} ahead.");
        }

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Build the historical context section for description prompts.
    /// Includes focal era name, summary, and world timeline.
    /// </summary>
    public static string BuildHistoricalContext(
        EraTemporalInfo? focalEra,
        IReadOnlyList<EraTemporalInfo>? allEras)
    {
        if (focalEra is null || allEras is null || allEras.Count == 0) return "";

        var lines = new List<string> { "HISTORICAL CONTEXT:" };

        lines.Add($"Era: {focalEra.Name}");
        if (!string.IsNullOrEmpty(focalEra.Summary))
            lines.Add(focalEra.Summary);

        lines.Add("");
        lines.Add(BuildWorldTimeline(allEras, focalEra.Id));

        return string.Join("\n", lines);
    }

    // =========================================================================
    // Step 1: Narrative prompt
    // =========================================================================

    /// <summary>
    /// Build the system and user prompts for the narrative step.
    /// Generates summary, description, and aliases.
    /// </summary>
    public static (string SystemPrompt, string UserPrompt) BuildNarrativePrompt(
        Entity entity,
        string entityContext,
        string? narrativeHint = null,
        bool lockedSummary = false,
        EraTemporalInfo? focalEra = null,
        IReadOnlyList<EraTemporalInfo>? allEras = null)
    {
        var hintBlock = narrativeHint is not null
            ? $"NARRATIVE HINT (do not contradict):\n\"{narrativeHint}\"\n\n"
            : "";

        string systemPrompt;
        if (lockedSummary)
        {
            systemPrompt = $"""
                You expand narrative hints into rich descriptions. Your prompt contains:

                {hintBlock}WORLD DATA:
                - Historical Context: Era and world timeline
                - Entity: Core identity (kind, status, prominence, culture)
                - Relationships: Connections with strength markers
                - Cultural Identity: How this culture thinks, speaks, acts

                TASK DATA:
                - Output: JSON with description, aliases

                Expand and enrich. Don't paraphrase the hint.
                """;
        }
        else
        {
            systemPrompt = $"""
                You are a creative writer building world lore. Your prompt contains:

                {hintBlock}WORLD DATA:
                - Historical Context: Era and world timeline
                - Entity: Core identity (kind, status, prominence, culture)
                - Relationships: Connections with strength markers
                - Cultural Identity: How this culture thinks, speaks, acts

                TASK DATA:
                - Output: JSON with summary, description, aliases

                Write personality over plot. One [strong] relationship anchors the narrative.

                USING EVENTS: Notable events are SOURCE MATERIAL, not a checklist. Pick 1-2 evocative moments to weave deeply into the description. Leave most events implied or unmentioned. The description is a narrative impression, not a timeline.
                """;
        }

        // Build historical context and prepend to entity context
        var historicalContext = BuildHistoricalContext(focalEra, allEras);
        var userPrompt = !string.IsNullOrEmpty(historicalContext)
            ? $"{historicalContext}\n\n---\n\n{entityContext}"
            : entityContext;

        return (systemPrompt, userPrompt);
    }

    // =========================================================================
    // Step 2: Visual Thesis prompt
    // =========================================================================

    /// <summary>
    /// Build the system and user prompts for the visual thesis step.
    /// Produces ONE sentence describing the dominant visual feature.
    /// </summary>
    public static (string SystemPrompt, string UserPrompt) BuildVisualThesisPrompt(
        Entity entity,
        string description,
        string kindInstructions,
        string? visualAvoid = null,
        string? visualThesisFraming = null,
        string? culturalVisualIdentity = null)
    {
        var systemPrompt =
            "You distill descriptions into dominant visual signals. Your prompt contains:\n\n" +
            "- Visual Context: Entity basics and culture\n" +
            "- Description: Source material\n" +
            "- Per-Kind Guidance: What to emphasize\n\n" +
            "Output ONE sentence. Shape only - no color, texture, or suggestive language (\"as if\", \"suggesting\").";

        if (visualAvoid is not null)
            systemPrompt += $"\n\nAVOID: {visualAvoid}";

        systemPrompt += $"\n\n{kindInstructions}";

        // Build visual context with cultural visual identity
        var identitySuffix = !string.IsNullOrEmpty(culturalVisualIdentity)
            ? "\n\n" + culturalVisualIdentity
            : "";
        var cultureLabel = entity.Culture.ToString() is { Length: > 0 } cult ? cult : "unaffiliated";
        var visualContext = $"Entity: {entity.Name} ({entity.Kind})\n" +
            $"Culture: {cultureLabel}{identitySuffix}";

        // Build user prompt with optional per-kind framing
        var framingPrefix = !string.IsNullOrEmpty(visualThesisFraming)
            ? visualThesisFraming + "\n\n"
            : "";

        var userPrompt = $"{framingPrefix}{visualContext}\n\n" +
            $"DESCRIPTION (extract visual elements from this):\n{description}\n\n" +
            "Generate the visual thesis.";

        return (systemPrompt, userPrompt);
    }

    // =========================================================================
    // Step 3: Visual Traits prompt
    // =========================================================================

    /// <summary>
    /// Build the system and user prompts for the visual traits step.
    /// Produces 2-4 traits expanding the visual identity beyond the thesis.
    /// </summary>
    public static (string SystemPrompt, string UserPrompt) BuildVisualTraitsPrompt(
        Entity entity,
        string visualThesis,
        string description,
        string kindInstructions,
        string? subtype = null,
        TraitGuidance? traitGuidance = null,
        string? visualTraitsFraming = null,
        string? culturalVisualIdentity = null)
    {
        var systemPrompt =
            "You expand visual theses with supporting details. Your prompt contains:\n\n" +
            "- Thesis: Primary visual signal (don't repeat)\n" +
            "- Visual Context: Entity basics and culture\n" +
            "- Description: Source for additional features\n" +
            "- Palette Guidance: Required directions (if provided)\n\n" +
            "Output 2-4 traits, one per line. Each 3-8 words, adding something NEW.";

        systemPrompt += $"\n\n{kindInstructions}";

        if (!string.IsNullOrEmpty(subtype))
            systemPrompt += $"\n\nSUBTYPE: {subtype}";

        if (traitGuidance is { AssignedCategories.Count: > 0 })
        {
            systemPrompt += "\n\nREQUIRED DIRECTIONS (address at least one):";
            foreach (var p in traitGuidance.AssignedCategories)
            {
                var examples = string.Join(", ", p.Examples.Take(2));
                systemPrompt += $"\n- {p.Category}: {p.Description} (e.g., {examples})";
            }
        }

        // Build visual context with cultural visual identity
        var identitySuffix = !string.IsNullOrEmpty(culturalVisualIdentity)
            ? "\n\n" + culturalVisualIdentity
            : "";
        var cultureLabel = entity.Culture.ToString() is { Length: > 0 } cult ? cult : "unaffiliated";
        var visualContext = $"Entity: {entity.Name} ({entity.Kind})\n" +
            $"Culture: {cultureLabel}{identitySuffix}";

        // Build user prompt with optional per-kind framing
        var framingPrefix = !string.IsNullOrEmpty(visualTraitsFraming)
            ? visualTraitsFraming + "\n\n"
            : "";

        var userPrompt = $"{framingPrefix}THESIS (the primary silhouette - don't repeat, expand):\n{visualThesis}\n\n" +
            $"{visualContext}\n\n" +
            $"DESCRIPTION (source material for additional distinctive features):\n{description}\n\n" +
            "Generate 2-4 visual traits that ADD to the thesis - features it didn't cover.";

        return (systemPrompt, userPrompt);
    }
}
