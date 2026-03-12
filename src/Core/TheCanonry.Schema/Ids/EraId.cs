namespace TheCanonry.Schema.Ids;

public readonly record struct EraId(string Value)
{
    public override string ToString() => Value;
}
