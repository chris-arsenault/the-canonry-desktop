using TheCanonry.ApiClients.Llm;

namespace TheCanonry.ApiClients.Shared;

/// <summary>
/// Utility methods for estimating and formatting API costs.
/// </summary>
public static class CostCalculator
{
    private const double TokensPerWord = 1.3;

    /// <summary>
    /// Estimate token count from plain text using a word-count heuristic.
    /// </summary>
    public static int EstimateTokens(string text)
    {
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return (int)Math.Ceiling(words * TokensPerWord);
    }

    /// <summary>
    /// Estimate the cost of a text completion given a prompt and expected output tokens.
    /// </summary>
    public static decimal EstimateTextCost(string prompt, string model, int expectedOutputTokens = 300)
    {
        var inputTokens = EstimateTokens(prompt);
        var inputCost = inputTokens / 1_000_000m * LlmModel.InputCostPerMTok(model);
        var outputCost = expectedOutputTokens / 1_000_000m * LlmModel.OutputCostPerMTok(model);
        return inputCost + outputCost;
    }

    /// <summary>
    /// Calculate the actual cost from token usage reported by the API.
    /// </summary>
    public static decimal CalculateActualCost(int inputTokens, int outputTokens, string model)
    {
        var inputCost = inputTokens / 1_000_000m * LlmModel.InputCostPerMTok(model);
        var outputCost = outputTokens / 1_000_000m * LlmModel.OutputCostPerMTok(model);
        return inputCost + outputCost;
    }

    /// <summary>
    /// Estimate the cost of an image generation request.
    /// Uses flat per-image estimates keyed by model prefix.
    /// </summary>
    public static decimal EstimateImageCost(string model, string size = "1024x1024", string quality = "standard")
    {
        // GPT Image models: token-based pricing estimate
        if (model.StartsWith("gpt-image", StringComparison.OrdinalIgnoreCase))
        {
            const int estimatedInputTokens = 300;
            var estimatedOutputTokens = quality switch
            {
                "high" => 8000,
                "medium" => 6500,
                "low" => 4000,
                _ => 6500,
            };
            var inputCost = estimatedInputTokens / 1_000_000m * 5m;
            var outputCost = estimatedOutputTokens / 1_000_000m * 40m;
            return inputCost + outputCost;
        }

        // WaveSpeed: flat estimate
        if (model.StartsWith("wavespeed", StringComparison.OrdinalIgnoreCase) ||
            model.StartsWith("ws-", StringComparison.OrdinalIgnoreCase))
        {
            return 0.03m;
        }

        // DALL-E 3: per-image pricing
        if (model == "dall-e-3")
        {
            return quality == "hd"
                ? size switch
                {
                    "1792x1024" or "1024x1792" => 0.12m,
                    _ => 0.08m,
                }
                : size switch
                {
                    "1792x1024" or "1024x1792" => 0.08m,
                    _ => 0.04m,
                };
        }

        // DALL-E 2: per-image pricing
        if (model == "dall-e-2")
        {
            return size switch
            {
                "512x512" => 0.018m,
                "256x256" => 0.016m,
                _ => 0.02m,
            };
        }

        // BFL Flux: flat estimate
        return 0.04m;
    }

    /// <summary>
    /// Format a dollar cost for display.
    /// </summary>
    public static string FormatCost(decimal cost)
    {
        if (cost < 0.001m) return "<$0.001";
        if (cost < 0.01m) return $"${cost:F4}";
        if (cost < 1m) return $"${cost:F3}";
        return $"${cost:F2}";
    }
}
