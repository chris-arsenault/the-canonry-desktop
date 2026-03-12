namespace TheCanonry.Schema.World;

public class TemporalSpan
{
    public int StartTick { get; }
    public TickStatus End { get; private set; }

    public TemporalSpan(int startTick)
    {
        StartTick = startTick;
        End = TickStatus.NotOccurred();
    }

    public void EndAt(int tick) => End = TickStatus.Occurred(tick);

    public bool IsActive => !End.HasOccurred;
}
