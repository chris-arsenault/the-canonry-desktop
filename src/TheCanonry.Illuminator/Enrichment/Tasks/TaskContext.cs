using TheCanonry.ApiClients.Llm;
using TheCanonry.Illuminator.Types;
using TheCanonry.Schema.Config;

namespace TheCanonry.Illuminator.Enrichment.Tasks;

/// <summary>
/// Shared context available to all enrichment task executors.
/// Provides access to the LLM client, domain schema, and call type resolution.
/// </summary>
public sealed record TaskContext
{
    public required ILlmClient LlmClient { get; init; }
    public required DomainSchema Schema { get; init; }
    public required CancellationToken Ct { get; init; }

    /// <summary>
    /// Build an LlmRequest from a call type's default metadata,
    /// applying the system prompt and user prompt.
    /// </summary>
    public static LlmRequest BuildRequest(LlmCallType callType, string systemPrompt, string userPrompt)
    {
        var meta = LlmCallDefaults.GetMetadata(callType);
        return new LlmRequest
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            Model = meta.DefaultModel,
            MaxTokens = meta.DefaultMaxTokens > 0 ? meta.DefaultMaxTokens : 1024,
            Temperature = meta.DefaultTemperature ?? 1.0,
            ThinkingBudget = meta.DefaultThinkingBudget > 0 ? meta.DefaultThinkingBudget : null,
        };
    }
}
