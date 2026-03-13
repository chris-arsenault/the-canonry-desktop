using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Primitives;

namespace TheCanonry.Schema.Tests.Primitives;

public class FrameworkPrimitivesTests
{
    [Fact]
    public void Framework_entity_kinds_are_defined()
    {
        Assert.Equal("era", FrameworkPrimitives.EntityKinds.Era.Value);
        Assert.Equal("occurrence", FrameworkPrimitives.EntityKinds.Occurrence.Value);
    }

    [Fact]
    public void Framework_relationship_kinds_are_defined()
    {
        Assert.Equal("supersedes", FrameworkPrimitives.RelationshipKinds.Supersedes.Value);
        Assert.Equal("part_of", FrameworkPrimitives.RelationshipKinds.PartOf.Value);
        Assert.Equal("active_during", FrameworkPrimitives.RelationshipKinds.ActiveDuring.Value);
        Assert.Equal("participant_in", FrameworkPrimitives.RelationshipKinds.ParticipantIn.Value);
        Assert.Equal("epicenter_of", FrameworkPrimitives.RelationshipKinds.EpicenterOf.Value);
        Assert.Equal("triggered_by", FrameworkPrimitives.RelationshipKinds.TriggeredBy.Value);
        Assert.Equal("created_during", FrameworkPrimitives.RelationshipKinds.CreatedDuring.Value);
    }

    [Fact]
    public void Framework_statuses_are_defined()
    {
        Assert.Equal("active", FrameworkPrimitives.Statuses.Active.Value);
        Assert.Equal("historical", FrameworkPrimitives.Statuses.Historical.Value);
        Assert.Equal("current", FrameworkPrimitives.Statuses.Current.Value);
        Assert.Equal("future", FrameworkPrimitives.Statuses.Future.Value);
        Assert.Equal("subsumed", FrameworkPrimitives.Statuses.Subsumed.Value);
    }

    [Fact]
    public void Framework_relationship_default_strengths_are_defined()
    {
        Assert.Equal(0.7, FrameworkPrimitives.GetDefaultRelationshipStrength(FrameworkPrimitives.RelationshipKinds.Supersedes));
        Assert.Equal(1.0, FrameworkPrimitives.GetDefaultRelationshipStrength(FrameworkPrimitives.RelationshipKinds.ParticipantIn));
    }

    [Fact]
    public void IsFrameworkEntityKind_identifies_framework_kinds()
    {
        Assert.True(FrameworkPrimitives.IsFrameworkEntityKind(new EntityKind("era")));
        Assert.True(FrameworkPrimitives.IsFrameworkEntityKind(new EntityKind("occurrence")));
        Assert.False(FrameworkPrimitives.IsFrameworkEntityKind(new EntityKind("faction")));
    }
}
