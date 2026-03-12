namespace TheCanonry.Schema.Ids;

public readonly record struct EntityId(string Value)
{
    public override string ToString() => Value;
}
