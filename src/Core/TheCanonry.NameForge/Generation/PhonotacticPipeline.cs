using TheCanonry.NameForge.Types;
using TheCanonry.NameForge.Utils;

namespace TheCanonry.NameForge.Generation;

/// <summary>
/// Debug information from the phonotactic pipeline.
/// </summary>
public sealed record PipelineDebug(
    string RawWord,
    string[] Syllables,
    string[] Templates,
    string MorphedWord,
    string[] MorphedSyllables,
    string MorphologyStructure,
    string[] MorphologyParts,
    string[] StyleTransforms);

/// <summary>
/// Result of the phonotactic pipeline.
/// </summary>
public sealed record PipelineResult(string Name, PipelineDebug Debug);

/// <summary>
/// Phonotactic name generation pipeline: phonology -> morphology -> style.
/// Single source of truth for generating names from a domain.
/// </summary>
public static class PhonotacticPipeline
{
    /// <summary>
    /// Execute the full phonotactic name generation pipeline.
    /// </summary>
    public static PipelineResult Execute(
        SeededRng rng,
        NamingDomain domain,
        int morphologyCandidates = 3,
        int maxMorphologyLength = 20)
    {
        // Phase 1: Generate phonological base with syllables
        var wordDebug = Phonology.GenerateWordWithDebug(rng, domain.Phonology);

        // Phase 2: Apply morphology (if configured), tracking syllables through
        var morphedWord = wordDebug.Word;
        var morphedSyllables = wordDebug.Syllables;
        var morphologyStructure = "root";
        var morphologyParts = new[] { $"root:{wordDebug.Word}" };

        if (Morphology.CanApplyMorphology(domain.Morphology))
        {
            var morphed = Morphology.ApplyMorphologyBest(
                rng,
                wordDebug.Word,
                domain.Morphology,
                morphologyCandidates,
                maxMorphologyLength,
                wordDebug.Syllables);

            morphedWord = morphed.Result;
            morphedSyllables = morphed.Syllables;
            morphologyStructure = morphed.Structure;
            morphologyParts = morphed.Parts;
        }

        // Phase 3: Apply style transforms with correct syllable boundaries
        var styleResult = Style.ApplyStyle(rng, morphedWord, domain.Style, morphedSyllables);

        var debug = new PipelineDebug(
            RawWord: wordDebug.Word,
            Syllables: wordDebug.Syllables,
            Templates: wordDebug.Templates,
            MorphedWord: morphedWord,
            MorphedSyllables: morphedSyllables,
            MorphologyStructure: morphologyStructure,
            MorphologyParts: morphologyParts,
            StyleTransforms: styleResult.Transforms);

        return new PipelineResult(styleResult.Result, debug);
    }

    /// <summary>
    /// Simple wrapper that returns just the name string.
    /// </summary>
    public static string GeneratePhonotacticName(SeededRng rng, NamingDomain domain)
    {
        return Execute(rng, domain).Name;
    }
}
