using TheCanonry.NameForge.Types;
using TheCanonry.NameForge.Utils;

namespace TheCanonry.NameForge.Generation;

/// <summary>
/// Syllable generation result.
/// </summary>
public sealed record SyllableResult(string Syllable, string Template);

/// <summary>
/// Word generation result with debug info.
/// </summary>
public sealed record WordDebugResult(string Word, string[] Syllables, string[] Templates);

/// <summary>
/// Phonology: generates syllables and words from a phonology profile.
/// </summary>
public static class Phonology
{
    /// <summary>
    /// Generate a single syllable from a phonology profile.
    /// Template expansion: C -> random consonant, V -> random vowel, other -> literal.
    /// </summary>
    public static SyllableResult GenerateSyllable(SeededRng rng, PhonologyProfile profile)
    {
        var template = rng.PickWeighted(profile.SyllableTemplates, profile.TemplateWeights);

        var syllable = new System.Text.StringBuilder();
        foreach (var symbol in template)
        {
            if (symbol == 'C')
            {
                var consonant = rng.PickWeighted(profile.Consonants, profile.ConsonantWeights);
                syllable.Append(consonant);
            }
            else if (symbol == 'V')
            {
                var vowel = rng.PickWeighted(profile.Vowels, profile.VowelWeights);
                syllable.Append(vowel);
            }
            else
            {
                syllable.Append(symbol);
            }
        }

        return new SyllableResult(syllable.ToString(), template);
    }

    /// <summary>
    /// Generate a multi-syllable word from a phonology profile.
    /// Rejects words containing forbidden clusters.
    /// </summary>
    public static string GenerateWord(SeededRng rng, PhonologyProfile profile, int maxAttempts = 50)
    {
        var minLength = profile.LengthRange[0];
        var maxLength = profile.LengthRange[1];
        var syllableCount = rng.NextInt(minLength, maxLength);

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var word = "";
            var valid = true;

            for (var i = 0; i < syllableCount; i++)
            {
                var result = GenerateSyllable(rng, profile);

                if (WouldCreateForbiddenCluster(word, result.Syllable, profile.ForbiddenClusters))
                {
                    valid = false;
                    break;
                }

                word += result.Syllable;
            }

            if (!valid) continue;

            if (profile.ForbiddenClusters.Length > 0 &&
                StringHelpers.HasForbiddenCluster(word, profile.ForbiddenClusters))
                continue;

            return word;
        }

        // Fallback: single syllable
        return GenerateSyllable(rng, profile).Syllable;
    }

    /// <summary>
    /// Generate multiple word candidates and pick the best one based on favored clusters.
    /// Uses best-of-N selection with favored cluster score boost.
    /// </summary>
    public static string GenerateWordWithFavoredClusters(
        SeededRng rng,
        PhonologyProfile profile,
        int candidateCount = 5)
    {
        if (profile.FavoredClusters.Length == 0)
            return GenerateWord(rng, profile);

        var candidates = new string[candidateCount];
        for (var i = 0; i < candidateCount; i++)
            candidates[i] = GenerateWord(rng, profile);

        var boost = profile.FavoredClusterBoost;
        var scores = new double[candidateCount];
        for (var i = 0; i < candidateCount; i++)
        {
            scores[i] = StringHelpers.HasFavoredCluster(candidates[i], profile.FavoredClusters)
                ? boost
                : 1.0;
        }

        return rng.PickWeighted(candidates, scores);
    }

    /// <summary>
    /// Generate a word with full debug info (syllables and templates).
    /// </summary>
    public static WordDebugResult GenerateWordWithDebug(SeededRng rng, PhonologyProfile profile)
    {
        var minLength = profile.LengthRange[0];
        var maxLength = profile.LengthRange[1];
        var syllableCount = rng.NextInt(minLength, maxLength);
        const int maxAttempts = 50;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var syllables = new List<string>();
            var templates = new List<string>();
            var word = "";
            var valid = true;

            for (var i = 0; i < syllableCount; i++)
            {
                var result = GenerateSyllable(rng, profile);

                if (WouldCreateForbiddenCluster(word, result.Syllable, profile.ForbiddenClusters))
                {
                    valid = false;
                    break;
                }

                syllables.Add(result.Syllable);
                templates.Add(result.Template);
                word += result.Syllable;
            }

            if (!valid) continue;

            if (profile.ForbiddenClusters.Length > 0 &&
                StringHelpers.HasForbiddenCluster(word, profile.ForbiddenClusters))
                continue;

            return new WordDebugResult(word, [.. syllables], [.. templates]);
        }

        // Fallback
        var fallback = GenerateSyllable(rng, profile);
        return new WordDebugResult(fallback.Syllable, [fallback.Syllable], [fallback.Template]);
    }

    private static bool WouldCreateForbiddenCluster(
        string currentWord,
        string newSyllable,
        string[] forbiddenClusters)
    {
        if (forbiddenClusters.Length == 0) return false;
        var testWord = currentWord + newSyllable;
        return StringHelpers.HasForbiddenCluster(testWord, forbiddenClusters);
    }
}
