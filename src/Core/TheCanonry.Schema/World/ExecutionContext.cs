namespace TheCanonry.Schema.World;

public sealed class ExecutionContext
{
    public int Tick { get; }
    public ExecutionSource Source { get; }
    public string SourceId { get; }
    public bool Success { get; }
    public string Narration { get; }

    public ExecutionContext(int tick, ExecutionSource source, string sourceId, bool success, string narration)
    {
        Tick = tick;
        Source = source;
        SourceId = sourceId;
        Success = success;
        Narration = narration;
    }
}

public enum ExecutionSource
{
    Template,
    System,
    Action,
    Pressure,
    Seed,
    Framework
}
