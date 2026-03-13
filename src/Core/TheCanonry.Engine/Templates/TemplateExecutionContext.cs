using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Rules;
using TheCanonry.Engine.Runtime;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

namespace TheCanonry.Engine.Templates;

/// <summary>
/// Execution context maintained during template expansion.
/// Stores resolved variables, tracks created entities by ref, and provides
/// entity resolution for the Rules evaluators via IEntityResolver.
/// </summary>
public sealed class TemplateExecutionContext : IEntityResolver
{
    private readonly WorldRuntime _runtime;
    private readonly IGraph _graph;
    private readonly Dictionary<string, Entity> _variables = [];
    private readonly Dictionary<string, List<Entity>> _createdEntitiesByRef = [];
    private readonly List<Entity> _createdEntities = [];
    private readonly Dictionary<string, int> _entityRefToIndex = [];
    private readonly Dictionary<string, HashSet<string>> _pathSets = [];

    public TemplateExecutionContext(WorldRuntime runtime, IGraph graph)
    {
        _runtime = runtime;
        _graph = graph;
    }

    /// <summary>The target entity this template is being applied to.</summary>
    public Entity? Target { get; set; }

    /// <summary>The template ID being executed (for error context).</summary>
    public string TemplateId { get; set; } = "";

    /// <summary>The runtime for graph and pressure queries.</summary>
    public WorldRuntime Runtime => _runtime;

    /// <summary>The graph for direct access by rule evaluators.</summary>
    public IGraph Graph => _graph;

    // =========================================================================
    // VARIABLE MANAGEMENT
    // =========================================================================

    /// <summary>Store a resolved variable (single entity).</summary>
    public void SetVariable(string name, Entity entity)
    {
        // Strip leading $ if present for internal storage
        var key = name.StartsWith('$') ? name[1..] : name;
        _variables[key] = entity;
    }

    /// <summary>Get a resolved variable entity by name.</summary>
    public Entity? GetVariable(string name)
    {
        var key = name.StartsWith('$') ? name[1..] : name;
        return _variables.GetValueOrDefault(key);
    }

    /// <summary>All resolved variable bindings (name without $ prefix -> Entity).</summary>
    public IReadOnlyDictionary<string, Entity> Variables => _variables;

    // =========================================================================
    // CREATED ENTITY TRACKING
    // =========================================================================

    /// <summary>Register a created entity under its entityRef label.</summary>
    public void TrackCreatedEntity(string entityRef, Entity entity, int indexInResultList)
    {
        if (!_createdEntitiesByRef.TryGetValue(entityRef, out var list))
        {
            list = [];
            _createdEntitiesByRef[entityRef] = list;
        }
        list.Add(entity);
        _createdEntities.Add(entity);

        // Track the first entity index for each ref
        if (!_entityRefToIndex.ContainsKey(entityRef))
            _entityRefToIndex[entityRef] = indexInResultList;
    }

    /// <summary>Get created entities by their entityRef label.</summary>
    public IReadOnlyList<Entity>? GetCreatedEntities(string entityRef)
    {
        return _createdEntitiesByRef.GetValueOrDefault(entityRef);
    }

    /// <summary>All created entities in order.</summary>
    public IReadOnlyList<Entity> AllCreatedEntities => _createdEntities;

    /// <summary>Map of entityRef to index in the result entities list.</summary>
    public IReadOnlyDictionary<string, int> EntityRefToIndex => _entityRefToIndex;

    // =========================================================================
    // IEntityResolver IMPLEMENTATION
    // =========================================================================

    /// <summary>
    /// Resolve an entity reference.
    /// Supports: "$target", "$variableName", "$entityRef" (created entities), literal entity IDs.
    /// </summary>
    public Entity? ResolveEntity(string reference)
    {
        if (string.IsNullOrEmpty(reference)) return Target;

        if (!reference.StartsWith('$'))
            return _graph.GetEntity(new EntityId(reference));

        var varName = reference[1..];

        if (varName == "target")
            return Target;

        if (varName == "self")
            return null; // $self is handled by callers at the RuleContext level

        // Check variables
        if (_variables.TryGetValue(varName, out var variable))
            return variable;

        // Check created entities by ref (return first)
        if (_createdEntitiesByRef.TryGetValue(reference, out var created) && created.Count > 0)
            return created[0];

        return null;
    }

    /// <summary>Store intermediate path results for graph path evaluation.</summary>
    public void SetPathSet(string name, HashSet<string> ids) => _pathSets[name] = ids;

    /// <summary>Get stored path set.</summary>
    public HashSet<string>? GetPathSet(string name) =>
        _pathSets.GetValueOrDefault(name);

    // =========================================================================
    // RULE CONTEXT CREATION
    // =========================================================================

    /// <summary>
    /// Create a RuleContext suitable for condition/filter/mutation evaluation,
    /// using this execution context's variable bindings.
    /// </summary>
    public RuleContext CreateRuleContext()
    {
        var bindings = new Dictionary<string, Entity>(_variables);
        if (Target is not null)
            bindings["target"] = Target;

        var currentEraId = _runtime.CurrentEra?.Id.Value ?? "";

        return Target is not null
            ? RuleContext.CreateForAction(_graph, bindings, Target, new Dictionary<string, object>(), currentEraId)
            : RuleContext.CreateForSystem(_graph, currentEraId);
    }
}
