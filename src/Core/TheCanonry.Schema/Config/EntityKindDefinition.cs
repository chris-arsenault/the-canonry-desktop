namespace TheCanonry.Schema.Config;

using TheCanonry.Schema.Domain;

public enum EntityCategory
{
    Character, Collective, Place, Object, Concept, Power, Era, Event
}

public enum Polarity
{
    Positive, Neutral, Negative
}

public class SubtypeDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public bool IsAuthority { get; init; }
}

public class StatusDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public bool IsTerminal { get; init; }
    public Polarity Polarity { get; init; } = Polarity.Neutral;
    public string TransitionVerb { get; init; } = "";
}

public class EntityKindDefinition
{
    public required EntityKind Kind { get; init; }
    public required string Description { get; init; }
    public bool IsFramework { get; init; }
    public required EntityCategory Category { get; init; }
    public required IReadOnlyList<SubtypeDefinition> Subtypes { get; init; }
    public required IReadOnlyList<StatusDefinition> Statuses { get; init; }
    public EntityStatus DefaultStatus { get; init; }
    public IReadOnlyList<RequiredRelationshipRule> RequiredRelationships { get; init; } = [];
    public EntityKindStyle? Style { get; init; }
    public SemanticPlane? SemanticPlane { get; init; }
    public IReadOnlyList<string> VisualIdentityKeys { get; init; } = [];
}

public class RequiredRelationshipRule
{
    public required string Kind { get; init; }
    public required string Description { get; init; }
}

public class EntityKindStyle
{
    public required string Color { get; init; }
    public string Shape { get; init; } = "";
    public string DisplayName { get; init; } = "";
}
