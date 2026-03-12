namespace TheCanonry.Schema.Tests.World;

using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

public class NarrativeEventTests
{
    [Fact]
    public void NarrativeEvent_constructor_sets_fields()
    {
        var subject = new NarrativeEntityRef(new EntityId("e-1"), "Guild", new EntityKind("faction"), "merchant");
        var evt = new NarrativeEvent(
            id: "evt-1", tick: 15, eraId: new EraId("era-1"),
            eventKind: NarrativeEventKind.RelationshipFormed, significance: 0.7,
            subject: subject, action: "formed alliance",
            description: "The guild allied with the crown", causedBy: EventCause.Uncaused());
        Assert.Equal("evt-1", evt.Id);
        Assert.Equal(15, evt.Tick);
        Assert.Equal(NarrativeEventKind.RelationshipFormed, evt.EventKind);
        Assert.Equal(0.7, evt.Significance);
    }

    [Fact]
    public void EntityEffect_pattern_matching_is_exhaustive()
    {
        EntityEffect effect = new TagGainedEffect("leader", "gained leadership", "");
        var description = effect switch
        {
            CreatedEffect e => e.Description,
            EndedEffect e => e.Description,
            RelationshipFormedEffect e => e.Description,
            RelationshipEndedEffect e => e.Description,
            TagGainedEffect e => e.Description,
            TagLostEffect e => e.Description,
            FieldChangedEffect e => e.Description,
            _ => throw new InvalidOperationException("Unexpected effect type"),
        };
        Assert.Equal("gained leadership", description);
    }
}
