namespace TheCanonry.Engine.Graph;

/// <summary>
/// Direction filter for relationship traversal.
/// Source means the entity is the source of the relationship.
/// Target means the entity is the target of the relationship.
/// Both means the entity can be either source or target.
/// </summary>
public enum Direction
{
    Source,
    Target,
    Both
}
