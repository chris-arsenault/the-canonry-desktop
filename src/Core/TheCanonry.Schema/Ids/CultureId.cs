namespace TheCanonry.Schema.Ids;

public readonly record struct CultureId(string Value)
{
    public override string ToString() => Value;
}
