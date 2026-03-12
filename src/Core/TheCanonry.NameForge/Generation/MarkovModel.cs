namespace TheCanonry.NameForge.Generation;

/// <summary>
/// Pre-trained character-level Markov chain model.
/// </summary>
public sealed record MarkovModel
{
    /// <summary>Order of the Markov chain (number of characters in state).</summary>
    public required int Order { get; init; }

    /// <summary>Start states mapped to their probabilities.</summary>
    public required Dictionary<string, double> StartStates { get; init; }

    /// <summary>Transition table: state -> (character -> probability).</summary>
    public required Dictionary<string, Dictionary<string, double>> Transitions { get; init; }
}
