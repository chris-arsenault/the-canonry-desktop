namespace TheCanonry.Illuminator.Catalog;

/// <summary>
/// Pairwise title similarity analysis using normalized Levenshtein distance.
///
/// Ported from apps/illuminator/webui/src/lib/catalogSimilarity.ts and
/// apps/illuminator/webui/src/lib/catalogSimilarityWorker.ts.
///
/// The TS implementation offloads the O(n²) computation to a web worker.
/// In C#, we run it on the calling thread; callers can wrap in Task.Run if needed.
/// </summary>
public sealed record SimilarityPair(
    string ImageIdA,
    string TitleA,
    string ImageIdB,
    string TitleB,
    double Score);

public static class CatalogSimilarity
{
    /// <summary>
    /// Default similarity threshold: pairs at or above this score are reported.
    /// </summary>
    public const double DefaultThreshold = 0.5;

    /// <summary>
    /// Detect similar titles using normalized Levenshtein distance.
    /// Returns pairs above the given threshold (default 0.5).
    /// Only images with non-null, non-empty titles are considered.
    /// </summary>
    public static IReadOnlyList<SimilarityPair> FindSimilarTitles(
        IReadOnlyList<ImageCatalogEntry> images,
        double threshold = DefaultThreshold)
    {
        var titled = images
            .Where(img => !string.IsNullOrEmpty(img.Title))
            .ToList();

        var pairs = new List<SimilarityPair>();

        for (var i = 0; i < titled.Count - 1; i++)
        {
            for (var j = i + 1; j < titled.Count; j++)
            {
                var a = titled[i];
                var b = titled[j];
                var score = ComputeSimilarity(a.Title!.ToLowerInvariant(), b.Title!.ToLowerInvariant());
                if (score >= threshold)
                    pairs.Add(new SimilarityPair(a.ImageId, a.Title!, b.ImageId, b.Title!, score));
            }
        }

        // Sort descending by score (most similar first)
        pairs.Sort((x, y) => y.Score.CompareTo(x.Score));
        return pairs;
    }

    /// <summary>
    /// Compute normalized Levenshtein similarity.
    /// Returns 1.0 for identical strings, 0.0 for completely different strings.
    /// Formula: 1.0 - (editDistance / max(a.Length, b.Length))
    /// Empty strings are considered identical to each other (returns 1.0).
    /// </summary>
    public static double ComputeSimilarity(string a, string b)
    {
        if (a.Length == 0 && b.Length == 0) return 1.0;

        var maxLen = Math.Max(a.Length, b.Length);
        if (maxLen == 0) return 1.0;

        var distance = LevenshteinDistance(a, b);
        return 1.0 - (double)distance / maxLen;
    }

    // -------------------------------------------------------------------------
    // Levenshtein distance (classic DP, O(m*n) space optimized to two rows)
    // -------------------------------------------------------------------------

    private static int LevenshteinDistance(string a, string b)
    {
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;
        if (a == b) return 0;

        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];

        for (var j = 0; j <= b.Length; j++)
            previous[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(previous[j] + 1, current[j - 1] + 1),
                    previous[j - 1] + cost);
            }
            Array.Copy(current, previous, b.Length + 1);
        }

        return previous[b.Length];
    }
}
