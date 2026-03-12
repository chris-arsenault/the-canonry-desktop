namespace TheCanonry.Engine.Rules.Types;

/// <summary>
/// Canonical comparison operators used across conditions and metrics.
/// </summary>
public enum ComparisonOperator
{
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Equal,
    NotEqual
}

public static class ComparisonOperatorExtensions
{
    /// <summary>Apply this operator to two numeric operands.</summary>
    public static bool Apply(this ComparisonOperator op, double a, double b) => op switch
    {
        ComparisonOperator.GreaterThan => a > b,
        ComparisonOperator.LessThan => a < b,
        ComparisonOperator.GreaterThanOrEqual => a >= b,
        ComparisonOperator.LessThanOrEqual => a <= b,
        ComparisonOperator.Equal => Math.Abs(a - b) < 1e-9,
        ComparisonOperator.NotEqual => Math.Abs(a - b) >= 1e-9,
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unknown comparison operator")
    };
}
