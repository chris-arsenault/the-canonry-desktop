namespace TheCanonry.Engine.Pressures;

using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Rules;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Engine.Runtime;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

/// <summary>
/// Converts DeclarativePressure configs into runtime Pressure objects
/// with executable growth functions backed by the metric evaluation system.
///
/// Port of: apps/lore-weave/lib/engine/pressureInterpreter.ts
/// </summary>
public static class PressureInterpreter
{
    /// <summary>
    /// Convert a single DeclarativePressure into a runtime Pressure.
    /// The growth function evaluates positive/negative feedback metric lists
    /// against the graph to compute the pressure delta per tick.
    /// </summary>
    public static Pressure Create(DeclarativePressure config, WorldRuntime runtime)
    {
        var positiveMetrics = config.Growth.PositiveFeedback;
        var negativeMetrics = config.Growth.NegativeFeedback;

        Func<WorldRuntime, double> growthFunction = rt =>
        {
            var graph = GetGraph(rt);
            var ruleContext = RuleContext.CreateForSystem(graph);
            var total = 0.0;

            for (var i = 0; i < positiveMetrics.Count; i++)
            {
                total += MetricEvaluator.Evaluate(positiveMetrics[i], ruleContext);
            }

            for (var i = 0; i < negativeMetrics.Count; i++)
            {
                total -= MetricEvaluator.Evaluate(negativeMetrics[i], ruleContext);
            }

            return total;
        };

        return new Pressure(
            id: config.Id,
            name: config.Name,
            initialValue: config.InitialValue,
            homeostasis: config.Homeostasis,
            growthFunction: growthFunction);
    }

    /// <summary>
    /// Load all pressures from a list of declarative pressure definitions.
    /// </summary>
    public static IReadOnlyList<Pressure> LoadPressures(
        IReadOnlyList<DeclarativePressure> configs,
        WorldRuntime runtime)
    {
        var pressures = new List<Pressure>(configs.Count);

        foreach (var config in configs)
        {
            pressures.Add(Create(config, runtime));
        }

        return pressures;
    }

    /// <summary>
    /// Extract the IGraph from a WorldRuntime for metric evaluation.
    /// Uses the runtime's public entity/relationship query methods
    /// to construct a graph view.
    /// </summary>
    private static IGraph GetGraph(WorldRuntime runtime)
    {
        // WorldRuntime delegates all graph operations to its internal IGraph.
        // For metric evaluation we need the IGraph interface directly.
        // We construct a RuntimeGraphAdapter that delegates to WorldRuntime.
        return new RuntimeGraphAdapter(runtime);
    }
}

/// <summary>
/// Adapter that implements IGraph by delegating to WorldRuntime.
/// This allows metric evaluation to work through the runtime facade
/// without exposing the internal graph directly.
/// </summary>
internal sealed class RuntimeGraphAdapter : IGraph
{
    private readonly WorldRuntime _runtime;

    public RuntimeGraphAdapter(WorldRuntime runtime)
    {
        _runtime = runtime;
    }

    public int Tick
    {
        get => _runtime.CurrentTick;
        set => _runtime.CurrentTick = value;
    }

    public Dictionary<string, double> Pressures
    {
        get
        {
            // Convert IReadOnlyDictionary to Dictionary for the interface
            var result = new Dictionary<string, double>();
            foreach (var kvp in _runtime.GetAllPressures())
                result[kvp.Key] = kvp.Value;
            return result;
        }
    }

    public Entity? GetEntity(EntityId id) => _runtime.GetEntity(id);
    public bool HasEntity(EntityId id) => _runtime.HasEntity(id);
    public int GetEntityCount(bool includeHistorical = false) => _runtime.GetEntityCount(includeHistorical);
    public IReadOnlyList<Entity> GetEntities(bool includeHistorical = false) => _runtime.GetEntities(includeHistorical);
    public IReadOnlyList<Entity> FindEntities(EntityCriteria criteria) => _runtime.FindEntities(criteria);
    public IReadOnlyList<Entity> GetEntitiesByKind(EntityKind kind, bool includeHistorical = false) => _runtime.GetEntitiesByKind(kind, includeHistorical);
    public IReadOnlyList<Entity> GetConnectedEntities(EntityId entityId, RelationshipKind? relationKind = null, Direction direction = Direction.Both, bool includeHistorical = false) => _runtime.GetConnectedEntities(entityId, relationKind, direction, includeHistorical);

    public EntityId CreateEntity(Entity entity) => _runtime.CreateEntity(entity);
    public bool UpdateEntity(EntityId id, Action<Entity> mutator) => _runtime.UpdateEntity(id, mutator);
    public bool DeleteEntity(EntityId id) => _runtime.DeleteEntity(id);

    public IReadOnlyList<Relationship> GetRelationships(bool includeHistorical = false) => _runtime.GetRelationships(includeHistorical);
    public IReadOnlyList<Relationship> FindRelationships(RelationshipCriteria criteria) => _runtime.FindRelationships(criteria);
    public IReadOnlyList<Relationship> GetEntityRelationships(EntityId entityId, Direction direction = Direction.Both, bool includeHistorical = false) => _runtime.GetEntityRelationships(entityId, direction, includeHistorical);
    public bool HasRelationship(EntityId srcId, EntityId dstId, RelationshipKind? kind = null) => _runtime.HasRelationship(srcId, dstId, kind);

    public bool AddRelationship(Relationship relationship) => _runtime.AddRelationship(relationship);
    public bool RemoveRelationship(EntityId srcId, EntityId dstId, RelationshipKind kind) => _runtime.RemoveRelationship(srcId, dstId, kind);
}
