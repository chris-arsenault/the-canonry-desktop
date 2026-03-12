namespace TheCanonry.Schema.Domain;

public readonly record struct Prominence(double Value)
{
    public string Label => Value switch
    {
        < 1.0 => "Forgotten",
        < 2.0 => "Marginal",
        < 3.0 => "Recognized",
        < 4.0 => "Renowned",
        _ => "Mythic"
    };

    public override string ToString() => $"{Value:F1} ({Label})";
}
