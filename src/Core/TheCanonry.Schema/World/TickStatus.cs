namespace TheCanonry.Schema.World;

public sealed class TickStatus
{
    public bool HasOccurred { get; }
    public int Tick { get; }

    private TickStatus(bool hasOccurred, int tick)
    {
        HasOccurred = hasOccurred;
        Tick = tick;
    }

    public static TickStatus Occurred(int tick) => new(true, tick);
    public static TickStatus NotOccurred() => new(false, 0);
}
