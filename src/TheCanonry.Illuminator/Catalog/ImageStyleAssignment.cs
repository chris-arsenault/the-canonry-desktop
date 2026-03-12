/**
 * Image Style Assignment — Deterministic corpus-level distribution balancer
 *
 * Ported from apps/illuminator/webui/src/lib/imageStyleAssignment.ts
 *
 * Primary assignment:
 *   Each dimension (artistic, composition, palette) is balanced independently.
 *   Start with top-ranked for each item. Repeatedly find the most
 *   overrepresented candidate and shift one of its assignments to the most
 *   underrepresented candidate that appears in that item's ranked list.
 *   Continue until max - min <= 1 or no more shifts are possible.
 *
 * Secondary assignment (pair-novelty greedy):
 *   Enumerate all (artistic × composition × palette) combos from remaining
 *   ranked options (excluding primary). Score each by how many of the 3 pairs
 *   (a|c, a|p, c|p) have not been used yet. Pick the highest-scoring combo.
 *
 * Forbidden combination enforcement:
 *   After primary assignment, check each item for forbidden artistic×composition
 *   pairs. Try swapping artistic first, then composition.
 */

namespace TheCanonry.Illuminator.Catalog;

public sealed record StyleAssignmentInput(
    string ItemId,
    IReadOnlyList<string> ArtisticRanked,
    IReadOnlyList<string> CompositionRanked,
    IReadOnlyList<string> PaletteRanked);

public sealed record StyleAssignmentResult(
    string ItemId,
    string PrimaryArtistic,
    string PrimaryComposition,
    string PrimaryPalette,
    string? SecondaryArtistic,
    string? SecondaryComposition,
    string? SecondaryPalette);

public static class ImageStyleAssignment
{
    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Assign primary styles using deterministic distribution balancing.
    /// Each dimension is balanced independently. Secondary slots are null.
    /// </summary>
    public static IReadOnlyList<StyleAssignmentResult> AssignPrimary(
        IReadOnlyList<StyleAssignmentInput> items)
    {
        if (items.Count == 0)
            return Array.Empty<StyleAssignmentResult>();

        var artisticResults  = BalanceDistribution(items.Select(i => i.ArtisticRanked).ToList());
        var compositionResults = BalanceDistribution(items.Select(i => i.CompositionRanked).ToList());
        var paletteResults   = BalanceDistribution(items.Select(i => i.PaletteRanked).ToList());

        var results = new StyleAssignmentResult[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            results[i] = new StyleAssignmentResult(
                ItemId:             items[i].ItemId,
                PrimaryArtistic:    artisticResults[i],
                PrimaryComposition: compositionResults[i],
                PrimaryPalette:     paletteResults[i],
                SecondaryArtistic:    null,
                SecondaryComposition: null,
                SecondaryPalette:     null);
        }

        EnforceForbiddenCombinations(results, items);
        return results;
    }

    /// <summary>
    /// Assign secondary styles by pair-novelty greedy selection.
    /// Returns new records with secondary slots filled; primary slots are
    /// copied unchanged from <paramref name="primaryResults"/>.
    /// </summary>
    public static IReadOnlyList<StyleAssignmentResult> AssignSecondary(
        IReadOnlyList<StyleAssignmentResult> primaryResults,
        IReadOnlyList<StyleAssignmentInput> items)
    {
        if (primaryResults.Count == 0)
            return Array.Empty<StyleAssignmentResult>();

        // Build lookup: itemId → input rankings
        var inputMap = items.ToDictionary(i => i.ItemId, StringComparer.Ordinal);

        // Seed used-pairs from all primary assignments
        var usedPairs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in primaryResults)
        {
            usedPairs.Add($"a:{p.PrimaryArtistic}|c:{p.PrimaryComposition}");
            usedPairs.Add($"a:{p.PrimaryArtistic}|p:{p.PrimaryPalette}");
            usedPairs.Add($"c:{p.PrimaryComposition}|p:{p.PrimaryPalette}");
        }

        var results = new StyleAssignmentResult[primaryResults.Count];

