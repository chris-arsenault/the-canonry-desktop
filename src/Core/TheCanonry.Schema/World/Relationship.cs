namespace TheCanonry.Schema.World;

using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.Primitives;

public class Relationship
{
    public EntityId SourceId { get; }
    public EntityId TargetId { get; }
    public RelationshipKind Kind { get; }
    public double Strength { get; private set; }
    public double Distance { get; private set; }
    public string Category { get; }
    public EntityStatus Status { get; private set; }
    public TickStatus Archived { get; private set; }
    public string CatalyzedBy { get; }
    public ExecutionContext CreatedBy { get; }
    public int CreatedAtTick { get; }

    public Relationship(
        EntityId sourceId, EntityId targetId, RelationshipKind kind,
        double strength, double distance, string category,
        ExecutionContext createdBy, int tick, string catalyzedBy = "")
    {
        SourceId = sourceId;
        TargetId = targetId;
        Kind = kind;
        Strength = strength;
        Distance = distance;
        Category = category;
        Status = FrameworkPrimitives.Statuses.Active;
        Archived = TickStatus.NotOccurred();
        CatalyzedBy = catalyzedBy;
        CreatedBy = createdBy;
        CreatedAtTick = tick;
    }

    public void Reinforce(double amount) => Strength = Math.Min(1.0, Strength + amount);

    public void Decay(double rate) => Strength = Math.Max(0.0, Strength * (1.0 - rate));

    public void Archive(int tick)
    {
        Status = FrameworkPrimitives.Statuses.Historical;
        Archived = TickStatus.Occurred(tick);
    }
}
