using TheCanonry.Illuminator.Chronicle.PerspectiveSynthesis;

namespace TheCanonry.Illuminator.Chronicle.V2;

/// <summary>
/// Input context for story prompt building.
/// </summary>
public sealed record StoryPromptContext
{
    public required ChronicleGenerationContext GenerationContext { get; init; }
    public required IReadOnlyList<EntityContext> PrimaryEntities { get; init; }
    public required IReadOnlyList<EntityContext> SupportingEntities { get; init; }
    // From perspective synthesis
    public IReadOnlyDictionary<string, string>? NarrativeVoice { get; init; }
    public IReadOnlyList<EntityDirective>? EntityDirectives { get; init; }
    public string? TemporalNarrative { get; init; }
    // From narrative style
    public string? NarrativeInstructions { get; init; }
    public string? EventInstructions { get; init; }
    public string? ProseInstructions { get; init; }
    public string? CraftPosture { get; init; }
    public int MinWords { get; init; } = 2000;
    public int MaxWords { get; init; } = 4000;
    public int MinScenes { get; init; } = 3;
    public int MaxScenes { get; init; } = 6;
    // Optional
    public EntityContext? LensEntity { get; init; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? NameBank { get; init; }
}

/// <summary>
/// Builds V2 story-format chronicle prompts.
/// </summary>
public static class StoryPromptBuilder
{
    /// <summary>
    /// Returns the system prompt for story-format chronicle generation.
    /// </summary>
    public static string GetSystemPrompt() =>
        """
        You are an expert fantasy author writing engaging fiction. Your readers expect vivid characters, emotional truth, and prose that lands.

        Your prompt contains:

        CRAFT (how to write):
        - Narrative Structure: Your beat sheet — scene progression and emotional shape
        - Writing Style: Prose craft specific to this story type, including craft posture (density and restraint constraints)

        STORY BIBLE (background reference, not requirements):
        - Tone & Atmosphere: Notes on emotional texture
        - Character Notes: Relationships and history — bring alive through specificity

        WORLD DATA (what to write about):
        - Cast: Characters to bring alive — descriptions show their FINAL state, but you're writing PAST EVENTS when they were alive/active
        - Narrative Lens (optional): A contextual entity that shapes the story without being a character
        - World: Setting context and canon facts
        - Historical Context: Era, timeline, and current conditions that shape what's possible
        - Events: What happened — show these through character experience, don't document them

        CRITICAL: Entity descriptions reflect who characters BECAME. Write them as they WERE during the story's events. A character described as dead was alive when your story takes place.

        Craft defines how to write; Story Bible is background reference. The world exists through what characters notice, do, and feel.
        """;

    /// <summary>
    /// Builds the user prompt by assembling sections in the canonical order.
    /// Order: TASK DATA (Task → Narrative Direction → Structure → Event Usage → Narrative Voice → Entity Directives → Writing Style)
    ///        WORLD DATA (Cast → Narrative Lens → World → Name Bank → Temporal → Data)
    /// </summary>
    public static string BuildUserPrompt(StoryPromptContext ctx)
    {
        var sections = new List<string>();

        // TASK DATA
        sections.Add(BuildTaskSection(ctx));

        var narrativeDirection = PromptSections.BuildNarrativeDirectionSection(ctx.GenerationContext.NarrativeDirection);
        if (!string.IsNullOrEmpty(narrativeDirection))
            sections.Add(narrativeDirection);

        if (ctx.NarrativeInstructions is not null)
            sections.Add($"# Narrative Structure\n{ctx.NarrativeInstructions}");

        if (ctx.EventInstructions is not null)
            sections.Add($"# Event Usage\n{ctx.EventInstructions}");

        var narrativeVoice = PromptSections.BuildNarrativeVoiceSection(ctx.NarrativeVoice);
        if (!string.IsNullOrEmpty(narrativeVoice))
            sections.Add(narrativeVoice);

        var entityDirectives = PromptSections.BuildEntityDirectivesSection(ctx.EntityDirectives);
        if (!string.IsNullOrEmpty(entityDirectives))
            sections.Add(entityDirectives);

        if (ctx.ProseInstructions is not null || ctx.CraftPosture is not null)
        {
            var styleLines = new List<string> { "# Writing Style" };
            if (ctx.ProseInstructions is not null)
                styleLines.Add(ctx.ProseInstructions);
            if (ctx.CraftPosture is not null)
                styleLines.Add($"\nCraft Posture: {ctx.CraftPosture}");
            sections.Add(string.Join("\n", styleLines));
        }

        // WORLD DATA
        var castSection = BuildCastSection(ctx.PrimaryEntities, ctx.SupportingEntities);
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

    private static string BuildTaskSection(StoryPromptContext ctx) =>
        $"""
        # Task
        Write a {ctx.MinWords}-{ctx.MaxWords} word piece of engaging fantasy fiction in {ctx.MinScenes}-{ctx.MaxScenes} distinct scenes.

        Your goal: A story that readers remember — with characters they care about, moments that land, and prose that moves. The world exists through what characters notice and feel, not through explanation.

        Requirements:
        - Follow the plot structure and scene progression
        - Incorporate the listed events as lived moments, not reported history
        - Write directly with no section headers or meta-commentary
        """;

    private static string BuildCastSection(
        IReadOnlyList<EntityContext> primary,
        IReadOnlyList<EntityContext> supporting)
    {
        if (primary.Count == 0 && supporting.Count == 0)
            return string.Empty;

        var lines = new List<string> { "# Cast" };

        if (primary.Count > 0)
        {
            lines.Add("Primary characters:");
            foreach (var e in primary)
            {
                lines.Add("");
                lines.Add(PromptSections.FormatEntityFull(e));
            }
        }

        if (supporting.Count > 0)
        {
            if (primary.Count > 0) lines.Add("");
            lines.Add("Supporting characters:");
            foreach (var e in supporting)
                lines.Add($"- {PromptSections.FormatEntityBrief(e)}");
        }

        return string.Join("\n", lines);
    }
}
