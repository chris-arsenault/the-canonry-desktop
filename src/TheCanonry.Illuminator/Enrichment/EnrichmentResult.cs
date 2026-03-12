using TheCanonry.ApiClients.Llm;

namespace TheCanonry.Illuminator.Enrichment;

/// <summary>
/// The result of executing an enrichment task.
/// </summary>
public sealed record EnrichmentResult
{
    public required bool Success { get; init; }
    public object? Data { get; init; }
    public TokenUsage? TokenUsage { get; init; }
    public string? Error { get; init; }
}
