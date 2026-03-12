namespace TheCanonry.Engine.Graph;

using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;

/// <summary>
/// Filter criteria for finding relationships in the graph.
/// All non-null fields are ANDed together. Null fields are ignored.
/// </summary>
public sealed class RelationshipCriteria
{
    public RelationshipKind? Kind { get; init; }
    public EntityId? Src { get; init; }
    public EntityId? Dst { get; init; }
    public string? Category { get; init; }
    public double MinStrength { get; init; }
    public bool IncludeHistorical { get; init; }

    /// <summary>
    /// Create criteria with only the fields you want to filter by.
    /// </summary>
    public static RelationshipCriteria Create(
        RelationshipKind? kind = null,
        EntityId? src = null,
        EntityId? dst = null,
        string? category = null,
        double minStrength = 0,
        bool includeHistorical = false)
    {
        return new RelationshipCriteria
        {
            Kind = kind,
            Src = src,
            Dst = dst,
            Category = category,
            MinStrength = minStrength,
            IncludeHistorical = includeHistorical
        };
    }
}
