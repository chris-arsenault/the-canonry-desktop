namespace TheCanonry.Schema.Domain;

public readonly record struct EntityKind(string Value)
{
    public override string ToString() => Value;
}
