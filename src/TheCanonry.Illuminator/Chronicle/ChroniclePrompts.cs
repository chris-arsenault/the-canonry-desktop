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
            "story" => StoryPromptBuilder.GetSystemPrompt(),
            "document" => DocumentPromptBuilder.GetSystemPrompt(),
            _ => StoryPromptBuilder.GetSystemPrompt(), // fallback
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

    /// <summary>
    /// Build the system prompt for the compare step.
    /// </summary>
    public static string BuildCompareSystemPrompt() =>
        """
        You compare multiple drafts of the same chronicle. Analyze:
        1. Voice consistency
        2. Narrative strength
        3. Factual accuracy
        4. Unique elements worth preserving from each

        Output a structured comparative report. Do not produce a new draft.
        """;

    /// <summary>
    /// Build the system prompt for the combine step.
    /// </summary>
    public static string BuildCombineSystemPrompt() =>
        """
        You synthesize multiple chronicle drafts into one superior version.
        Combine the strongest elements of each draft while maintaining voice consistency.
        Output only the combined chronicle text.
        """;
}
