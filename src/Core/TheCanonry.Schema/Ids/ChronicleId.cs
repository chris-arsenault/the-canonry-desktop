namespace TheCanonry.Schema.Ids;

public readonly record struct ChronicleId(string Value)
{
    public override string ToString() => Value;
}
