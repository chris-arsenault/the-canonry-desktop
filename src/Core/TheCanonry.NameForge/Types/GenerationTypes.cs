namespace TheCanonry.NameForge.Types;

/// <summary>
/// Generation request: input for name generation.
/// </summary>
public sealed record GenerateRequest
{
    /// <summary>Culture to use.</summary>
    public required string CultureId { get; init; }

    /// <summary>Profile ID (optional, uses first matching profile if not specified).</summary>
    public string ProfileId { get; init; } = "";

    /// <summary>Entity kind for condition matching.</summary>
    public string Kind { get; init; } = "";

    /// <summary>Entity subtype for condition matching.</summary>
    public string Subtype { get; init; } = "";

    /// <summary>Prominence level for condition matching.</summary>
    public string Prominence { get; init; } = "";

    /// <summary>Tags for condition matching.</summary>
    public string[] Tags { get; init; } = [];

    /// <summary>Context values for context: slots.</summary>
    public Dictionary<string, string> Context { get; init; } = [];

    /// <summary>Number of names to generate.</summary>
    public int Count { get; init; } = 10;

    /// <summary>Random seed for reproducibility.</summary>
    public string Seed { get; init; } = "";
}

/// <summary>
/// Debug info for why a strategy group matched or didn't match.
/// </summary>
public sealed record GroupMatchDebug(
    string GroupName,
    bool Matched,
    string Reason,
    int Priority);

/// <summary>
/// Debug info for a single generated name.
/// </summary>
public sealed record NameDebugInfo
{
    /// <summary>Which strategy group was used.</summary>
    public required string GroupUsed { get; init; }

    /// <summary>Which strategy within the group was selected.</summary>
    public required string StrategyUsed { get; init; }

    /// <summary>What type of strategy (grammar, phonotactic, fallback).</summary>
    public required string StrategyType { get; init; }

    /// <summary>For grammar strategies, which grammar ID.</summary>
    public string GrammarId { get; init; } = "";

    /// <summary>For phonotactic strategies, which domain ID.</summary>
    public string DomainId { get; init; } = "";

    /// <summary>Debug info for all groups showing match status.</summary>
    public GroupMatchDebug[] GroupMatching { get; init; } = [];
}

/// <summary>
/// Generation result: output from name generation.
/// </summary>
public sealed record GenerateResult
{
    /// <summary>Generated names.</summary>
    public required string[] Names { get; init; }

    /// <summary>Strategy usage counts.</summary>
    public required Dictionary<string, int> StrategyUsage { get; init; }

    /// <summary>Debug info for each name (same length as Names array).</summary>
    public NameDebugInfo[] DebugInfo { get; init; } = [];
}
