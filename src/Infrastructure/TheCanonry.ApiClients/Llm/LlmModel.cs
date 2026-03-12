namespace TheCanonry.ApiClients.Llm;

/// <summary>
/// Known Claude model identifiers and per-model pricing.
/// </summary>
public static class LlmModel
{
    public const string Opus46 = "claude-opus-4-6";
    public const string Sonnet46 = "claude-sonnet-4-6";
    public const string Haiku45 = "claude-haiku-4-5-20251001";

    /// <summary>
    /// Models that support extended thinking.
    /// </summary>
    public static readonly IReadOnlyList<string> ThinkingCapableModels =
        [Opus46, Sonnet46];

    /// <summary>
    /// Cost per million input tokens, keyed by model ID.
    /// </summary>
    public static decimal InputCostPerMTok(string model) => model switch
    {
        Opus46 => 15m,
        Sonnet46 => 3m,
        Haiku45 => 1m,
        _ => 3m, // default to Sonnet-tier pricing
    };

    /// <summary>
    /// Cost per million output tokens, keyed by model ID.
    /// </summary>
    public static decimal OutputCostPerMTok(string model) => model switch
    {
        Opus46 => 75m,
        Sonnet46 => 15m,
        Haiku45 => 5m,
        _ => 15m,
    };
}
