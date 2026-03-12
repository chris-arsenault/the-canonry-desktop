using TheCanonry.Schema.World;

namespace TheCanonry.Illuminator.Enrichment.Prompts;

/// <summary>
/// Prompt builders for the 3-step description chain:
/// Narrative -> Visual Thesis -> Visual Traits.
/// </summary>
public static class DescriptionPrompts
{
    /// <summary>
    /// Build the system and user prompts for the narrative step.
    /// Generates summary, description, and aliases.
    /// </summary>
    public static (string SystemPrompt, string UserPrompt) BuildNarrativePrompt(
        Entity entity, string entityContext, string? narrativeHint = null, bool lockedSummary = false)
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

        var userPrompt = entityContext;

        return (systemPrompt, userPrompt);
    }

    /// <summary>
    /// Build the system and user prompts for the visual thesis step.
    /// Produces ONE sentence describing the dominant visual feature.
    /// </summary>
    public static (string SystemPrompt, string UserPrompt) BuildVisualThesisPrompt(
        Entity entity, string description, string kindInstructions, string? visualAvoid = null)
    {
        var systemPrompt = $"""
            You distill descriptions into dominant visual signals. Your prompt contains:

            - Visual Context: Entity basics and culture
            - Description: Source material
            - Per-Kind Guidance: What to emphasize

            Output ONE sentence. Shape only - no color, texture, or suggestive language ("as if", "suggesting").
            """;

        if (visualAvoid is not null)
            systemPrompt += $"\n\nAVOID: {visualAvoid}";

        systemPrompt += $"\n\n{kindInstructions}";

        var userPrompt = $"""
            Entity: {entity.Name} ({entity.Kind})
            Culture: {entity.Culture}

            DESCRIPTION (extract visual elements from this):
            {description}

            Generate the visual thesis.
            """;

        return (systemPrompt, userPrompt);
    }

    /// <summary>
    /// Build the system and user prompts for the visual traits step.
    /// Produces 2-4 traits expanding the visual identity beyond the thesis.
    /// </summary>
    public static (string SystemPrompt, string UserPrompt) BuildVisualTraitsPrompt(
        Entity entity, string visualThesis, string description, string kindInstructions)
    {
        var systemPrompt = $"""
            You expand visual theses with supporting details. Your prompt contains:

            - Thesis: Primary visual signal (don't repeat)
            - Visual Context: Entity basics and culture
            - Description: Source for additional features
            - Palette Guidance: Required directions (if provided)

            Output 2-4 traits, one per line. Each 3-8 words, adding something NEW.

            {kindInstructions}
            """;

        var userPrompt = $"""
            THESIS (the primary silhouette - don't repeat, expand):
            {visualThesis}

            Entity: {entity.Name} ({entity.Kind})
            Culture: {entity.Culture}

            DESCRIPTION (source material for additional distinctive features):
            {description}

            Generate 2-4 visual traits that ADD to the thesis - features it didn't cover.
            """;

        return (systemPrompt, userPrompt);
    }
}
