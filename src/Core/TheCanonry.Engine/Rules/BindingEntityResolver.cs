using TheCanonry.Engine.Graph;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

namespace TheCanonry.Engine.Rules;

/// <summary>
/// Entity resolver for action contexts. Resolves named bindings
/// like $actor, $target, as well as literal entity IDs.
/// </summary>
public sealed class BindingEntityResolver : IEntityResolver
{
    private readonly IGraph _graph;
    private readonly Dictionary<string, Entity> _bindings;
    private readonly Dictionary<string, HashSet<string>> _pathSets = [];

    public BindingEntityResolver(IGraph graph, Dictionary<string, Entity> bindings)
    {
        _graph = graph;
        _bindings = bindings;
    }

    public Entity? ResolveEntity(string reference)
    {
        if (!reference.StartsWith('$'))
            return _graph.GetEntity(new EntityId(reference));

        var varName = reference[1..];

        if (varName == "self")
            return null; // $self is handled by callers

        return _bindings.GetValueOrDefault(varName);
    }

    public void SetPathSet(string name, HashSet<string> ids) => _pathSets[name] = ids;

    public HashSet<string>? GetPathSet(string name) =>
        _pathSets.TryGetValue(name, out var set) ? set : null;
}
