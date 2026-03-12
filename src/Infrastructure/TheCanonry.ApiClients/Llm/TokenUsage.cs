namespace TheCanonry.ApiClients.Llm;

/// <summary>
/// Token usage reported by the API for a single request/response.
/// </summary>
public sealed record TokenUsage(int InputTokens, int OutputTokens);
