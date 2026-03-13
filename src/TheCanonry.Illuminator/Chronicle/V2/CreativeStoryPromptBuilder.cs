using TheCanonry.Illuminator.Chronicle.PerspectiveSynthesis;

namespace TheCanonry.Illuminator.Chronicle.V2;

/// <summary>
/// Builds V2 creative-freedom story prompts.
/// Same PS outputs, same world data, same entity selection as structured mode.
/// Difference is purely in how the generation prompt frames the material:
/// fiction-writer identity, creative target, structure as suggestion.
/// </summary>
public static class CreativeStoryPromptBuilder
{
    /// <summary>
    /// System prompt for creative freedom generation — fiction writer identity
    /// focused on finding the specific, haunting detail.
    /// </summary>
    public static string SystemPrompt =>
        """
        You are a fiction writer. Find the one detail that makes this story specific — the gesture, the object, the sensory moment no other story contains. Build outward from there. The measure is whether a reader finishes this and sits with it. Avoid the patterns, rhythms, and hedging that mark machine-generated text.

        Your prompt contains:

        GUIDANCE (pre-synthesized — follow their intent, find your own voice):
        - Tone & Atmosphere: Synthesized prose guidance for this chronicle
        - Character Notes: Per-entity guidance for portrayal

        WORLD DATA (what to write about):
        - Cast: Characters and narrative roles
        - World: Setting, canon facts, events, relationships

        Entity descriptions reflect who characters BECAME. Write them as they WERE during the story's events.

        Tone & Atmosphere and Character Notes are pre-synthesized guidance — follow their intent closely, but express them in your own voice.
        """;

    /// <summary>
    /// Builds the creative user prompt by assembling sections in the canonical order.
    /// Same order as structured: TASK DATA then WORLD DATA.
    /// </summary>
    public static string BuildUserPrompt(StoryPromptContext ctx)
    {
        var sections = new List<string>();

        // TASK DATA
        sections.Add(BuildTaskSection(ctx));

        var narrativeDirection = PromptSections.BuildNarrativeDirectionSection(ctx.GenerationContext.NarrativeDirection);
        if (!string.IsNullOrEmpty(narrativeDirection))
            sections.Add(narrativeDirection);

        sections.Add(BuildStructureSection(ctx));

        if (ctx.EventInstructions is not null)
            sections.Add($"# How to Use Events\n{ctx.EventInstructions}");

        var narrativeVoice = BuildCreativeNarrativeVoiceSection(ctx.NarrativeVoice);
        if (!string.IsNullOrEmpty(narrativeVoice))
            sections.Add(narrativeVoice);

        var entityDirectives = BuildCreativeEntityDirectivesSection(ctx.EntityDirectives);
        if (!string.IsNullOrEmpty(entityDirectives))
            sections.Add(entityDirectives);

        // Writing style — same as structured, including craft posture
        {
            var tone = ctx.GenerationContext.Tone;
            var hasContent = false;
            var styleLines = new List<string> { "# Writing Style" };

            if (!string.IsNullOrEmpty(tone))
            {
                styleLines.Add("");
                styleLines.Add(tone);
                hasContent = true;
            }

            if (ctx.ProseInstructions is not null)
            {
                if (hasContent) styleLines.Add("");
                var header = ctx.StyleName is not null ? $"## Prose: {ctx.StyleName}" : "## Prose";
                styleLines.Add(header);
                styleLines.Add(ctx.ProseInstructions);
                hasContent = true;
            }

            if (ctx.CraftPosture is not null)
            {
                if (hasContent) styleLines.Add("");
                styleLines.Add("## Craft Posture");
                styleLines.Add("How to relate to the material — density, withholding, and elaboration:");
                styleLines.Add(ctx.CraftPosture);
                hasContent = true;
            }

            if (hasContent)
                sections.Add(string.Join("\n", styleLines));
        }

        // WORLD DATA — same as structured
        var castSection = BuildCastSection(ctx);
        if (!string.IsNullOrEmpty(castSection))
            sections.Add(castSection);

        var lensSection = PromptSections.BuildNarrativeLensSection(ctx.LensEntity);
        if (!string.IsNullOrEmpty(lensSection))
            sections.Add(lensSection);

        sections.Add(PromptSections.BuildWorldSection(ctx.GenerationContext));

        var nameBankSection = PromptSections.BuildNameBankSection(ctx.NameBank, ctx.PrimaryEntities);
        if (!string.IsNullOrEmpty(nameBankSection))
            sections.Add(nameBankSection);

        var temporalSection = PromptSections.BuildTemporalSection(
            ctx.GenerationContext.FocalEraName, null, ctx.TemporalNarrative);
        if (!string.IsNullOrEmpty(temporalSection))
            sections.Add(temporalSection);

        var dataSection = PromptSections.BuildDataSection(ctx.GenerationContext.Relationships);
        if (!string.IsNullOrEmpty(dataSection))
            sections.Add(dataSection);

        return string.Join("\n\n", sections);
    }

