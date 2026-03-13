using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;

namespace TheCanonry.Engine.Graph;

/// <summary>
/// Filter criteria for finding entities in the graph.
/// All non-null fields are ANDed together. Null fields are ignored.
/// </summary>
public sealed class EntityCriteria
{
    public EntityKind? Kind { get; init; }
    public string? Subtype { get; init; }
    public EntityStatus? Status { get; init; }
    public string? ProminenceLabel { get; init; }
    public CultureId? Culture { get; init; }
    public string? Tag { get; init; }
    public IReadOnlyList<EntityId>? Exclude { get; init; }
    public bool IncludeHistorical { get; init; }

    /// <summary>
    /// Create criteria with only the fields you want to filter by.
    /// </summary>
    public static EntityCriteria Create(
        EntityKind? kind = null,
        string? subtype = null,
        EntityStatus? status = null,
        string? prominenceLabel = null,
        CultureId? culture = null,
        string? tag = null,
        IReadOnlyList<EntityId>? exclude = null,
        bool includeHistorical = false)
    {
        return new EntityCriteria
        {
            Kind = kind,
            Subtype = subtype,
            Status = status,
            ProminenceLabel = prominenceLabel,
            Culture = culture,
            Tag = tag,
            Exclude = exclude,
            IncludeHistorical = includeHistorical
        };
    }
}
