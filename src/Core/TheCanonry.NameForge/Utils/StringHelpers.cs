using System.Text.RegularExpressions;
using TheCanonry.NameForge.Types;

namespace TheCanonry.NameForge.Utils;

/// <summary>
/// String manipulation helpers for name generation.
/// </summary>
public static partial class StringHelpers
{
    /// <summary>
    /// Capitalize the first letter of a string.
    /// </summary>
    public static string Capitalize(string str)
    {
        if (str.Length == 0) return str;
        return char.ToUpperInvariant(str[0]) + str[1..];
    }

    /// <summary>
    /// Capitalize the first letter of each word.
    /// </summary>
    public static string CapitalizeWords(string str)
    {
        var parts = WhitespaceSplitRegex().Split(str);
        for (var i = 0; i < parts.Length; i++)
        {
            if (WhitespaceOnlyRegex().IsMatch(parts[i])) continue;
            parts[i] = Capitalize(parts[i].ToLowerInvariant());
        }
        return string.Join("", parts);
    }

    /// <summary>
    /// Alternating capitalization (e.g., "test name" -> "TeSt NaMe").
    /// </summary>
    public static string MixedCase(string str)
    {
        var letterIndex = 0;
        var chars = str.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (char.IsLetter(chars[i]))
            {
                chars[i] = letterIndex % 2 == 0
                    ? char.ToUpperInvariant(chars[i])
                    : char.ToLowerInvariant(chars[i]);
                letterIndex++;
            }
        }
        return new string(chars);
    }

    /// <summary>
    /// Apply a capitalization style to a string.
    /// </summary>
    public static string ApplyCapitalization(string str, Capitalization style) => style switch
    {
        Capitalization.Title => Capitalize(str.ToLowerInvariant()),
        Capitalization.TitleWords => CapitalizeWords(str),
        Capitalization.AllCaps => str.ToUpperInvariant(),
        Capitalization.Lowercase => str.ToLowerInvariant(),
        Capitalization.Mixed => MixedCase(str),
        _ => str
    };

    /// <summary>
    /// Find legal positions to insert apostrophes or hyphens
    /// (indices between syllables).
    /// </summary>
    public static int[] FindSyllableBoundaries(string word, IReadOnlyList<string> syllables)
    {
        _ = word; // word is used for context in TS; boundaries come from syllable lengths
        var boundaries = new List<int>();
        var position = 0;
        for (var i = 0; i < syllables.Count - 1; i++)
        {
            position += syllables[i].Length;
            boundaries.Add(position);
        }
        return [.. boundaries];
    }

    /// <summary>
    /// Insert a character at a random syllable boundary.
    /// </summary>
    public static string InsertAtBoundary(string word, char ch, int[] boundaries, SeededRng rng)
    {
        if (boundaries.Length == 0) return word;
        var index = boundaries[rng.NextInt(0, boundaries.Length - 1)];
        return string.Concat(word.AsSpan(0, index), ch.ToString(), word.AsSpan(index));
    }

    /// <summary>
    /// Check if a name contains any forbidden clusters.
    /// </summary>
    public static bool HasForbiddenCluster(string name, IReadOnlyList<string> forbiddenClusters)
    {
        var lower = name.ToLowerInvariant();
        for (var i = 0; i < forbiddenClusters.Count; i++)
        {
            if (lower.Contains(forbiddenClusters[i].ToLowerInvariant(), StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Check if a name contains any favored clusters.
    /// </summary>
    public static bool HasFavoredCluster(string name, IReadOnlyList<string> favoredClusters)
    {
        var lower = name.ToLowerInvariant();
        for (var i = 0; i < favoredClusters.Count; i++)
        {
            if (lower.Contains(favoredClusters[i].ToLowerInvariant(), StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Estimate syllable count by counting vowel groups.
    /// </summary>
    public static int EstimateSyllableCount(string word)
    {
        var matches = VowelGroupRegex().Matches(word.ToLowerInvariant());
        return matches.Count > 0 ? matches.Count : 1;
    }

    /// <summary>
    /// Check if a string ends with any of the given suffixes (case-insensitive).
    /// </summary>
    public static bool EndsWithAny(string str, IReadOnlyList<string> suffixes)
    {
        for (var i = 0; i < suffixes.Count; i++)
        {
            if (str.EndsWith(suffixes[i], StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    [GeneratedRegex(@"(\s+)")]
    private static partial Regex WhitespaceSplitRegex();

    [GeneratedRegex(@"^\s+$")]
    private static partial Regex WhitespaceOnlyRegex();

    [GeneratedRegex("[aeiou]+")]
    private static partial Regex VowelGroupRegex();
}
