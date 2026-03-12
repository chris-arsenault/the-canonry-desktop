using TheCanonry.NameForge.Utils;

namespace TheCanonry.NameForge.Tests.Utils;

public class SeededRngTests
{
    [Fact]
    public void SameSeed_ProducesSameSequence()
    {
        var rng1 = new SeededRng("test-seed");
        var rng2 = new SeededRng("test-seed");

        for (var i = 0; i < 100; i++)
            Assert.Equal(rng1.NextDouble(), rng2.NextDouble());
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentSequences()
    {
        var rng1 = new SeededRng("seed-a");
        var rng2 = new SeededRng("seed-b");

        var allSame = true;
        for (var i = 0; i < 20; i++)
        {
            if (rng1.NextDouble() != rng2.NextDouble())
            {
                allSame = false;
                break;
            }
        }
        Assert.False(allSame);
    }

    [Fact]
    public void NextInt_ReturnsValuesInRange()
    {
        var rng = new SeededRng("range-test");
        for (var i = 0; i < 200; i++)
        {
            var value = rng.NextInt(3, 7);
            Assert.InRange(value, 3, 7);
        }
    }

    [Fact]
    public void PickRandom_ReturnsElementFromList()
    {
        var rng = new SeededRng("pick-test");
        var items = new[] { "a", "b", "c", "d" };

        for (var i = 0; i < 50; i++)
            Assert.Contains(rng.PickRandom<string>(items), items);
    }

    [Fact]
    public void PickRandom_ThrowsOnEmptyList()
    {
        var rng = new SeededRng("empty-test");
        Assert.Throws<ArgumentException>(() => rng.PickRandom<string>(Array.Empty<string>()));
    }

    [Fact]
    public void PickWeighted_RespectsWeights()
    {
        var rng = new SeededRng("weighted-test");
        var items = new[] { "rare", "common" };
        var weights = new[] { 0.01, 0.99 };

        var counts = new Dictionary<string, int> { ["rare"] = 0, ["common"] = 0 };
        for (var i = 0; i < 1000; i++)
            counts[rng.PickWeighted<string>(items, weights)]++;

        // Common should appear much more often
        Assert.True(counts["common"] > counts["rare"] * 3,
            $"Expected 'common' ({counts["common"]}) to be at least 3x 'rare' ({counts["rare"]})");
    }

    [Fact]
    public void PickWeighted_FallsBackToUniformWithMismatchedWeights()
    {
        var rng = new SeededRng("mismatch-test");
        var items = new[] { "a", "b", "c" };
        var weights = new[] { 1.0 }; // Wrong length

        // Should not throw, falls back to uniform
        var result = rng.PickWeighted<string>(items, weights);
        Assert.Contains(result, items);
    }

    [Fact]
    public void Chance_ReturnsTrueAndFalse()
    {
        var rng = new SeededRng("chance-test");
        var trueCount = 0;
        var falseCount = 0;

        for (var i = 0; i < 1000; i++)
        {
            if (rng.Chance(0.5))
                trueCount++;
            else
                falseCount++;
        }

        // Both should have occurred
        Assert.True(trueCount > 100, $"Expected true count > 100, got {trueCount}");
        Assert.True(falseCount > 100, $"Expected false count > 100, got {falseCount}");
    }

    [Fact]
    public void Shuffle_ReturnsAllElements()
    {
        var rng = new SeededRng("shuffle-test");
        var items = new[] { 1, 2, 3, 4, 5 };
        var shuffled = rng.Shuffle<int>(items);

        Assert.Equal(items.Length, shuffled.Count);
        foreach (var item in items)
            Assert.Contains(item, shuffled);
    }

    [Fact]
    public void Shuffle_IsDeterministic()
    {
        var rng1 = new SeededRng("shuffle-det");
        var rng2 = new SeededRng("shuffle-det");
        var items = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        var shuffled1 = rng1.Shuffle<int>(items);
        var shuffled2 = rng2.Shuffle<int>(items);

        Assert.Equal(shuffled1, shuffled2);
    }
}
