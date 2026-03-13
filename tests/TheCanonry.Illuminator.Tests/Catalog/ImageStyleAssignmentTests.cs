using TheCanonry.Illuminator.Catalog;

namespace TheCanonry.Illuminator.Tests.Catalog;

public sealed class ImageStyleAssignmentTests
{
    // -------------------------------------------------------------------------
    // Test 1: Empty input returns empty results
    // -------------------------------------------------------------------------

    [Fact]
    public void AssignAll_EmptyInput_ReturnsEmptyList()
    {
        var results = ImageStyleAssignment.AssignAll(Array.Empty<StyleAssignmentInput>());
        Assert.Empty(results);
    }

    [Fact]
    public void AssignPrimary_EmptyInput_ReturnsEmptyList()
    {
        var results = ImageStyleAssignment.AssignPrimary(Array.Empty<StyleAssignmentInput>());
        Assert.Empty(results);
    }

    // -------------------------------------------------------------------------
    // Test 2: Single item gets top-ranked style
    // -------------------------------------------------------------------------

    [Fact]
    public void AssignPrimary_SingleItem_GetsTopRankedStyle()
    {
        var items = new[]
        {
            new StyleAssignmentInput(
                "item-1",
                ArtisticRanked:     ["oil-painting", "watercolor", "gouache"],
                CompositionRanked:  ["portrait-closeup", "portrait-bust"],
                PaletteRanked:      ["warm-earth", "cool-blue"]),
        };

        var results = ImageStyleAssignment.AssignPrimary(items);

        Assert.Single(results);
        Assert.Equal("oil-painting",    results[0].PrimaryArtistic);
        Assert.Equal("portrait-closeup", results[0].PrimaryComposition);
        Assert.Equal("warm-earth",       results[0].PrimaryPalette);
    }

    // -------------------------------------------------------------------------
    // Test 3: Primary assignment produces uniform distribution
    // -------------------------------------------------------------------------

    [Fact]
    public void AssignPrimary_ProducesUniformDistribution()
    {
        // 6 items, 3 artistic styles — each style should appear ~2 times (spread ≤ 1)
        var items = Enumerable.Range(0, 6).Select(i =>
            new StyleAssignmentInput(
                $"item-{i}",
                ArtisticRanked:    ["style-a", "style-b", "style-c"],
                CompositionRanked: ["comp-x"],
                PaletteRanked:     ["pal-p"])).ToList();

        var results = ImageStyleAssignment.AssignPrimary(items);

        var distribution = results
            .GroupBy(r => r.PrimaryArtistic)
            .ToDictionary(g => g.Key, g => g.Count());

        var counts = distribution.Values.ToList();
        var spread = counts.Max() - counts.Min();

        Assert.True(spread <= 1,
            $"Expected spread ≤ 1, got spread={spread}, distribution={string.Join(",", distribution.Select(kv => $"{kv.Key}:{kv.Value}"))}");

        // All 3 styles should be represented
        Assert.Equal(3, distribution.Count);
    }

    // -------------------------------------------------------------------------
    // Test 4: Primary assignment respects ranked preferences
    // -------------------------------------------------------------------------

    [Fact]
    public void AssignPrimary_RespectsRankedPreferences()
    {
        // Each item has a unique top-ranked style → all get their top choice
        var items = new[]
        {
            new StyleAssignmentInput("item-1", ["style-a", "style-b", "style-c"], ["comp-x"], ["pal-p"]),
            new StyleAssignmentInput("item-2", ["style-b", "style-a", "style-c"], ["comp-x"], ["pal-p"]),
            new StyleAssignmentInput("item-3", ["style-c", "style-a", "style-b"], ["comp-x"], ["pal-p"]),
        };

        var results = ImageStyleAssignment.AssignPrimary(items);

        // 3 items, 3 distinct styles — perfectly balanced, everyone keeps top pick
        Assert.Equal("style-a", results.First(r => r.ItemId == "item-1").PrimaryArtistic);
        Assert.Equal("style-b", results.First(r => r.ItemId == "item-2").PrimaryArtistic);
        Assert.Equal("style-c", results.First(r => r.ItemId == "item-3").PrimaryArtistic);
    }

    // -------------------------------------------------------------------------
    // Test 5: Secondary assignment maximizes pair novelty
    // -------------------------------------------------------------------------

    [Fact]
    public void AssignSecondary_MaximizesPairNovelty()
    {
        // Two items with the same primary. The secondary should differ between them
        // to maximize novel pairs.
        var items = new[]
        {
            new StyleAssignmentInput(
                "item-1",
                ArtisticRanked:    ["style-a", "style-b", "style-c"],
                CompositionRanked: ["comp-x", "comp-y"],
                PaletteRanked:     ["pal-p", "pal-q"]),
            new StyleAssignmentInput(
                "item-2",
                ArtisticRanked:    ["style-a", "style-b", "style-c"],
                CompositionRanked: ["comp-x", "comp-y"],
                PaletteRanked:     ["pal-p", "pal-q"]),
        };

        // Force both to the same primary to stress secondary novelty
        var primary = new[]
        {
            new StyleAssignmentResult("item-1", "style-a", "comp-x", "pal-p", null, null, null),
            new StyleAssignmentResult("item-2", "style-a", "comp-x", "pal-p", null, null, null),
        };

        var results = ImageStyleAssignment.AssignSecondary(primary, items);

        // Both should have secondaries assigned
        Assert.All(results, r => Assert.NotNull(r.SecondaryArtistic));
        Assert.All(results, r => Assert.NotNull(r.SecondaryComposition));
        Assert.All(results, r => Assert.NotNull(r.SecondaryPalette));

        // Secondaries should be excluded from primary (since alternatives exist)
        Assert.NotEqual("style-a", results[0].SecondaryArtistic);
        Assert.NotEqual("style-a", results[1].SecondaryArtistic);

        // The two secondary assignments together should cover novel pairs
        // (at least one pair that differs between the two items)
        var pair1 = $"{results[0].SecondaryArtistic}|{results[0].SecondaryComposition}";
        var pair2 = $"{results[1].SecondaryArtistic}|{results[1].SecondaryComposition}";
        // With only 2 items, pair novelty means item-2 should pick a combo that
        // introduces at least one new pair vs item-1
        Assert.True(
            pair1 != pair2 ||
            results[0].SecondaryPalette != results[1].SecondaryPalette,
            "Expected secondaries to differ in at least one dimension to maximize novelty");
    }

