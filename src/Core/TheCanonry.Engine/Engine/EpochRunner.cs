using TheCanonry.Engine.Runtime;
using TheCanonry.Engine.Systems;

namespace TheCanonry.Engine.Engine;

// =============================================================================
// EPOCH RESULT
// =============================================================================

/// <summary>
/// Result of a completed epoch: growth summary + per-tick simulation results.
/// </summary>
public sealed record EpochResult(
    int EpochNumber,
    GrowthEpochSummary GrowthSummary,
    IReadOnlyList<TickResult> TickResults,
    int EntityCountAtEnd,
    int RelationshipCountAtEnd);

// =============================================================================
// EPOCH RUNNER
// =============================================================================

/// <summary>
/// Manages the lifecycle of a single epoch: growth phase followed by
/// simulation ticks. Each epoch advances the world by one chunk of time.
///
/// Port of: apps/lore-weave/lib/engine/worldEngine.ts — runEpoch()
/// </summary>
public sealed class EpochRunner
{
    private readonly GrowthSystem _growth;
    private readonly SimulationTickRunner _tickRunner;
    private readonly WorldRuntime _runtime;
    private readonly EngineConfig _config;

    public EpochRunner(
        GrowthSystem growth,
        SimulationTickRunner tickRunner,
        WorldRuntime runtime,
        EngineConfig config)
    {
        _growth = growth;
        _tickRunner = tickRunner;
        _runtime = runtime;
        _config = config;
    }

    /// <summary>
    /// Run one complete epoch: growth phase then simulation ticks.
    /// </summary>
    /// <param name="currentEra">The active era for this epoch.</param>
    /// <param name="epochNumber">Zero-based epoch index.</param>
    public async Task<EpochResult> RunEpochAsync(Era currentEra, int epochNumber)
    {
        // 1. Emit epoch start
        _config.Emitter.EpochStart(new EpochStartPayload(
            Epoch: epochNumber,
            EraId: currentEra.Id,
            EraName: currentEra.Name,
            EraSummary: currentEra.Summary,
            Tick: _runtime.CurrentTick));

        // 2. Growth phase: initialize budget, run growth ticks, complete
        _growth.StartEpoch(currentEra);

        var growthModifier = GetGrowthModifier(currentEra);
        for (var i = 0; i < _config.TicksPerEpoch; i++)
        {
            _growth.Apply(growthModifier);
        }

        var growthSummary = _growth.CompleteEpoch();

        // 3. Simulation phase: run simulation ticks
        var tickResults = new List<TickResult>();
        for (var i = 0; i < _config.TicksPerEpoch; i++)
        {
            var tickResult = await _tickRunner.RunTickAsync(_runtime.CurrentTick, currentEra);
            tickResults.Add(tickResult);
        }

        // 4. Update population metrics
        _runtime.UpdatePopulationMetrics();

        // 5. Emit progress
        _config.Emitter.Progress(new ProgressPayload(
            Phase: SimulationPhase.Running,
            Tick: _runtime.CurrentTick,
            MaxTicks: _config.MaxTicks,
            Epoch: epochNumber,
            TotalEpochs: _config.MaxEpochs,
            EntityCount: _runtime.GetEntityCount(),
            RelationshipCount: _runtime.GetRelationships().Count));

        return new EpochResult(
            EpochNumber: epochNumber,
            GrowthSummary: growthSummary,
            TickResults: tickResults,
            EntityCountAtEnd: _runtime.GetEntityCount(),
            RelationshipCountAtEnd: _runtime.GetRelationships().Count);
    }

    /// <summary>
    /// Get the growth system modifier from the era. Defaults to 1.0.
    /// The growth system uses "framework-growth" or "growth" as its system ID.
    /// </summary>
    private static double GetGrowthModifier(Era era)
    {
        if (era.SystemModifiers.TryGetValue("framework-growth", out var mod))
            return mod;
        if (era.SystemModifiers.TryGetValue("growth", out mod))
            return mod;
        return 1.0;
    }
}
