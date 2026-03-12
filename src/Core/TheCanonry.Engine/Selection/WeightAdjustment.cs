namespace TheCanonry.Engine.Selection;

/// <summary>
/// Result of a dynamic weight calculation for a single template.
/// </summary>
public class WeightAdjustment
{
    public string TemplateId { get; init; } = "";
    public double BaseWeight { get; init; }
    public double AdjustedWeight { get; init; }
    public double AdjustmentFactor { get; init; }
    public string Reason { get; init; } = "";
}
