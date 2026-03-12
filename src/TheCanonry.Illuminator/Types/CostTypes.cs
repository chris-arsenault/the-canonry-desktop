namespace TheCanonry.Illuminator.Types;

/// <summary>
/// A single API cost entry recording one LLM or image generation call.
/// </summary>
public sealed record ApiCostEntry
{
    public required string Id { get; init; }
    public required EnrichmentType TaskType { get; init; }
    public required string Model { get; init; }
    public required int InputTokens { get; init; }
    public required int OutputTokens { get; init; }
    public required decimal Cost { get; init; }
    public required long Timestamp { get; init; }
    public string? EntityId { get; init; }
    public string? ChronicleId { get; init; }
    public string? ProjectId { get; init; }
}

/// <summary>
/// Aggregated cost summary across multiple API calls.
/// </summary>
public sealed record CostSummary
{
    public required decimal TotalEstimated { get; init; }
    public required decimal TotalActual { get; init; }
    public required int Count { get; init; }
    public required IReadOnlyDictionary<string, CostBreakdown> ByModel { get; init; }
}

/// <summary>
/// Cost breakdown for a single model or task type.
/// </summary>
public sealed record CostBreakdown
{
    public required decimal Estimated { get; init; }
    public required decimal Actual { get; init; }
    public required int Count { get; init; }
}