    // -------------------------------------------------------------------------
    // Test 6: Forbidden combination enforcement swaps to valid option
    // -------------------------------------------------------------------------

    [Fact]
    public void AssignPrimary_ForbiddenArtisticCompositionPair_SwapsToValidOption()
    {
        // oil-painting × logo-mark is forbidden (Rule 2: painting × logo/badge)
        // The item ranks oil-painting first and logo-mark first.
        // After forbidden enforcement, the assignment must not be oil-painting × logo-mark.
        var items = new[]
        {
            new StyleAssignmentInput(
                "item-1",
                ArtisticRanked:    ["oil-painting", "ink-wash"],
                CompositionRanked: ["logo-mark", "portrait-closeup"],
                PaletteRanked:     ["warm-earth"]),
        };

        var results = ImageStyleAssignment.AssignPrimary(items);

        var r = results[0];
        Assert.False(
            ForbiddenCombinations.IsForbidden(r.PrimaryArtistic, r.PrimaryComposition),
            $"Expected a non-forbidden combination, got {r.PrimaryArtistic} × {r.PrimaryComposition}");
    }

    // -------------------------------------------------------------------------
    // Test 7: ForbiddenCombinations.IsForbidden returns true for known bad pairs
    // -------------------------------------------------------------------------

    [Fact]
    public void ForbiddenCombinations_KnownBadPairs_ReturnTrue()
    {
        // oil-painting is category:painting, logo-mark is forbidden with painting
        Assert.True(ForbiddenCombinations.IsForbidden("oil-painting", "logo-mark"));

        // document style with character composition
        Assert.True(ForbiddenCombinations.IsForbidden("illuminated-manuscript", "portrait-closeup"));

        // tilt-shift with character composition
        Assert.True(ForbiddenCombinations.IsForbidden("tilt-shift", "portrait-closeup"));
    }

    [Fact]
    public void ForbiddenCombinations_ValidPairs_ReturnFalse()
    {
        // oil-painting × portrait-closeup is a normal combination
        Assert.False(ForbiddenCombinations.IsForbidden("oil-painting", "portrait-closeup"));

        // watercolor × panoramic-vista is fine
        Assert.False(ForbiddenCombinations.IsForbidden("watercolor", "panoramic-vista"));
    }

    // -------------------------------------------------------------------------
    // Test 8: AssignAll full pipeline produces non-null primary and secondary
    // -------------------------------------------------------------------------

    [Fact]
    public void AssignAll_MultipleItems_AssignsBothPrimaryAndSecondary()
    {
        var items = Enumerable.Range(0, 4).Select(i =>
            new StyleAssignmentInput(
                $"item-{i}",
                ArtisticRanked:    ["style-a", "style-b", "style-c"],
                CompositionRanked: ["comp-x", "comp-y", "comp-z"],
                PaletteRanked:     ["pal-p", "pal-q", "pal-r"])).ToList();

        var results = ImageStyleAssignment.AssignAll(items);

        Assert.Equal(4, results.Count);
        Assert.All(results, r =>
        {
            Assert.NotEmpty(r.PrimaryArtistic);
            Assert.NotEmpty(r.PrimaryComposition);
            Assert.NotEmpty(r.PrimaryPalette);
            Assert.NotNull(r.SecondaryArtistic);
            Assert.NotNull(r.SecondaryComposition);
            Assert.NotNull(r.SecondaryPalette);
        });
    }

    // -------------------------------------------------------------------------
    // Test 9: Balancing shifts from overrepresented to underrepresented
    // -------------------------------------------------------------------------

    [Fact]
    public void AssignPrimary_AllItemsPreferSameStyle_BalancesDistribution()
    {
        // All 9 items prefer style-a first, but 3 styles available
        // Should end up with 3 of each (spread = 0)
        var items = Enumerable.Range(0, 9).Select(i =>
            new StyleAssignmentInput(
                $"item-{i}",
                ArtisticRanked:    ["style-a", "style-b", "style-c"],
                CompositionRanked: ["comp-x"],
                PaletteRanked:     ["pal-p"])).ToList();

        var results = ImageStyleAssignment.AssignPrimary(items);

        var distribution = results
            .GroupBy(r => r.PrimaryArtistic)
            .ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(3, distribution.Count);
        var counts = distribution.Values.ToList();
        Assert.Equal(0, counts.Max() - counts.Min());
    }
}
