using TheCanonry.NameForge.Types;
using TheCanonry.NameForge.Utils;

namespace TheCanonry.NameForge.Generation;

/// <summary>
/// Result of style application.
/// </summary>
public sealed record StyleResult(string Result, string[] Transforms);

/// <summary>
/// Style: applies stylistic transforms (apostrophes, hyphens, capitalization) to names.
/// </summary>
public static class Style
{
    /// <summary>
    /// Apply stylistic transforms to a name.
    /// </summary>
    public static StyleResult ApplyStyle(
        SeededRng rng,
        string name,
        StyleRules style,
        string[] syllables)
    {
        var result = name;
        var transforms = new List<string>();

        var apostropheRate = style.ApostropheRate;
        var hyphenRate = style.HyphenRate;
        var capitalization = style.Capitalization;

        var wantApostrophe = apostropheRate > 0 && rng.Chance(apostropheRate);
        var wantHyphen = hyphenRate > 0 && rng.Chance(hyphenRate);

        if ((wantApostrophe || wantHyphen) && syllables.Length > 1)
        {
            var boundaries = StringHelpers.FindSyllableBoundaries(result, syllables);
            if (boundaries.Length > 0)
            {
                result = InsertStyleMarkers(result, boundaries, wantApostrophe, wantHyphen, rng, transforms);
            }
        }

        result = StringHelpers.ApplyCapitalization(result, capitalization);
        transforms.Add($"cap:{capitalization}");

        return new StyleResult(result, [.. transforms]);
    }

    /// <summary>
    /// Apply rhythm-based adjustments to a name.
    /// Currently a placeholder for future enhancement.
    /// </summary>
    public static string ApplyRhythmBias(string name, RhythmBias rhythmBias)
    {
        _ = rhythmBias;
        return name;
    }

    private static string InsertStyleMarkers(
        string text,
        int[] boundaries,
        bool wantApostrophe,
        bool wantHyphen,
        SeededRng rng,
        List<string> transforms)
    {
        if (wantApostrophe && wantHyphen)
            return InsertBothMarkers(text, boundaries, rng, transforms);

        if (wantApostrophe)
        {
            transforms.Add("apostrophe");
            return StringHelpers.InsertAtBoundary(text, '\'', boundaries, rng);
        }

        transforms.Add("hyphen");
        return StringHelpers.InsertAtBoundary(text, '-', boundaries, rng);
    }

    private static string InsertBothMarkers(
        string text,
        int[] boundaries,
        SeededRng rng,
        List<string> transforms)
    {
        if (boundaries.Length >= 2)
        {
            var shuffled = rng.Shuffle<int>(boundaries);
            var apoIdx = shuffled[0];
            var hypIdx = shuffled[1];

            var result = text;
            if (apoIdx > hypIdx)
            {
                result = result[..apoIdx] + "'" + result[apoIdx..];
                result = result[..hypIdx] + "-" + result[hypIdx..];
            }
            else
            {
                result = result[..hypIdx] + "-" + result[hypIdx..];
                result = result[..apoIdx] + "'" + result[apoIdx..];
            }
            transforms.Add("apostrophe");
            transforms.Add("hyphen");
            return result;
        }

        // Only one boundary: randomly pick one marker
        if (rng.Chance(0.5))
        {
            transforms.Add("apostrophe");
            return StringHelpers.InsertAtBoundary(text, '\'', boundaries, rng);
        }
        transforms.Add("hyphen");
        return StringHelpers.InsertAtBoundary(text, '-', boundaries, rng);
    }
}
