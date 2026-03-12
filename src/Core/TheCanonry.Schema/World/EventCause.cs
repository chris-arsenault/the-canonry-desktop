namespace TheCanonry.Schema.World;

public sealed class EventCause
{
    public bool HasCause { get; }
    public string EventId { get; }
    public string EntityId { get; }
    public string ActionType { get; }
    public bool Success { get; }

    private EventCause(bool hasCause, string eventId, string entityId, string actionType, bool success)
    {
        HasCause = hasCause;
        EventId = eventId;
        EntityId = entityId;
        ActionType = actionType;
        Success = success;
    }

    public static EventCause From(string eventId, string entityId, string actionType, bool success)
        => new(true, eventId, entityId, actionType, success);

    public static EventCause Uncaused()
        => new(false, "", "", "", true);
}
