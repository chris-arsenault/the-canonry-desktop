using TheCanonry.Illuminator.Chronicle.PerspectiveSynthesis;

namespace TheCanonry.Illuminator.Chronicle.V2;

/// <summary>
/// Input context for document prompt building.
/// </summary>
public sealed record DocumentPromptContext
{
    public required ChronicleGenerationContext GenerationContext { get; init; }
    public required IReadOnlyList<EntityContext> PrimaryEntities { get; init; }
    public required IReadOnlyList<EntityContext> SupportingEntities { get; init; }
    public IReadOnlyDictionary<string, string>? NarrativeVoice { get; init; }
    public IReadOnlyList<EntityDirective>? EntityDirectives { get; init; }
    public string? TemporalNarrative { get; init; }
    public string? DocumentInstructions { get; init; }
    public string? EventInstructions { get; init; }
    public string? CraftPosture { get; init; }
    public string StyleName { get; init; } = "document";
    public int MinWords { get; init; } = 400;
    public int MaxWords { get; init; } = 600;
    public EntityContext? LensEntity { get; init; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? NameBank { get; init; }
}

/// <summary>
/// Builds V2 document-format chronicle prompts.
/// </summary>
public static class DocumentPromptBuilder
{
    /// <summary>
    /// Returns the system prompt for document-format chronicle generation.
    /// </summary>
    public static string SystemPrompt =>
        """
        You are crafting an in-universe document that feels authentic and alive. Your prompt contains:

        CRAFT (how to write):
        - Document Instructions: Structure, voice, tone - THIS DEFINES YOUR DOCUMENT
        - Perspective: This chronicle's thematic angle and suggested motifs

        STORY BIBLE (background reference):
        - Tone & Atmosphere: Notes on emotional texture
        - Character Notes: Relationships and history

        WORLD DATA (what to write about):
        - Cast: Characters referenced — descriptions show their FINAL state, but the document may depict PAST EVENTS when they were alive/active
        - Narrative Lens (optional): A contextual entity that shapes the document's assumptions
        - World: Setting context and canon facts
        - Historical Context: Era, timeline, and current conditions that shape what's possible
        - Events: What happened — reference naturally, don't list

        CRITICAL: Entity descriptions reflect who characters BECAME. If depicting past events, write them as they WERE during those events.

        Document Instructions define the document's structure and format — they are primary. The Perspective provides thematic focus, not prose style. Write as the document's author would write, not as a storyteller.

        Write authentically as if the document exists within the world. No meta-commentary.
        """;

    /// <summary>
    /// Builds the user prompt by assembling sections in canonical order.
    /// Order: TASK DATA (Task → Narrative Direction → Document Instructions → Event Usage → Narrative Voice → Entity Directives → Craft Posture)
    ///        WORLD DATA (Cast → Narrative Lens → World → Name Bank → Temporal → Data)
    /// </summary>
    public static string BuildUserPrompt(DocumentPromptContext ctx)
    {
        var sections = new List<string>();

        // TASK DATA
        sections.Add(BuildTaskSection(ctx));

        var narrativeDirection = PromptSections.BuildNarrativeDirectionSection(ctx.GenerationContext.NarrativeDirection);
        if (!string.IsNullOrEmpty(narrativeDirection))
            sections.Add(narrativeDirection);

        if (ctx.DocumentInstructions is not null)
            sections.Add($"# Document Instructions\n{ctx.DocumentInstructions}");

        if (ctx.EventInstructions is not null)
            sections.Add($"# How to Use Events\n{ctx.EventInstructions}");

        var narrativeVoice = PromptSections.BuildNarrativeVoiceSection(ctx.NarrativeVoice);
        if (!string.IsNullOrEmpty(narrativeVoice))
            sections.Add(narrativeVoice);

        var entityDirectives = PromptSections.BuildEntityDirectivesSection(ctx.EntityDirectives);
        if (!string.IsNullOrEmpty(entityDirectives))
            sections.Add(entityDirectives);

        // Perspective section from world tone (document format uses this instead of Writing Style)
        var tone = ctx.GenerationContext.Tone;
        if (!string.IsNullOrEmpty(tone))
            sections.Add($"# Perspective\n{tone}");

        if (ctx.CraftPosture is not null)
            sections.Add($"# Craft Posture\nHow to relate to the material — density, withholding, and elaboration:\n{ctx.CraftPosture}");

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

    private static string BuildTaskSection(DocumentPromptContext ctx) =>
        $"""
        # Task
        Write a {ctx.MinWords}-{ctx.MaxWords} word {ctx.StyleName}.

        Requirements:
        - Assign the provided characters to the narrative roles defined below
        - Follow the document structure and voice guidance below
        - Incorporate the listed events naturally as context or content
        - Use the research notes as inspiration for how characters relate
        - Ground the document in the historical era and timeline provided
        - Make the document feel authentic - as if it exists within the world
        - Write the document directly with no meta-commentary
        """;

    private static string BuildCastSection(
        IReadOnlyList<EntityContext> primary,
        IReadOnlyList<EntityContext> supporting)
    {
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
