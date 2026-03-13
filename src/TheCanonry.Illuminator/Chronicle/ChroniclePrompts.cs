using TheCanonry.Illuminator.Chronicle.V2;

namespace TheCanonry.Illuminator.Chronicle;

/// <summary>
/// Prompt builders for the multi-step chronicle generation pipeline.
/// </summary>
public static class ChroniclePrompts
{
    /// <summary>
    /// Build the system prompt for chronicle generation.
    /// Dispatches to V2 builders based on format.
    /// </summary>
    public static string BuildGenerationSystemPrompt(ChronicleGenerationContext ctx, string format) =>
        format.ToLowerInvariant() switch
        {
            "story" => StoryPromptBuilder.SystemPrompt,
            "document" => DocumentPromptBuilder.SystemPrompt,
            _ => StoryPromptBuilder.SystemPrompt, // fallback
        };

    /// <summary>
    /// Build the user prompt for chronicle generation.
    /// </summary>
    public static string BuildGenerationUserPrompt(ChronicleGenerationContext ctx)
    {
        var lines = new List<string>();

        if (ctx.FocalEraName is not null)
            lines.Add($"ERA: {ctx.FocalEraName}");

        if (ctx.NarrativeDirection is not null)
            lines.Add($"\nNARRATIVE DIRECTION: {ctx.NarrativeDirection}");

        lines.Add("\nCAST:");
        foreach (var e in ctx.Entities)
        {
            var desc = e.Description is not null ? $" - {e.Description[..Math.Min(200, e.Description.Length)]}..." : "";
            lines.Add($"- {e.Name} ({e.Kind}, {e.Culture}){desc}");
        }

        if (ctx.Relationships.Count > 0)
        {
            lines.Add("\nRELATIONSHIPS:");
            foreach (var r in ctx.Relationships)
                lines.Add($"- {r.SourceName} --[{r.Kind}]--> {r.TargetName}");
        }

        if (ctx.CanonFacts.Count > 0)
        {
            lines.Add("\nCANON FACTS:");
            foreach (var fact in ctx.CanonFacts)
                lines.Add($"- {fact}");
        }

        lines.Add("\nWrite the chronicle.");
        return string.Join('\n', lines);
    }

    /// <summary>
    /// Build the system prompt for chronicle summary generation.
    /// </summary>
    public static string BuildSummarySystemPrompt() =>
        "You are a concise summarizer. Given a narrative text, produce a 1-3 sentence summary capturing the key events and themes. Output only the summary text.";

    /// <summary>
    /// Build the user prompt for chronicle summary generation.
    /// </summary>
    public static string BuildSummaryUserPrompt(string chronicleContent) =>
        $"Summarize the following chronicle:\n\n{chronicleContent}";

    /// <summary>
    /// Build the system prompt for chronicle title generation.
    /// </summary>
    public static string BuildTitleSystemPrompt() =>
        """
        Generate 5 evocative title candidates for the given chronicle. Output a JSON array of strings.
        Titles should be 2-6 words, atmospheric, and hint at the chronicle's themes without spoiling.
        Example: ["The Silence Before Ice", "Under Borrowed Skies", "What the Tide Forgot"]
        """;

    /// <summary>
    /// Build the user prompt for chronicle title generation.
    /// </summary>
    public static string BuildTitleUserPrompt(string chronicleContent, string? summary) =>
        $"SUMMARY: {summary ?? "(none)"}\n\nCHRONICLE:\n{chronicleContent}";

    /// <summary>
    /// Build the system prompt for image reference extraction.
    /// </summary>
    public static string BuildImageRefsSystemPrompt() =>
        """
        You identify image-worthy moments in narratives. For each moment, provide:
        - refId: unique identifier
        - anchorText: exact text phrase near the moment (verbatim from the source)
        - sceneDescription: 1-2 sentence visual description of the scene
        - size: "small", "medium", "large", or "full-width"
        - involvedEntityIds: entity IDs in the scene

        Output JSON: { "refs": [...] }
        Identify 3-6 moments. Prefer dramatic, visual, or emotionally resonant scenes.
        """;

    /// <summary>
    /// Build the user prompt for image reference extraction.
    /// </summary>
    public static string BuildImageRefsUserPrompt(string chronicleContent, IReadOnlyList<EntityContext> entities)
    {
        var entityList = string.Join("\n", entities.Select(e => $"- {e.Id}: {e.Name} ({e.Kind})"));
        return $"ENTITIES:\n{entityList}\n\nCHRONICLE:\n{chronicleContent}";
    }