    /// <summary>
    /// Creative task section — "find the one image", not a requirements list.
    /// </summary>
    private static string BuildTaskSection(StoryPromptContext ctx) =>
        $"""
        # Task
        Write a {ctx.MinWords}-{ctx.MaxWords} word story.

        Find the one image the reader won't forget. Build outward from there.

        - You may reassign characters to different roles or invent minor characters
        - The narrative structure below is a starting shape, not a requirement
        - Write directly with no section headers or meta-commentary
        """;

    /// <summary>
    /// Structure section — presented as a starting shape, not prescription.
    /// </summary>
    private static string BuildStructureSection(StoryPromptContext ctx)
    {
        var lines = new List<string>
        {
            "# Narrative Structure",
            "One possible shape for this story. Use it, adapt it, or find a better one.",
        };

        if (ctx.MinScenes > 0 || ctx.MaxScenes > 0)
        {
            lines.Add("");
            lines.Add($"Target: {ctx.MinScenes}-{ctx.MaxScenes} scenes");
        }

        if (ctx.NarrativeInstructions is not null)
        {
            lines.Add("");
            lines.Add(ctx.NarrativeInstructions);
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Narrative voice for creative mode — "Tone &amp; Atmosphere", not "Story Bible".
    /// </summary>
    private static string BuildCreativeNarrativeVoiceSection(IReadOnlyDictionary<string, string>? narrativeVoice)
    {
        if (narrativeVoice is null || narrativeVoice.Count == 0)
            return string.Empty;

        var lines = new List<string>
        {
            "# Tone & Atmosphere",
            "Synthesized prose guidance for this chronicle:",
            "",
        };

        foreach (var (key, value) in narrativeVoice)
            lines.Add($"**{key}**: {value}");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Entity directives for creative mode — "Character Notes", not "Story Bible".
    /// </summary>
    private static string BuildCreativeEntityDirectivesSection(IReadOnlyList<EntityDirective>? directives)
    {
        if (directives is null || directives.Count == 0)
            return string.Empty;

        var lines = new List<string>
        {
            "# Character Notes",
            "Specific guidance for writing each entity — interpret creatively, don't reproduce this language directly:",
            "",
        };

        foreach (var d in directives)
            lines.Add($"**{d.EntityName}**: {d.Directive}");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Cast section — same as structured mode.
    /// </summary>
    private static string BuildCastSection(StoryPromptContext ctx)
    {
        var primary = ctx.PrimaryEntities;
        var supporting = ctx.SupportingEntities;

        if (primary.Count == 0 && supporting.Count == 0)
            return string.Empty;

        var total = primary.Count + supporting.Count;
        var lines = new List<string> { $"# Cast ({total} characters)" };
        lines.Add("");
        lines.Add(
            "**Temporal context**: Entity descriptions reflect their CURRENT state — who they became, how they ended up. " +
            "This chronicle depicts PAST EVENTS when characters who are now dead/changed were alive and active. " +
            "Write them as they WERE during the story, not as they ARE now.");

        if (primary.Count > 0)
        {
            lines.Add("");
            lines.Add("## Primary Characters");
            foreach (var e in primary)
            {
                lines.Add("");
                lines.Add($"### {e.Name}");
                lines.Add(PromptSections.FormatEntityFull(e));
            }
        }

        if (supporting.Count > 0)
        {
            lines.Add("");
            lines.Add("## Supporting Characters");
            foreach (var e in supporting)
            {
                lines.Add("");
                lines.Add(PromptSections.FormatEntityBrief(e));
            }
        }

        return string.Join("\n", lines);
    }
}