        // Shuffle iteration order to avoid systematic bias (deterministic seed for reproducibility)
        var indices = Enumerable.Range(0, primaryResults.Count).ToArray();
        var rng = new Random(42);
        for (var i = indices.Length - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        // Place results in a temporary array indexed by position
        var temp = new StyleAssignmentResult?[primaryResults.Count];

        foreach (var idx in indices)
        {
            var primary = primaryResults[idx];
            if (!inputMap.TryGetValue(primary.ItemId, out var input))
            {
                temp[idx] = primary;
                continue;
            }

            // Candidate pools: ranked options excluding primary
            var artCandidates  = ExcludePrimary(input.ArtisticRanked,    primary.PrimaryArtistic);
            var compCandidates = ExcludePrimary(input.CompositionRanked, primary.PrimaryComposition);
            var palCandidates  = ExcludePrimary(input.PaletteRanked,     primary.PrimaryPalette);

            // Fallback: if filtering removed everything, use full list
            var arts  = artCandidates.Count  > 0 ? artCandidates  : input.ArtisticRanked;
            var comps = compCandidates.Count > 0 ? compCandidates : input.CompositionRanked;
            var pals  = palCandidates.Count  > 0 ? palCandidates  : input.PaletteRanked;

            // Enumerate all combos, score by novel pair count
            var bestA = arts[0];
            var bestC = comps[0];
            var bestP = pals[0];
            var bestScore = -1;

            foreach (var a in arts)
            {
                foreach (var c in comps)
                {
                    foreach (var p in pals)
                    {
                        var score = 0;
                        if (!usedPairs.Contains($"a:{a}|c:{c}")) score++;
                        if (!usedPairs.Contains($"a:{a}|p:{p}")) score++;
                        if (!usedPairs.Contains($"c:{c}|p:{p}")) score++;

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestA = a;
                            bestC = c;
                            bestP = p;
                        }
                    }
                }
            }

            // Record and update used pairs
            usedPairs.Add($"a:{bestA}|c:{bestC}");
            usedPairs.Add($"a:{bestA}|p:{bestP}");
            usedPairs.Add($"c:{bestC}|p:{bestP}");

            temp[idx] = primary with
            {
                SecondaryArtistic    = bestA,
                SecondaryComposition = bestC,
                SecondaryPalette     = bestP,
            };
        }

        for (var i = 0; i < primaryResults.Count; i++)
            results[i] = temp[i] ?? primaryResults[i];

