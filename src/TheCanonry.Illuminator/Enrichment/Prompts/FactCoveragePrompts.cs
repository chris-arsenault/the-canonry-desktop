namespace TheCanonry.Illuminator.Enrichment.Prompts;

/// <summary>
/// Prompt builders for the fact coverage analysis task.
/// Prompt text ported from factCoverageTask.ts.
/// </summary>
public static class FactCoveragePrompts
{
    // =========================================================================
    // System prompt
    // =========================================================================

    private const string SystemPromptText =
        "You are a literary analyst assessing how thoroughly a narrative text incorporates specific world-building facts. For each fact, rate its presence in the text.\n\n" +
        "Ratings:\n" +
        "- **missing**: The fact is not referenced, implied, or reflected in the text at all.\n" +
        "- **mentioned**: The fact is briefly touched on, alluded to indirectly, or present only as background assumption. A careful reader might notice it.\n" +
        "- **prevalent**: The fact meaningfully shapes part of the narrative — it influences events, character behavior, or setting details in a visible way.\n" +
        "- **integral**: The fact is central to the narrative. Remove it and the story would not work.\n\n" +
        "Rules:\n" +
        "- Judge based on the narrative content, not what you think should be there.\n" +
        "- A fact can be \"prevalent\" even if the exact wording never appears — what matters is whether the concept drives the text.\n" +
        "- Be precise in your evidence: quote or reference specific passages.\n" +
        "- Return ONLY a JSON array of objects with \"factId\" (string), \"rating\" (string), and \"evidence\" (string, 1 sentence max). No other text.";

    /// <summary>
    /// Returns the system prompt for the fact coverage analysis task.
    /// </summary>
    public static string BuildSystemPrompt() => SystemPromptText;

    // =========================================================================
    // User prompt
    // =========================================================================

    /// <summary>
    /// Builds the user prompt with facts list and narrative text.
    /// </summary>
    public static string BuildUserPrompt(
        IReadOnlyList<(string Id, string Text)> facts,
        string narrativeText)
    {
        var factLines = facts.Select((f, i) => $"[{i + 1}] {f.Id}: {f.Text}");
        return $"=== FACTS TO ASSESS ===\n{string.Join("\n", factLines)}\n\n=== NARRATIVE TEXT ===\n{narrativeText}";
    }
}
