namespace TheCanonry.Schema.Domain;

public readonly record struct RelationshipKind(string Value)
{
    public override string ToString() => Value;
}
