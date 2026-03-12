namespace TheCanonry.Schema.Tests.World;

using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.Primitives;
using TheCanonry.Schema.World;

public class EntityTests
{
    private Entity CreateTestEntity(string id = "e-1", string kind = "faction")
    {
        return new Entity(
            id: new EntityId(id), kind: new EntityKind(kind), subtype: "merchant",
            name: "The Silver Guild", culture: new CultureId("northern"),
            eraId: new EraId("era-1"), coordinates: new SemanticCoordinates(0.5, 0.3, 0.1),
            createdBy: new ExecutionContext(10, ExecutionSource.Template, "tmpl-1", true, "spawned"),
            tick: 10);
    }

    [Fact]
    public void Constructor_sets_all_required_fields()
    {
        var entity = CreateTestEntity();
        Assert.Equal(new EntityId("e-1"), entity.Id);
        Assert.Equal(new EntityKind("faction"), entity.Kind);
        Assert.Equal("merchant", entity.Subtype);
        Assert.Equal("The Silver Guild", entity.Name);
        Assert.Equal(new CultureId("northern"), entity.Culture);
        Assert.Equal(new EraId("era-1"), entity.EraId);
        Assert.Equal(FrameworkPrimitives.Statuses.Active, entity.Status);
        Assert.Equal(10, entity.CreatedAtTick);
        Assert.Equal(10, entity.UpdatedAtTick);
    }

    [Fact]
    public void Description_and_summary_start_empty()
    {
        var entity = CreateTestEntity();
        Assert.Equal("", entity.Description);
        Assert.Equal("", entity.Summary);
    }

    [Fact]
    public void UpdateStatus_changes_status_and_tick()
    {
        var entity = CreateTestEntity();
        entity.UpdateStatus(FrameworkPrimitives.Statuses.Historical, 25);
        Assert.Equal(FrameworkPrimitives.Statuses.Historical, entity.Status);
        Assert.Equal(25, entity.UpdatedAtTick);
    }

    [Fact]
    public void Links_are_empty_initially()
    {
        var entity = CreateTestEntity();
        Assert.Empty(entity.Links);
    }

    [Fact]
    public void Temporal_span_starts_at_creation_tick()
    {
        var entity = CreateTestEntity();
        Assert.Equal(10, entity.Temporal.StartTick);
        Assert.False(entity.Temporal.End.HasOccurred);
    }
}