    // =========================================================================
    // Compare Step
    // =========================================================================

    /// <summary>
    /// Build the system prompt for the compare step.
    /// </summary>
    public static string BuildCompareSystemPrompt(bool isDocument) =>
        isDocument
            ? "You are an editorial reviewer evaluating drafts of an in-universe document. The core question is: which draft feels more like a real artifact from this world? Assess believability, world integration, and documentary craft. A document that maintains consistent voice is doing its job — sustained register is a strength."
            : "You are a narrative editor providing a comparative analysis of chronicle drafts. Be specific and cite examples from the text.";

    /// <summary>
    /// Build the compare user prompt for story or document format.
    /// </summary>
    public static string BuildCompareUserPrompt(
        IReadOnlyList<(string Label, string Content)> versions,
        string narrativeStyleName,
        bool isDocument,
        string? narrativeStyleBlock = null,
        string? worldFactsBlock = null,
        string? narrativeDirection = null)
    {
        var versionsBlock = string.Join("\n\n---\n\n",
            versions.Select(v => $"## {v.Label}\n\n{v.Content}"));

        var styleRef = !string.IsNullOrEmpty(narrativeStyleBlock)
            ? isDocument
                ? $"\n## Document Format Reference\n{narrativeStyleBlock}\n"
                : $"\n## Narrative Style Reference\n{narrativeStyleBlock}\n"
            : "";

        var worldRef = !string.IsNullOrEmpty(worldFactsBlock) ? $"\n{worldFactsBlock}" : "";
        var directionBlock = !string.IsNullOrEmpty(narrativeDirection)
            ? $"\n## Narrative Direction\nThe author specified this narrative purpose: \"{narrativeDirection}\"\nEvaluate how well each version fulfills this specific intent.\n"
            : "";

        var entityLabel = isDocument ? "Document" : "Chronicle";

        return isDocument
            ? BuildDocumentComparePrompt(versions.Count, narrativeStyleName, styleRef, worldRef, directionBlock, versionsBlock)
            : BuildStoryComparePrompt(versions.Count, narrativeStyleName, styleRef, worldRef, directionBlock, versionsBlock);
    }

    private static string BuildStoryComparePrompt(
        int versionCount, string styleName, string styleRef, string worldRef, string directionBlock, string versionsBlock) =>
        $"""
        You are comparing {versionCount} versions of the same chronicle. Each was generated from the same prompt and narrative style ({styleName}) but with different sampling modes (normal vs low).
        {styleRef}{worldRef}{directionBlock}
        Your output must have THREE sections in this exact order. Keep the total output under 800 words.

        ## Comparative Analysis

        Cover each dimension in 2-3 sentences, naming the stronger version with one specific example:

        1. **Prose Quality**: Which has more natural, engaging prose? Where does each feel templated or forced?
        2. **Structural Choices**: How do versions differ in scene ordering, POV, pacing? Which makes more surprising or effective choices?
        3. **World-Building Detail**: Which invents richer names, places, customs, sensory details? Which feels more grounded?
        4. **Factual Utilization**: Review the World Facts above. Which version better integrates these facts — weaving them naturally into narrative rather than listing them? Which shows facts through action and dialogue vs. exposition? Are any facts missing or contradicted?
        5. **Narrative Style Adherence ({styleName})**: Evaluate against the narrative style above — its required structure, prose techniques, and specific instructions. Which better fulfills these? Which better follows entity directives?
        6. **Emotional Range**: Which has more varied emotional registers vs. falling into a single mood?
        7. **Perspective Integration**: Which better integrates the perspective synthesis outputs — narrative voice (synthesized prose guidance), entity directives (per-character writing guidance), and cultural identities? Which feels inhabited vs. described from outside?

        ## Recommendation

        State one of: **Keep Version [X]** (one version is clearly superior), or **Combine** (each version has distinct strengths worth merging). Explain why in 2-3 sentences.

        ## Combine Instructions

        Write a SHORT paragraph (4-6 sentences) of editorial direction for a writer who will combine these versions. This paragraph will be the ONLY guidance they receive — they will not see your analysis above — so it must carry enough context to stand alone. Ground your instructions in the specific findings from your analysis.

        Name which version should serve as the foundation and why — its arc, its scenes, its strongest moments. Then name what the other version does that the foundation draft doesn't: scenes it invented, character beats it handled better, details that enrich the world. These are the things the combined version shouldn't lose.

        Equally important: name what to CUT from the foundation to make room. Every import should displace something weaker, not stack on top. Flag any passages in either version that catalog or list (names, events, effects in sequence), repeat the same dramatic beat in parallel structure, or read as a report of what happened rather than lived experience — these should not survive the combine.

        Don't recommend swapping names or terminology between versions. Trust the writer to make specific decisions — do not prescribe line-by-line changes or integration points.

        ## Chronicle Versions

        {versionsBlock}
        """;

