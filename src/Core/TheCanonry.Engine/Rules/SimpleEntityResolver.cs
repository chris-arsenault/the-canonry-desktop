using TheCanonry.Engine.Graph;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

namespace TheCanonry.Engine.Rules;

/// <summary>
/// Simple entity resolver that only handles literal entity IDs.
/// Suitable for system contexts where there are no variable references.
/// </summary>
public sealed class SimpleEntityResolver : IEntityResolver
{
    private readonly IGraph _graph;
    private readonly Dictionary<string, HashSet<string>> _pathSets = [];

    public SimpleEntityResolver(IGraph graph)
    {
        _graph = graph;
    }

    public Entity? ResolveEntity(string reference)
    {
        if (reference.StartsWith('$'))
            return null;

        return _graph.GetEntity(new EntityId(reference));
    }

    public void SetPathSet(string name, HashSet<string> ids) => _pathSets[name] = ids;

    public HashSet<string>? GetPathSet(string name) =>
        _pathSets.TryGetValue(name, out var set) ? set : null;
}
