namespace TheCanonry.Schema.Tests.World;

using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

public class RelationshipTests
{
    private Relationship CreateTestRelationship()
    {
        return new Relationship(
            sourceId: new EntityId("e-1"), targetId: new EntityId("e-2"),
            kind: new RelationshipKind("alliance"), strength: 0.8, distance: 0.2,
            category: "political",
            createdBy: new ExecutionContext(5, ExecutionSource.System, "sys-1", true, "formed"),
            tick: 5);
    }

    [Fact]
    public void Constructor_sets_all_fields()
    {
        var rel = CreateTestRelationship();
        Assert.Equal(new EntityId("e-1"), rel.SourceId);
        Assert.Equal(new EntityId("e-2"), rel.TargetId);
        Assert.Equal(new RelationshipKind("alliance"), rel.Kind);
        Assert.Equal(0.8, rel.Strength);
        Assert.Equal(0.2, rel.Distance);
        Assert.Equal("political", rel.Category);
        Assert.Equal(new EntityStatus("active"), rel.Status);
        Assert.False(rel.Archived.HasOccurred);
    }

    [Fact]
    public void Reinforce_increases_strength()
    {
        var rel = CreateTestRelationship();
        rel.Reinforce(0.1);
        Assert.Equal(0.9, rel.Strength, precision: 5);
    }

    [Fact]
    public void Decay_decreases_strength()
    {
        var rel = CreateTestRelationship();
        rel.Decay(0.5);
        Assert.Equal(0.4, rel.Strength, precision: 5);
    }

    [Fact]
    public void Archive_sets_status_and_tick()
    {
        var rel = CreateTestRelationship();
        rel.Archive(20);
        Assert.Equal(new EntityStatus("historical"), rel.Status);
        Assert.True(rel.Archived.HasOccurred);
        Assert.Equal(20, rel.Archived.Tick);
    }
}
