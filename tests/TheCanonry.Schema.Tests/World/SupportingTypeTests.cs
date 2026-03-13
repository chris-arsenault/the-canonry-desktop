using TheCanonry.Schema.World;

namespace TheCanonry.Schema.Tests.World;

public class SupportingTypeTests
{
    [Fact]
    public void TickStatus_occurred_stores_tick()
    {
        var status = TickStatus.Occurred(42);
        Assert.True(status.HasOccurred);
        Assert.Equal(42, status.Tick);
    }

    [Fact]
    public void TickStatus_not_occurred()
    {
        var status = TickStatus.NotOccurred();
        Assert.False(status.HasOccurred);
        Assert.Equal(0, status.Tick);
    }

    [Fact]
    public void EventCause_with_cause()
    {
        var cause = EventCause.From("evt-1", "ent-1", "attack", true);
        Assert.True(cause.HasCause);
        Assert.Equal("evt-1", cause.EventId);
    }

    [Fact]
    public void EventCause_uncaused()
    {
        var cause = EventCause.Uncaused();
        Assert.False(cause.HasCause);
    }

    [Fact]
    public void SemanticCoordinates_has_xyz()
    {
        var coords = new SemanticCoordinates(1.0, 2.5, -0.5);
        Assert.Equal(1.0, coords.X);
        Assert.Equal(2.5, coords.Y);
        Assert.Equal(-0.5, coords.Z);
    }

    [Fact]
    public void EntityTags_get_set_contains()
    {
        var tags = new EntityTags();
        tags.Set("role", "leader");
        tags.Set("temporal", true);

        Assert.Equal("leader", tags.GetString("role")!);
        Assert.True(tags.GetBool("temporal"));
        Assert.True(tags.Contains("role"));
        Assert.False(tags.Contains("missing"));
    }
}
