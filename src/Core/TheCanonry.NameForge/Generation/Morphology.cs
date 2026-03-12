using TheCanonry.NameForge.Types;
using TheCanonry.NameForge.Utils;

namespace TheCanonry.NameForge.Generation;

/// <summary>
/// Result of morphology application.
/// </summary>
public sealed record MorphologyResult(
    string Result,
    string Structure,
    string[] Parts,
    string[] Syllables);

/// <summary>
/// Morphology: applies morphological structure to base words.
/// </summary>
public static class Morphology
{
    /// <summary>
    /// Apply morphological structure to a base word (root).
    /// </summary>
    public static MorphologyResult ApplyMorphology(
        SeededRng rng,
        string root,
        MorphologyProfile profile,
        string[] rootSyllables)
    {
        var structure = rng.PickWeighted(profile.Structure, profile.StructureWeights);
        var result = "";
        var parts = new List<string>();
        var syllables = new List<string>();
        var tokens = structure.Split('-');

        foreach (var token in tokens)
        {
            switch (token)
            {
                case "root":
                    result += root;
                    parts.Add($"root:{root}");
                    if (rootSyllables.Length > 0)
                        syllables.AddRange(rootSyllables);
                    else
                        syllables.Add(root);
                    break;

                case "prefix":
                    if (profile.Prefixes.Length == 0) break;
                    var prefix = rng.PickWeighted(profile.Prefixes, profile.PrefixWeights);
                    result += prefix;
                    parts.Add($"prefix:{prefix}");
                    syllables.Add(prefix);
                    break;

                case "suffix":
                    if (profile.Suffixes.Length == 0) break;
                    var suffix = rng.PickWeighted(profile.Suffixes, profile.SuffixWeights);
                    result += suffix;
                    parts.Add($"suffix:{suffix}");
                    syllables.Add(suffix);
                    break;

                case "infix":
                    if (profile.Infixes.Length == 0 || root.Length <= 2) break;
                    var infix = rng.PickRandom(profile.Infixes);
                    var mid = root.Length / 2;
                    result = result[..^root.Length] + root[..mid] + infix + root[mid..];
                    parts.Add($"infix:{infix}");
                    break;

                case "wordroot":
                    if (profile.WordRoots.Length > 0)
                    {
                        var wordRoot = rng.PickRandom(profile.WordRoots);
                        result += wordRoot;
                        parts.Add($"wordroot:{wordRoot}");
                        syllables.Add(wordRoot);
                    }
                    else
                    {
                        result += root;
                        parts.Add($"root:{root}");
                        if (rootSyllables.Length > 0)
                            syllables.AddRange(rootSyllables);
                        else
                            syllables.Add(root);
                    }
                    break;

                case "honorific":
                    if (profile.Honorifics.Length == 0) break;
                    var honorific = rng.PickRandom(profile.Honorifics);
                    result = honorific + " " + result;
                    parts.Add($"honorific:{honorific}");
                    break;
            }
        }

        return new MorphologyResult(result, structure, [.. parts], [.. syllables]);
    }

    /// <summary>
    /// Apply morphology with multiple candidates and pick the best one.
    /// Useful for avoiding overly long or awkward combinations.
    /// </summary>
    public static MorphologyResult ApplyMorphologyBest(
        SeededRng rng,
        string root,
        MorphologyProfile profile,
        int candidateCount = 3,
        int maxLength = 20,
        string[]? rootSyllables = null)
    {
        rootSyllables ??= [root];

        var candidates = new List<(MorphologyResult Result, double Score)>();

        for (var i = 0; i < candidateCount; i++)
        {
            var morphed = ApplyMorphology(rng, root, profile, rootSyllables);

            var score = 1.0;
            if (morphed.Result.Length > maxLength) score *= 0.5;
            if (morphed.Result.Length < 3) score *= 0.5;

            candidates.Add((morphed, score));
        }

        var scores = candidates.Select(c => c.Score).ToArray();
        var totalScore = scores.Sum();

        var r = rng.NextDouble() * totalScore;
        var cumulative = 0.0;
        foreach (var (result, score) in candidates)
        {
            cumulative += score;
            if (r < cumulative)
                return result;
        }

        return candidates[0].Result;
    }

    /// <summary>
    /// Check if a morphology profile can actually modify names
    /// (i.e., has at least some affixes or structures beyond "root").
    /// </summary>
    public static bool CanApplyMorphology(MorphologyProfile profile)
    {
        var hasComplexStructures = profile.Structure.Any(
            s => s != "root" && s.Contains('-'));

        if (!hasComplexStructures) return false;

        var hasAffixes =
            profile.Prefixes.Length > 0 ||
            profile.Suffixes.Length > 0 ||
            profile.Infixes.Length > 0 ||
            profile.WordRoots.Length > 0;

        return hasAffixes;
    }

    /// <summary>
    /// Generate a compound name (root + separator + root).
    /// </summary>
    public static string GenerateCompound(string root1, string root2, string separator = "")
    {
        return root1 + separator + root2;
    }

    /// <summary>
    /// Apply an honorific prefix to a name.
    /// </summary>
    public static string ApplyHonorific(SeededRng rng, string name, MorphologyProfile profile)
    {
        if (profile.Honorifics.Length == 0) return name;
        var honorific = rng.PickRandom(profile.Honorifics);
        return $"{honorific} {name}";
    }
}
