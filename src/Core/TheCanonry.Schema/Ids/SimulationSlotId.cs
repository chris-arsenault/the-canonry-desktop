namespace TheCanonry.Schema.Ids;

public readonly record struct SimulationSlotId(int Value)
{
    public override string ToString() => Value.ToString();
}
