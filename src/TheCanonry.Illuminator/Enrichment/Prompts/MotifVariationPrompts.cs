namespace TheCanonry.Illuminator.Enrichment.Prompts;

/// <summary>
/// Prompt builders for the motif variation task (vary and weave modes).
/// Prompt text ported from motifVariationTask.ts.
/// </summary>
public static class MotifVariationPrompts
{
    // =========================================================================
    // Vary mode — rewrite overused annotation phrases
    // =========================================================================

    /// <summary>
    /// Builds the system prompt for vary mode: rewriting overused phrases in annotations.
    /// </summary>
    public static string BuildVarySystemPrompt(string motifLabel) =>
        $"You are a copy-editor revising a historian's encyclopedia margin annotations. The historian — a frost-scarred archivist working alone in Foundation Depths ice — has overused the phrase \"{motifLabel}\" across many entries. You receive the FULL annotation text for each instance so you can hear the historian's voice and understand the rhetorical flow.\n\n" +
        $"Your job: rewrite ONLY the clause or sentence containing \"{motifLabel}\" so the phrase is gone but the meaning, rhythm, and voice survive. The rest of the annotation must remain unchanged.\n\n" +
        "Rules:\n" +
        $"- Return the FULL annotation text with only the clause around \"{motifLabel}\" rewritten. Every other word stays exactly as written.\n" +
        "- The rewrite must read as if the historian wrote it this way originally. Match his cadence, his precision, his habit of self-interruption.\n" +
        "- Do NOT absorb, merge, or duplicate content from surrounding sentences into the rewritten clause. The adjacent sentences are staying — do not repeat them.\n" +
        "- Do NOT use generic duration substitutes (\"enough time\", \"my tenure\", \"my career\"). These are bureaucratic; the historian is not.\n" +
        "- Vary aggressively — never reuse the same approach across instances.\n" +
        "- Return ONLY a JSON array of objects with \"index\" (number) and \"variant\" (string), where variant is the full rewritten annotation. No other text.";

    /// <summary>
    /// Represents a single annotation instance for vary mode.
    /// </summary>
    public record MotifInstance(int Index, string EntityName, string AnnotationText, string MatchedPhrase);

    /// <summary>
    /// Builds the user prompt for vary mode from annotation instances.
    /// </summary>
    public static string BuildVaryUserPrompt(IReadOnlyList<MotifInstance> instances)
    {
        var lines = instances.Select(inst =>
            $"[{inst.Index}] Entity: {inst.EntityName}\nPhrase to replace: \"{inst.MatchedPhrase}\"\nFull annotation:\n{inst.AnnotationText}");
        return string.Join("\n\n---\n\n", lines);
    }

    // =========================================================================
    // Weave mode — incorporate target phrase into sentences
    // =========================================================================

    /// <summary>
    /// Builds the system prompt for weave mode: incorporating a motif phrase into existing sentences.
    /// </summary>
    public static string BuildWeaveSystemPrompt(string targetPhrase) =>
        $"You are an editor reintroducing a thematic motif into a historian's encyclopedia entries. The phrase \"{targetPhrase}\" is the title motif of this work. It was editorially stripped during a revision pass and needs to be woven back into select sentences.\n\n" +
        "Rules:\n" +
        $"- Rewrite each sentence to naturally incorporate the exact phrase \"{targetPhrase}\".\n" +
        "- The phrase should feel organic, not forced. If it reads as inserted, you have failed.\n" +
        "- Preserve the historian's voice: weary, precise, scholarly.\n" +
        "- Preserve the factual content of the original sentence.\n" +
        "- Keep roughly the same sentence length.\n" +
        "- Return ONLY a JSON array of objects with \"index\" (number) and \"variant\" (string). No other text.";

    /// <summary>
    /// Represents a single sentence instance for weave mode.
    /// </summary>
    public record MotifWeaveInstance(int Index, string EntityName, string Sentence, string SurroundingContext);

    /// <summary>
    /// Builds the user prompt for weave mode from sentence instances.
    /// </summary>
    public static string BuildWeaveUserPrompt(IReadOnlyList<MotifWeaveInstance> instances)
    {
        const int MaxContextLength = 300;
        var lines = instances.Select(inst =>
        {
            var ctx = inst.SurroundingContext.Length > MaxContextLength
                ? inst.SurroundingContext[..MaxContextLength] + "…"
                : inst.SurroundingContext;
            return $"[{inst.Index}] Entity: {inst.EntityName}\nSentence: \"{inst.Sentence}\"\nSurrounding context: {ctx}";
        });
        return string.Join("\n\n", lines);
    }
}
