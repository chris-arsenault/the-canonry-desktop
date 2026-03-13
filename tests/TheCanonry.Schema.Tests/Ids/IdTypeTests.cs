using TheCanonry.Schema.Ids;

namespace TheCanonry.Schema.Tests.Ids;

public class IdTypeTests
{
    [Fact]
    public void EntityId_wraps_string_value()
    {
        var id = new EntityId("entity-123");
        Assert.Equal("entity-123", id.Value);
        Assert.Equal("entity-123", id.ToString());
    }

    [Fact]
    public void EntityId_equality_is_by_value()
    {
        var a = new EntityId("abc");
        var b = new EntityId("abc");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Different_id_types_are_not_assignable()
    {
        var entityId = new EntityId("x");
        var chronicleId = new ChronicleId("x");
        Assert.NotEqual(entityId.GetType(), chronicleId.GetType());
    }

    [Fact]
    public void SimulationSlotId_wraps_int()
    {
        var slot = new SimulationSlotId(3);
        Assert.Equal(3, slot.Value);
    }
}