        return results;
    }

    /// <summary>
    /// Full pipeline: primary balancing → secondary pair-novelty → forbidden enforcement.
    /// </summary>
    public static IReadOnlyList<StyleAssignmentResult> AssignAll(
        IReadOnlyList<StyleAssignmentInput> items)
    {
        var primary   = AssignPrimary(items);
        var secondary = AssignSecondary(primary, items);
        return secondary;
    }

    // -------------------------------------------------------------------------
    // Core balancing algorithm
    // -------------------------------------------------------------------------

    /// <summary>
    /// Given items each with a ranked list of candidates, assign one candidate
    /// per item targeting even distribution across all candidates.
    ///
    /// Strategy: start with rank-0 for everyone, then repeatedly find the most
    /// overrepresented candidate and shift one assignment to the most
    /// underrepresented candidate that appears in that item's ranked list.
    /// Stop when max - min &lt;= 1 or no shift is possible.
    /// </summary>
    private static IReadOnlyList<string> BalanceDistribution(
        IReadOnlyList<IReadOnlyList<string>> rankedLists)
    {
        var n = rankedLists.Count;
        if (n == 0) return Array.Empty<string>();

        // Start with rank-0 for everyone
        var assignments = rankedLists.Select(r => r.Count > 0 ? r[0] : "").ToArray();

        // Collect all candidates
        var allCandidates = new HashSet<string>(StringComparer.Ordinal);
        foreach (var ranked in rankedLists)
            foreach (var id in ranked)
                allCandidates.Add(id);

        if (allCandidates.Count <= 1)
            return assignments;

        // Reverse index: candidate → list of (itemIdx, rank)
        var candidateToItems = new Dictionary<string, List<(int ItemIdx, int Rank)>>(StringComparer.Ordinal);
        for (var i = 0; i < n; i++)
        {
            for (var rank = 0; rank < rankedLists[i].Count; rank++)
            {
                var id = rankedLists[i][rank];
                if (!candidateToItems.TryGetValue(id, out var list))
                {
                    list = new List<(int, int)>();
                    candidateToItems[id] = list;
                }
                list.Add((i, rank));
            }
        }

        // Mutable counts
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var id in allCandidates) counts[id] = 0;
        foreach (var a in assignments) counts[a] = counts.GetValueOrDefault(a) + 1;

        var stuck = new HashSet<string>(StringComparer.Ordinal);

        for (var round = 0; round < n * 5; round++)
        {
            // Most overrepresented
            string? overCandidate = null;
            var overCount = 0;
            foreach (var (id, count) in counts)
            {
                if (count > overCount)
                {
                    overCandidate = id;
                    overCount = count;
                }
            }

            // Most underrepresented (excluding stuck)
            string? underCandidate = null;
            var underCount = int.MaxValue;
            foreach (var (id, count) in counts)
            {
                if (stuck.Contains(id)) continue;
                if (count < underCount)
                {
                    underCandidate = id;
                    underCount = count;
                }
            }

            if (overCandidate is null || underCandidate is null || overCount - underCount <= 1)
                break;

            if (!candidateToItems.TryGetValue(underCandidate, out var donors) || donors.Count == 0)
            {
                stuck.Add(underCandidate);
                continue;
            }

            var bestIdx   = -1;
            var bestScore = int.MinValue;

            foreach (var (itemIdx, rank) in donors)
            {
                var currentAssigned = assignments[itemIdx];
                if (currentAssigned == underCandidate) continue;
                var donorCount = counts.GetValueOrDefault(currentAssigned);
                // Only take from a candidate that is above underCount + 1
                if (donorCount <= underCount + 1) continue;
                // Prefer most overrepresented; tiebreak by rank (lower is better)
                var score = donorCount * 100 - rank * 10;
                if (score > bestScore)
                {
                    bestIdx   = itemIdx;
                    bestScore = score;
                }
            }

            if (bestIdx == -1)
            {
                stuck.Add(underCandidate);
                continue;
            }

            // Perform shift
            var old = assignments[bestIdx];
            counts[old] = counts.GetValueOrDefault(old) - 1;
            counts[underCandidate] = counts.GetValueOrDefault(underCandidate) + 1;
            assignments[bestIdx] = underCandidate;

            // Landscape changed — previously stuck candidates may now be shiftable
            stuck.Clear();
        }

        return assignments;
    }

    // -------------------------------------------------------------------------
    // Forbidden combination enforcement
    // -------------------------------------------------------------------------

    /// <summary>
    /// For each entry, if the primary artistic×composition pair is forbidden,
    /// try swapping the artistic style first (using next-ranked valid option),
    /// then fall back to swapping the composition.
    /// Mutates the results array in place.
    /// </summary>
    private static void EnforceForbiddenCombinations(
        StyleAssignmentResult[] results,
        IReadOnlyList<StyleAssignmentInput> inputs)
    {
        for (var i = 0; i < results.Length; i++)
        {
            var r = results[i];
            if (!ForbiddenCombinations.IsForbidden(r.PrimaryArtistic, r.PrimaryComposition))
                continue;

            // Try swapping artistic
            var swapped = false;
            foreach (var altArt in inputs[i].ArtisticRanked)
            {
                if (altArt == r.PrimaryArtistic) continue;
                if (!ForbiddenCombinations.IsForbidden(altArt, r.PrimaryComposition))
                {
                    results[i] = r with { PrimaryArtistic = altArt };
                    swapped = true;
                    break;
                }
            }
            if (swapped) continue;

            // Try swapping composition
            foreach (var altComp in inputs[i].CompositionRanked)
            {
                if (altComp == r.PrimaryComposition) continue;
                if (!ForbiddenCombinations.IsForbidden(r.PrimaryArtistic, altComp))
                {
                    results[i] = r with { PrimaryComposition = altComp };
                    break;
                }
            }
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static IReadOnlyList<string> ExcludePrimary(
        IReadOnlyList<string> ranked, string primaryId)
    {
        var filtered = ranked.Where(id => id != primaryId).ToList();
        return filtered;
    }
}
