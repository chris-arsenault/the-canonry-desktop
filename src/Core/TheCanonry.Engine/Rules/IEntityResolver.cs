using TheCanonry.Schema.World;

namespace TheCanonry.Engine.Rules;

/// <summary>
/// Interface for resolving entity references (like $actor, $target, literal IDs)
/// in rule evaluation contexts.
/// </summary>
public interface IEntityResolver
{
    /// <summary>
    /// Resolve a reference to an entity.
    /// References starting with '$' are variable references (e.g. "$actor", "$target").
    /// Other strings are treated as literal entity IDs.
    /// </summary>
    Entity? ResolveEntity(string reference);

    /// <summary>Store intermediate path results for graph path evaluation.</summary>
    void SetPathSet(string name, HashSet<string> ids);

    /// <summary>Get stored path results.</summary>
    HashSet<string>? GetPathSet(string name);
}
