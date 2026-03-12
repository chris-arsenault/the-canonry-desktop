namespace TheCanonry.Engine.Engine;

using TheCanonry.Engine.Pressures;
using TheCanonry.Engine.Runtime;

// =============================================================================
// TICK RESULT
// =============================================================================

/// <summary>
/// Aggregated result of running all systems for one simulation tick.
/// </summary>
public sealed record TickResult(
    int TickNumber,
    int RelationshipsAdded,
    int RelationshipsAdjusted,
    int RelationshipsArchived,
    int EntitiesModified,
    IReadOnlyList<(string SystemId, SystemResult Result)> PerSystemResults);

// =============================================================================
// SIMULATION TICK RUNNER
// =============================================================================

/// <summary>
/// Runs all simulation systems for a single tick, applying era modifiers
/// and updating pressures. This is the inner loop of the simulation.
///
/// Port of: apps/lore-weave/lib/engine/worldEngine.ts — runSimulationTick()
/// </summary>
public sealed class SimulationTickRunner
{
    private readonly IReadOnlyList<ISimulationSystem> _systems;
    private readonly IReadOnlyList<Pressure> _pressures;
    private readonly WorldRuntime _runtime;

    public SimulationTickRunner(
        IReadOnlyList<ISimulationSystem> systems,
        IReadOnlyList<Pressure> pressures,
        WorldRuntime runtime)
    {
        _systems = systems;
        _pressures = pressures;
        _runtime = runtime;
    }

    /// <summary>
    /// Execute all systems for one tick, then update pressures.
    /// </summary>
    /// <param name="tickNumber">Current tick number (for logging/tracking).</param>
    /// <param name="currentEra">The active era, used to look up system modifiers.</param>
    public async Task<TickResult> RunTickAsync(int tickNumber, Era currentEra)
    {
        var perSystem = new List<(string SystemId, SystemResult Result)>();
        var totalRelAdded = 0;
        var totalRelAdjusted = 0;
        var totalRelArchived = 0;
        var totalEntitiesModified = 0;

        // 1. Run each system with its era modifier
        foreach (var system in _systems)
        {
            var modifier = GetSystemModifier(currentEra, system.Id);
            if (modifier == 0)
                continue;

            try
            {
                var result = await system.ApplyAsync(modifier);
                perSystem.Add((system.Id, result));

                totalRelAdded += result.RelationshipsAdded.Count;
                totalRelAdjusted += result.RelationshipsAdjusted.Count;
                totalRelArchived += result.RelationshipsToArchive.Count;
                totalEntitiesModified += result.EntitiesModified.Count;

                // Apply pressure changes from this system
                foreach (var (pressureId, delta) in result.PressureChanges)
                {
                    _runtime.ModifyPressure(pressureId, delta);
                }
            }
            catch (Exception ex)
            {
                _runtime.Log(LogLevel.Error,
                    $"System {system.Id} failed at tick {tickNumber}: {ex.Message}");
            }
        }

        // 2. Apply pressure growth functions
        foreach (var pressure in _pressures)
        {
            pressure.ApplyGrowth(_runtime);
        }

        // 3. Apply pressure homeostasis
        foreach (var pressure in _pressures)
        {
            pressure.ApplyHomeostasis(_runtime.Config.PressureDeltaSmoothing);
        }

        // 4. Sync pressure values back to runtime
        foreach (var pressure in _pressures)
        {
            _runtime.SetPressure(pressure.Id, pressure.Value);
        }

        // 5. Advance runtime tick
        _runtime.AdvanceTick();

        return new TickResult(
            TickNumber: tickNumber,
            RelationshipsAdded: totalRelAdded,
            RelationshipsAdjusted: totalRelAdjusted,
            RelationshipsArchived: totalRelArchived,
            EntitiesModified: totalEntitiesModified,
            PerSystemResults: perSystem);
    }

    /// <summary>
    /// Get the era modifier for a system. Defaults to 1.0 if not specified.
    /// </summary>
    private static double GetSystemModifier(Era era, string systemId)
    {
        return era.SystemModifiers.TryGetValue(systemId, out var modifier) ? modifier : 1.0;
    }
}
