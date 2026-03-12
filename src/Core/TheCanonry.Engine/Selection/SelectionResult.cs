namespace TheCanonry.Engine.Selection;

using TheCanonry.Schema.World;

/// <summary>
/// Result of target selection with selected entities and diagnostic info.
/// </summary>
public class SelectionResult
{
    public IReadOnlyList<Entity> Existing { get; init; } = [];
    public SelectionDiagnostics Diagnostics { get; init; } = new();
}

/// <summary>
/// Diagnostic information about a selection operation.
/// </summary>
public class SelectionDiagnostics
{
    public int CandidatesEvaluated { get; init; }
    public double BestScore { get; init; }
    public double WorstScore { get; init; }
    public double AvgScore { get; init; }
}
