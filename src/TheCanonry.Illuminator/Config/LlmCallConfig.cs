namespace TheCanonry.Illuminator.Config;

/// <summary>
/// Per-call-type LLM configuration overrides
/// </summary>
public sealed class LlmCallConfigMap
{
    public required Dictionary<string, LlmCallOverride> Overrides { get; init; }
}

public sealed record LlmCallOverride(
    string? Model,
    int? MaxTokens,
    double? Temperature,
    int? ThinkingBudget);
