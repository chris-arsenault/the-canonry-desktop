namespace TheCanonry.Illuminator.BulkOps;

/// <summary>
/// Configuration for generating a bulk era narrative.
/// </summary>
public sealed record BulkEraNarrativeConfig(
    string EraId,
    string EraName,
    string Tone,
    int TargetWordCountMin,
    int TargetWordCountMax);

/// <summary>
/// Bulk era narrative generation helpers.
///
/// Ported from apps/illuminator/webui/src/components/BulkEraNarrativeModal.jsx.
///
/// Supports 8 tones and a 3-step pipeline: threads → generate → edit.
/// Target length: 5,000–7,000 words.
/// </summary>
public static class BulkEraNarrative
{
    /// <summary>
    /// Available tones for era narrative generation.
    /// </summary>
    public static readonly string[] AvailableTones =
        ["witty", "cantankerous", "bemused", "defiant", "sardonic", "tender", "hopeful", "enthusiastic"];

    /// <summary>
    /// Default word count range for era narratives.
    /// </summary>
    public const int DefaultMinWords = 5000;

    /// <summary>
    /// Default maximum word count for era narratives.
    /// </summary>
    public const int DefaultMaxWords = 7000;

    /// <summary>
    /// Pipeline step names for era narrative generation.
    /// </summary>
    public static readonly string[] PipelineSteps = ["threads", "generate", "edit"];

    /// <summary>
    /// Build a default config for era narrative generation with the given tone.
    /// Validates that the tone is one of the available tones.
    /// </summary>
    public static BulkEraNarrativeConfig CreateConfig(
        string eraId,
        string eraName,
        string tone)
    {
        if (!AvailableTones.Contains(tone, StringComparer.Ordinal))
            throw new ArgumentException($"Tone '{tone}' is not valid. Available: {string.Join(", ", AvailableTones)}", nameof(tone));

        return new BulkEraNarrativeConfig(
            EraId: eraId,
            EraName: eraName,
            Tone: tone,
            TargetWordCountMin: DefaultMinWords,
            TargetWordCountMax: DefaultMaxWords);
    }
}
