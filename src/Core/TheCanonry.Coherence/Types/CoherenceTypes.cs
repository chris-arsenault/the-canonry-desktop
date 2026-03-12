namespace TheCanonry.Coherence.Types;

/// <summary>
/// Severity level for coherence issues.
/// Errors indicate config that will cause runtime failures.
/// Warnings indicate config that degrades simulation quality.
/// </summary>
public enum CoherenceIssueSeverity
{
    Error,
    Warning
}

/// <summary>
/// A specific config element affected by a coherence issue.
/// </summary>
public sealed record AffectedItem(
    string Id,
    string Label,
    string Detail);

/// <summary>
/// A single coherence issue found during validation.
/// </summary>
public sealed record CoherenceIssue(
    /// <summary>Machine-readable rule identifier.</summary>
    string RuleId,

    /// <summary>Human-readable title.</summary>
    string Title,

    /// <summary>Explanation of the issue and its impact.</summary>
    string Message,

    /// <summary>Error or warning.</summary>
    CoherenceIssueSeverity Severity,

    /// <summary>Specific config elements affected.</summary>
    IReadOnlyList<AffectedItem> AffectedItems);

/// <summary>
/// Overall coherence validation result.
/// </summary>
public sealed record CoherenceReport(IReadOnlyList<CoherenceIssue> Issues)
{
    public IReadOnlyList<CoherenceIssue> Errors =>
        Issues.Where(i => i.Severity == CoherenceIssueSeverity.Error).ToList();

    public IReadOnlyList<CoherenceIssue> Warnings =>
        Issues.Where(i => i.Severity == CoherenceIssueSeverity.Warning).ToList();

    public bool IsClean => Issues.Count == 0;

    public CoherenceStatus Status =>
        Errors.Count > 0 ? CoherenceStatus.Error
        : Warnings.Count > 0 ? CoherenceStatus.Warning
        : CoherenceStatus.Clean;
}

public enum CoherenceStatus
{
    Clean,
    Warning,
    Error
}
