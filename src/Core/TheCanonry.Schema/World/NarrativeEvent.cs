namespace TheCanonry.Schema.World;

using TheCanonry.Schema.Ids;

public class NarrativeEvent
{
    public string Id { get; }
    public int Tick { get; }
    public EraId EraId { get; }
    public NarrativeEventKind EventKind { get; }
    public double Significance { get; }
    public NarrativeEntityRef Subject { get; }
    public string Action { get; }
    public string Description { get; }
    public EventCause CausedBy { get; }
    public IReadOnlyList<string> NarrativeTags { get; }

    private readonly List<ParticipantEffect> _participantEffects = [];
    public IReadOnlyList<ParticipantEffect> ParticipantEffects => _participantEffects;

    public NarrativeEvent(
        string id, int tick, EraId eraId, NarrativeEventKind eventKind,
        double significance, NarrativeEntityRef subject, string action,
        string description, EventCause causedBy, IReadOnlyList<string>? narrativeTags = null)
    {
        Id = id;
        Tick = tick;
        EraId = eraId;
        EventKind = eventKind;
        Significance = significance;
        Subject = subject;
        Action = action;
        Description = description;
        CausedBy = causedBy;
        NarrativeTags = narrativeTags ?? [];
    }

    public void AddParticipantEffect(ParticipantEffect effect) => _participantEffects.Add(effect);
}