    private static string BuildDocumentComparePrompt(
        int versionCount, string styleName, string styleRef, string worldRef, string directionBlock, string versionsBlock) =>
        $"""
        You are comparing {versionCount} versions of the same in-universe document. Each was generated from the same prompt and document format ({styleName}) but with different sampling modes (normal vs low).
        {styleRef}{worldRef}{directionBlock}
        Your output must have THREE sections in this exact order. Keep the total output under 800 words.

        ## Comparative Analysis

        Cover each dimension in 2-3 sentences, naming the stronger version with one specific example:

        1. **In-Universe Believability**: Which reads more like a real {styleName} that exists within this world? Consider the whole artifact — does it feel like something a person in this setting would produce, handle, or encounter? Material texture (stamps, marginalia, wear) counts, but so does getting the tone and purpose right.
        2. **World Integration**: Review the World Facts above. Which version better absorbs the world's specifics — names, places, factions, customs, tensions — into the document's fabric? Which makes the world feel lived-in rather than referenced? Are any key facts missing or contradicted?
        3. **Voice & Purpose**: Does the document sound like its supposed author? A wanted notice written by a bureaucrat should sound bureaucratic; a folk song collected by a scholar should sound collected. Which version better inhabits its authorial perspective? Note: a document that maintains consistent voice throughout is doing its job — sustained register is a virtue.
        4. **Documentary Craft**: Which makes smarter choices about what to include, emphasize, bury, or omit? Documents communicate through structure and editorial choices — what's foregrounded, what's in footnotes, what's conspicuously absent. Which version shows better editorial intelligence?
        5. **Specificity & Invention**: Which grounds itself in concrete, plausible details (titles, dates, procedures, citations) rather than generic atmosphere or invented backstory? Which stays within what its author would know and record?

        ## Recommendation

        State one of: **Keep Version [X]** (one version is clearly superior), or **Combine** (each version has distinct strengths worth merging). Explain why in 2-3 sentences.

        ## Combine Instructions

        Write a SHORT paragraph (4-6 sentences) of editorial direction for a writer who will combine these versions. This paragraph will be the ONLY guidance they receive — they will not see your analysis above — so it must carry enough context to stand alone. Ground your instructions in the specific findings from your analysis.

        Name which version should serve as the foundation and why — its structure, its information arc, its strongest sections. Then name what the other version does that the foundation draft doesn't: sections it handled better, details it included, structural choices that strengthen the piece. These are the things the combined version shouldn't lose.

        Equally important: name what to CUT from the foundation to make room. Every import should displace something weaker, not stack on top. Flag any passages in either version that catalog or list without purpose, repeat the same information in template form, or pad the document without adding substance — these should not survive the combine.

        Don't recommend swapping names or terminology between versions. Trust the writer to make specific decisions — do not prescribe line-by-line changes.

        ## Document Versions

        {versionsBlock}
        """;

    // =========================================================================
    // Combine Step
    // =========================================================================

    /// <summary>
    /// Build the system prompt for the combine step.
    /// </summary>
    public static string BuildCombineSystemPrompt(bool isDocument) =>
        isDocument
            ? "You are an editorial reviewer producing the definitive version of an in-universe document from multiple drafts. The result should feel like a real artifact from this world. Maintain consistent voice. Output only the final document text."
            : "You are a narrative editor producing the definitive version of a chronicle from multiple drafts. Output only the final chronicle text.";

    /// <summary>
    /// Build the combine user prompt for story or document format.
    /// </summary>
    public static string BuildCombineUserPrompt(
        IReadOnlyList<(string Label, string Content)> versions,
        string narrativeStyleName,
        bool isDocument,
        string generationSystemPrompt,
        string? craftPosture = null,
        string? narrativeDirection = null,
        string? combineInstructions = null)
    {
        var versionsBlock = string.Join("\n\n---\n\n",
            versions.Select(v => $"## {v.Label}\n\n{v.Content}"));

        var directionBlock = !string.IsNullOrEmpty(narrativeDirection)
            ? $"\n## Narrative Direction\nThe author specified this narrative purpose: \"{narrativeDirection}\"\nThe combined version must fulfill this intent.\n"
            : "";

        var craftPostureBlock = !string.IsNullOrEmpty(craftPosture)
            ? $"\n\n## Craft Posture\nDensity and restraint constraints for this format:\n{craftPosture}"
            : "";

        return isDocument
            ? BuildDocumentCombinePrompt(versions.Count, narrativeStyleName, versionsBlock, generationSystemPrompt, craftPostureBlock, directionBlock, combineInstructions)
            : BuildStoryCombinePrompt(versions.Count, narrativeStyleName, versionsBlock, generationSystemPrompt, craftPostureBlock, directionBlock, combineInstructions);
    }

