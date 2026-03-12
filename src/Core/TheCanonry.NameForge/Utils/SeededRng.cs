namespace TheCanonry.NameForge.Utils;

/// <summary>
/// Seeded random number generator wrapping System.Random.
/// Provides deterministic generation for reproducible name output.
/// </summary>
public sealed class SeededRng
{
    private readonly Random _random;

    /// <summary>
    /// Create an RNG from a string seed (hashed to an integer seed).
    /// </summary>
    public SeededRng(string seed)
    {
        _random = new Random(GetDeterministicHashCode(seed));
    }

    /// <summary>
    /// Create an RNG from an integer seed.
    /// </summary>
    public SeededRng(int seed)
    {
        _random = new Random(seed);
    }

    /// <summary>
    /// Returns a random double in [0, 1).
    /// </summary>
    public double NextDouble() => _random.NextDouble();

    /// <summary>
    /// Returns a random integer in [min, max] inclusive.
    /// </summary>
    public int NextInt(int min, int max) => _random.Next(min, max + 1);

    /// <summary>
    /// Pick a random element from a list.
    /// </summary>
    public T PickRandom<T>(IReadOnlyList<T> items)
    {
        if (items.Count == 0)
            throw new ArgumentException("Cannot pick from empty collection.");
        return items[_random.Next(items.Count)];
    }

    /// <summary>
    /// Pick a random element from a list using weights.
    /// Weights must be non-negative (normalized internally).
    /// </summary>
    public T PickWeighted<T>(IReadOnlyList<T> items, IReadOnlyList<double> weights)
    {
        if (items.Count == 0)
            throw new ArgumentException("Cannot pick from empty collection.");

        if (weights.Count != items.Count)
            return PickRandom(items);

        var total = 0.0;
        for (var i = 0; i < weights.Count; i++)
            total += Math.Max(0, weights[i]);

        if (total == 0)
            return PickRandom(items);

        var r = _random.NextDouble() * total;
        var cumulative = 0.0;
        for (var i = 0; i < items.Count; i++)
        {
            cumulative += Math.Max(0, weights[i]);
            if (r < cumulative)
                return items[i];
        }

        return items[^1];
    }

    /// <summary>
    /// Returns true with the given probability [0, 1].
    /// </summary>
    public bool Chance(double probability) => _random.NextDouble() < probability;

    /// <summary>
    /// Shuffle a list using Fisher-Yates algorithm, returning a new list.
    /// </summary>
    public List<T> Shuffle<T>(IReadOnlyList<T> items)
    {
        var result = new List<T>(items);
        for (var i = result.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }
        return result;
    }

    /// <summary>
    /// Deterministic hash code for a string.
    /// Uses a stable algorithm independent of .NET runtime behavior.
    /// </summary>
    private static int GetDeterministicHashCode(string str)
    {
        unchecked
        {
            var hash1 = 5381;
            var hash2 = hash1;

            for (var i = 0; i < str.Length; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ str[i];
                if (i + 1 < str.Length)
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
            }

            return hash1 + (hash2 * 1566083941);
        }
    }
}
