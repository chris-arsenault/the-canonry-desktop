namespace TheCanonry.Schema.Ids;

public readonly record struct RegionId(string Value)
{
    public override string ToString() => Value;
}
