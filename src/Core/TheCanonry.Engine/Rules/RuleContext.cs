namespace TheCanonry.Engine.Rules;

using TheCanonry.Engine.Graph;
using TheCanonry.Schema.World;

/// <summary>
/// Unified context for all rule evaluation: conditions, metrics, mutations,
/// filters, and selections. Holds references to the graph, current entity,
/// entity resolver, and named bindings.
/// </summary>
public sealed class RuleContext
{
    /// <summary>Graph access for all queries and mutations.</summary>
    public IGraph Graph { get; }

    /// <summary>Current simulation tick.</summary>
    public int Tick { get; }

    /// <summary>Entity resolver for references like $actor, $target.</summary>
    public IEntityResolver Resolver { get; }

    /// <summary>Current entity being evaluated (for per-entity conditions).</summary>
    public Entity? Self { get; }

    /// <summary>Named entity bindings for context-specific references.</summary>
    public IReadOnlyDictionary<string, Entity> Entities { get; }

    /// <summary>Named values for context-specific lookups (e.g., tag value sources).</summary>
    public IReadOnlyDictionary<string, object> Values { get; }

    /// <summary>Current era ID for era-related conditions.</summary>
    public string CurrentEraId { get; }

    private RuleContext(
        IGraph graph,
        int tick,
        IEntityResolver resolver,
        Entity? self,
        IReadOnlyDictionary<string, Entity> entities,
        IReadOnlyDictionary<string, object> values,
        string currentEraId)
    {
        Graph = graph;
        Tick = tick;
        Resolver = resolver;
        Self = self;
        Entities = entities;
        Values = values;
        CurrentEraId = currentEraId;
    }

    /// <summary>
    /// Create a RuleContext for system execution (no variable resolution).
    /// </summary>
    public static RuleContext CreateForSystem(IGraph graph, string currentEraId = "")
    {
        return new RuleContext(
            graph,
            graph.Tick,
            new SimpleEntityResolver(graph),
            self: null,
            entities: new Dictionary<string, Entity>(),
            values: new Dictionary<string, object>(),
            currentEraId: currentEraId);
    }

    /// <summary>
    /// Create a RuleContext for template/action execution with resolver and self entity.
    /// </summary>
    public static RuleContext CreateForTemplate(
        IGraph graph,
        IEntityResolver resolver,
        Entity? self,
        string currentEraId = "")
    {
        return new RuleContext(
            graph,
            graph.Tick,
            resolver,
            self,
            entities: new Dictionary<string, Entity>(),
            values: new Dictionary<string, object>(),
            currentEraId: currentEraId);
    }

    /// <summary>
    /// Create a RuleContext for action execution with bindings.
    /// </summary>
    public static RuleContext CreateForAction(
        IGraph graph,
        Dictionary<string, Entity> bindings,
        Entity self,
        Dictionary<string, object> values,
        string currentEraId = "")
    {
        var resolver = new BindingEntityResolver(graph, bindings);
        return new RuleContext(
            graph,
            graph.Tick,
            resolver,
            self,
            entities: bindings,
            values: values,
            currentEraId: currentEraId);
    }

    /// <summary>
    /// Create a new context with a different self entity.
    /// </summary>
    public RuleContext WithSelf(Entity self)
    {
        return new RuleContext(
            Graph,
            Tick,
            Resolver,
            self,
            Entities,
            Values,
            CurrentEraId);
    }

    /// <summary>
    /// Create a new context with additional entity bindings.
    /// </summary>
    public RuleContext WithEntities(IReadOnlyDictionary<string, Entity> entities)
    {
        return new RuleContext(
            Graph,
            Tick,
            Resolver,
            Self,
            entities,
            Values,
            CurrentEraId);
    }
}