    private static string BuildStoryCombinePrompt(
        int versionCount, string styleName, string versionsBlock, string systemPrompt,
        string craftPostureBlock, string directionBlock, string? combineInstructions)
    {
        var criteriaBlock = !string.IsNullOrEmpty(combineInstructions)
            ? $"\n## Editorial Direction\n\n{combineInstructions}\n"
            : $"""

            ## Selection Criteria

            Prefer whichever version has:
            - **Richer world-building details** — invented names, places, customs that feel grounded
            - **More surprising structural choices** — non-obvious scene ordering, interesting POV decisions
            - **More natural prose** — sentences that flow without feeling templated
            - **Better adherence to the narrative voice and entity directives**
            - **Stronger emotional range** — not every beat should feel the same

            If one version has a better opening and another has a better middle, use each. If versions handle the same beat differently, pick the one that reads more naturally.
            """;

        return $"""
        You are a narrative editor combining {versionCount} versions of the same chronicle into a single final version. All versions follow the same prompt and narrative style — they differ in sampling mode (normal vs low).

        You have two drafts of the same story. Read both. Build your revision from the ground up: which version has the stronger opening? Which handles the climax better? Which invented scenes, character beats, or details the other missed? Take the best from each. Where they cover the same ground differently, go with whichever makes the better story. Where one draft has something the other lacks entirely, bring it in.

        Do not swap names or terminology between versions — keep each draft's choices consistent within the sections you draw from. A polish pass will follow to smooth voice and tone; your job is to produce the best possible version of this story.
        {criteriaBlock}{directionBlock}
        ## Original System Prompt Context
        {systemPrompt}

        Style: {styleName}{craftPostureBlock}

        ## Chronicle Versions

        {versionsBlock}

        ## YOUR TASK

        Produce the final chronicle by selecting the strongest elements from each version. Output ONLY the chronicle text — no commentary, no labels, no preamble.
        """;
    }

    private static string BuildDocumentCombinePrompt(
        int versionCount, string styleName, string versionsBlock, string systemPrompt,
        string craftPostureBlock, string directionBlock, string? combineInstructions)
    {
        var criteriaBlock = !string.IsNullOrEmpty(combineInstructions)
            ? $"\n## Editorial Direction\n\n{combineInstructions}\n"
            : $"""

            ## Selection Criteria

            Prefer whichever version:
            - **Feels more like a real {styleName}** — could this artifact exist in this world? Does it read like something its supposed author would produce?
            - **Better integrates the world** — names, factions, customs, tensions woven into the document's fabric rather than referenced from outside
            - **Inhabits its author's voice** — consistent register that matches who wrote this and why
            - **Shows stronger editorial intelligence** — smart choices about what to foreground, bury, or omit
            - **Grounds itself in specifics** — concrete details (titles, dates, procedures) over generic atmosphere

            If one version has a better opening and another has better closing sections, use each. If versions handle the same section differently, pick the one that feels more like it belongs in this world.
            """;

        return $"""
        You are an editorial reviewer combining {versionCount} versions of the same in-universe document into a single final version. All versions follow the same prompt and document format — they differ in sampling mode (normal vs low).

        You have two drafts of the same in-universe document. Read both. Build your revision from the ground up: which version has the stronger opening? Which handles each section better? Which included details, structure, or framing the other missed? Take the best from each. Where they cover the same ground differently, go with whichever makes the document feel more like a real artifact from its world. Where one draft has something the other lacks entirely, bring it in.

        Do not swap names or terminology between versions — keep each draft's choices consistent within the sections you draw from. A polish pass will follow to smooth voice and register; your job is to produce the best possible version of this document.
        {criteriaBlock}{directionBlock}
        ## Original System Prompt Context
        {systemPrompt}

        Style: {styleName}{craftPostureBlock}

        ## Document Versions

        {versionsBlock}

        ## YOUR TASK

        Produce the final document by selecting the strongest elements from each version. The result should feel like a real artifact from this world. Output ONLY the document text — no commentary, no labels, no preamble.
        """;
    }
}
