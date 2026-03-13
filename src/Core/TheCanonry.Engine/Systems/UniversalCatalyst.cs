using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Rules;
using TheCanonry.Engine.Runtime;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Primitives;
using TheCanonry.Schema.World;

namespace TheCanonry.Engine.Systems;

/// <summary>
/// Enables agents to perform domain-defined actions.
/// Success chance is based on entity prominence.
///
/// Flow per tick:
/// 1. Find all entities that can act (catalyst.canAct = true)
/// 2. For each agent, roll for action attempt based on prominence
/// 3. Select action from available actions, weighted by pressures
/// 4. Execute action via declarative handler (success chance based on prominence)
/// 5. On success: apply success mutations (modify tags, create relationships, etc.)
/// 6. On failure: apply failure mutations (prominence decrease, etc.)
/// 7. Record catalyzedBy attribution
/// </summary>
public sealed class UniversalCatalyst : ISimulationSystem
{
    private readonly UniversalCatalystConfig _config;

    public UniversalCatalyst(UniversalCatalystConfig config, WorldRuntime runtime)
    {
        _config = config;
        Runtime = runtime;
    }

    public string Id => _config.Id;
    public string Name => _config.Name;

    /// <summary>The runtime this system operates on.</summary>
    internal WorldRuntime Runtime { get; }

    /// <summary>The typed configuration for this system.</summary>
    internal UniversalCatalystConfig Config => _config;

    public void Initialize() { }

    public Task<SystemResult> ApplyAsync(double modifier)
    {
        var runtime = Runtime;
        var actionAttemptRate = _config.ActionAttemptRate;
        var prominenceUpChance = _config.ProminenceUpChanceOnSuccess;
        var prominenceDownChance = _config.ProminenceDownChanceOnFailure;

        // Find all agents (entities that can act)
        var allEntities = runtime.GetEntities();
        var agents = new List<Entity>();
        foreach (var entity in allEntities)
        {
            if (entity.Catalyst.CanAct)
                agents.Add(entity);
        }

        if (agents.Count == 0)
        {
            return Task.FromResult(new SystemResult(
                [], [], [], [],
                new Dictionary<string, double>(),
                "Catalyst system dormant (no agents can act)",
                new Dictionary<string, object?>(),
                new Dictionary<string, string>()));
        }

        var relationshipsAdded = new List<RelationshipAdded>();
        var relationshipsAdjusted = new List<RelationshipAdjusted>();
        var entitiesModified = new List<EntityModified>();
        var pressureChanges = new Dictionary<string, double>();
        var narrations = new Dictionary<string, string>();
        var actionsAttempted = 0;
        var actionsSucceeded = 0;

        foreach (var agent in agents)
        {
            // Calculate attempt chance based on prominence
            var prominenceMultiplier = CatalystHelpers.GetProminenceMultiplier(agent.Prominence.Value);
            var attemptChance = Math.Min(1.0, actionAttemptRate * prominenceMultiplier * modifier);

            // Roll for action attempt
            if (runtime.NextRandomDouble() >= attemptChance) continue;

            actionsAttempted++;

            // Select an action (simplified: pick tag-based action from entity tags)
            var actionId = $"action:{agent.Kind.Value}:{agent.Id.Value}";
            var actionCtx = new ActionContext("action", actionId, true);

            // Compute success chance based on prominence
            var baseSuccessChance = 0.5;
            var successChance = Math.Min(0.95, baseSuccessChance * prominenceMultiplier);

            // Roll for success
            var success = runtime.NextRandomDouble() < successChance;

            if (success)
            {
                actionsSucceeded++;

                // Success mutations: potentially create a relationship or modify tags
                var targetEntities = runtime.GetEntities();
                if (targetEntities.Count > 1)
                {
                    // Pick a random target that isn't the agent
                    Entity? target = null;
                    for (var attempts = 0; attempts < 5; attempts++)
                    {
                        var candidate = targetEntities[runtime.NextRandomInt(targetEntities.Count)];
                        if (candidate.Id != agent.Id)
                        {
                            target = candidate;
                            break;
                        }
                    }

                    if (target is not null)
                    {
                        // Apply success: set a catalyst tag on the agent
                        var changes = new Dictionary<string, object?>
                        {
                            ["catalyst_acted"] = true,
                            ["updatedAt"] = runtime.CurrentTick
                        };
                        entitiesModified.Add(new EntityModified(agent.Id, changes, actionCtx, actionId));

                        // Prominence increase chance
                        if (runtime.NextRandomDouble() < prominenceUpChance
                            && !agent.Tags.Contains(FrameworkPrimitives.Tags.ProminenceLocked))
                        {
                            var newProminence = ProminenceHelper.Clamp(agent.Prominence.Value + 0.5);
                            var promChanges = new Dictionary<string, object?> { ["prominence"] = newProminence };
                            entitiesModified.Add(new EntityModified(agent.Id, promChanges, actionCtx, actionId));

                            runtime.UpdateEntity(agent.Id, e =>
                                e.SetProminence(new Prominence(newProminence), runtime.CurrentTick));
                        }
                    }
                }
            }
            else
            {
                // Failure mutations: prominence decrease
                var failActionCtx = new ActionContext("action", actionId, false);

                if (runtime.NextRandomDouble() < prominenceDownChance
                    && !agent.Tags.Contains(FrameworkPrimitives.Tags.ProminenceLocked))
                {
                    var newProminence = ProminenceHelper.Clamp(agent.Prominence.Value - 0.5);
                    var promChanges = new Dictionary<string, object?> { ["prominence"] = newProminence };
                    entitiesModified.Add(new EntityModified(agent.Id, promChanges, failActionCtx, actionId));

                    runtime.UpdateEntity(agent.Id, e =>
                        e.SetProminence(new Prominence(newProminence), runtime.CurrentTick));
                }
            }
        }

        string description;
        if (actionsSucceeded > 0)
            description = $"Agents shape the world ({actionsSucceeded}/{actionsAttempted} actions succeeded)";
        else if (actionsAttempted > 0)
            description = $"Agents attempt to act (all {actionsAttempted} failed)";
        else
            description = "Agents dormant this cycle";

        return Task.FromResult(new SystemResult(
            relationshipsAdded,
            relationshipsAdjusted,
            [],
            entitiesModified,
            pressureChanges,
            description,
            new Dictionary<string, object?>(),
            narrations));
    }
}
