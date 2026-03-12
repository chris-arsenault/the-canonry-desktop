namespace TheCanonry.Schema.World;

using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.Primitives;

public class Entity
{
    public EntityId Id { get; }
    public EntityKind Kind { get; }
    public string Subtype { get; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Summary { get; private set; }
    public string NarrativeHint { get; private set; }
    public bool LockedSummary { get; private set; }
    public EntityStatus Status { get; private set; }
    public Prominence Prominence { get; private set; }
    public CultureId Culture { get; }
    public EraId EraId { get; }
    public EntityTags Tags { get; }
    public SemanticCoordinates Coordinates { get; private set; }
    public TemporalSpan Temporal { get; }
    public CatalystState Catalyst { get; }
    public RegionId RegionId { get; private set; }
    public IReadOnlyList<RegionId> AllRegionIds { get; private set; }
    public ExecutionContext CreatedBy { get; }
    public int CreatedAtTick { get; }
    public int UpdatedAtTick { get; private set; }

    private readonly List<Relationship> _links = [];
    public IReadOnlyList<Relationship> Links => _links;

    public Entity(
        EntityId id, EntityKind kind, string subtype, string name,
        CultureId culture, EraId eraId, SemanticCoordinates coordinates,
        ExecutionContext createdBy, int tick)
    {
        Id = id;
        Kind = kind;
        Subtype = subtype;
        Name = name;
        Description = "";
        Summary = "";
        NarrativeHint = "";
        LockedSummary = false;
        Status = FrameworkPrimitives.Statuses.Active;
        Prominence = new Prominence(0.0);
        Culture = culture;
        EraId = eraId;
        Tags = new EntityTags();
        Coordinates = coordinates;
        Temporal = new TemporalSpan(tick);
        Catalyst = new CatalystState(false);
        RegionId = new RegionId("");
        AllRegionIds = [];
        CreatedBy = createdBy;
        CreatedAtTick = tick;
        UpdatedAtTick = tick;
    }

    public void UpdateStatus(EntityStatus newStatus, int tick)
    {
        Status = newStatus;
        UpdatedAtTick = tick;
    }

    public void SetProminence(Prominence prominence, int tick)
    {
        Prominence = prominence;
        UpdatedAtTick = tick;
    }

    public void SetDescription(string description, int tick)
    {
        Description = description;
        UpdatedAtTick = tick;
    }

    public void SetSummary(string summary, int tick)
    {
        Summary = summary;
        UpdatedAtTick = tick;
    }

    public void SetNarrativeHint(string hint, int tick)
    {
        NarrativeHint = hint;
        UpdatedAtTick = tick;
    }

    public void LockSummary() => LockedSummary = true;

    public void SetName(string name, int tick)
    {
        Name = name;
        UpdatedAtTick = tick;
    }

    public void SetCoordinates(SemanticCoordinates coordinates, int tick)
    {
        Coordinates = coordinates;
        UpdatedAtTick = tick;
    }

    public void SetRegion(RegionId regionId, IReadOnlyList<RegionId> allRegionIds, int tick)
    {
        RegionId = regionId;
        AllRegionIds = allRegionIds;
        UpdatedAtTick = tick;
    }

    public void AddLink(Relationship relationship) => _links.Add(relationship);

    public void RemoveLink(Relationship relationship) => _links.Remove(relationship);

    public bool IsConnectedTo(EntityId other) =>
        _links.Any(l => l.SourceId == other || l.TargetId == other);
}
