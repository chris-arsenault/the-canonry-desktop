namespace TheCanonry.Illuminator.Enrichment.Prompts;

/// <summary>
/// Prompt builders for the historian tone ranking task.
/// Prompt text ported from toneRankingTask.ts.
/// </summary>
public static class ToneRankingPrompts
{
    // =========================================================================
    // System prompt
    // =========================================================================

    private const string SystemPromptText =
        "You will receive a chronicle summary. Determine which historian annotation tone will resonate best with the material.\n\n" +
        "For each of the 7 tones below, answer its question about the chronicle:\n\n" +
        "- **witty**: Is this text funnier than it realizes?\n" +
        "- **weary**: Have I read this story before in different names?\n" +
        "- **elegiac**: Is something specific gone forever?\n" +
        "- **cantankerous**: Is the text wrong about something?\n" +
        "- **rueful**: Is someone making a mistake they can't see?\n" +
        "- **conspiratorial**: Is the text hiding something specific?\n" +
        "- **bemused**: Is this genuinely weird?\n\n" +
        "Rank the 3 tones that get the strongest \"yes.\" For each, state what in the text answers that question.\n\n" +
        "You must respond with ONLY a JSON object in this exact format, no other text:\n" +
        "{ \"ranking\": [\"tone1\", \"tone2\", \"tone3\"], \"rationales\": { \"tone1\": \"answer...\", \"tone2\": \"answer...\", \"tone3\": \"answer...\" } }";

    /// <summary>
    /// Returns the system prompt for the tone ranking task.
    /// </summary>
    public static string BuildSystemPrompt() => SystemPromptText;

    // =========================================================================
    // User prompt
    // =========================================================================

    /// <summary>
    /// Builds the user prompt for tone ranking from chronicle metadata.
    /// </summary>
    public static string BuildUserPrompt(
        string summary,
        string format,
        string? narrativeStyleName = null,
        string? brief = null)
    {
        var lines = new List<string>
        {
            "Evaluate this chronicle:",
            "",
            $"Format: {format}"
        };

        if (!string.IsNullOrEmpty(narrativeStyleName))
            lines.Add($"Style: {narrativeStyleName}");

        lines.Add($"Summary: {summary}");

        if (!string.IsNullOrEmpty(brief))
            lines.Add($"Perspective brief: {brief}");

        return string.Join("\n", lines);
    }
}
