namespace TheCanonry.Schema.Domain;

public readonly record struct EntityStatus(string Value)
{
    public override string ToString() => Value;
}
