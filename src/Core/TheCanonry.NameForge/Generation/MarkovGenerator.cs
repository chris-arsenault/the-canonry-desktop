using TheCanonry.NameForge.Utils;

namespace TheCanonry.NameForge.Generation;

/// <summary>
/// Character-level Markov chain name generation.
/// </summary>
public static class MarkovGenerator
{
    private const string End = "$";

    /// <summary>
    /// Generate a name from a Markov model.
    /// </summary>
    public static string Generate(
        MarkovModel model,
        SeededRng rng,
        int minLength = 3,
        int maxLength = 12)
    {
        var state = WeightedRandom(model.StartStates, rng);
        var result = "";

        for (var i = 0; i < maxLength + model.Order; i++)
        {
            if (!model.Transitions.TryGetValue(state, out var nextProbs))
                break;

            var next = WeightedRandom(nextProbs, rng);
            if (next == End)
            {
                if (result.Length >= minLength) break;
                continue;
            }

            result += next;
            state = state[1..] + next;
        }

        if (result.Length == 0) return "Name";

        return char.ToUpperInvariant(result[0]) + result[1..];
    }

    /// <summary>
    /// Weighted random selection from a probability distribution.
    /// </summary>
    internal static string WeightedRandom(Dictionary<string, double> probs, SeededRng rng)
    {
        var r = rng.NextDouble();
        var sum = 0.0;
        foreach (var (item, prob) in probs)
        {
            sum += prob;
            if (r <= sum) return item;
        }
        // Fallback
        return probs.Keys.First();
    }
}
