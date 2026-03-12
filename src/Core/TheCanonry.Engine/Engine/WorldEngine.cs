namespace TheCanonry.Engine.Engine;

using System.Diagnostics;
using TheCanonry.Engine.Pressures;
using TheCanonry.Engine.Runtime;
using TheCanonry.Engine.Selection;
using TheCanonry.Engine.Systems;
using TheCanonry.Engine.Validation;

// =============================================================================
// WORLD RESULT
// =============================================================================

/// <summary>
/// Final result of a completed simulation run.
/// Contains summary statistics — not the full graph, which is accessible via the runtime.
/// </summary>
public sealed record WorldResult(
    int TotalEntities,
    int TotalRelationships,
    int TotalTicks,
    int TotalEpochs,
    TimeSpan Elapsed,
    IReadOnlyList<EpochResult> EpochSummaries);

// =============================================================================
// WORLD ENGINE
// =============================================================================

/// <summary>
/// Top-level simulation orchestrator. Creates a WorldRuntime, initializes all
/// subsystems, and runs epochs in a loop until completion or cancellation.
///
/// Port of: apps/lore-weave/lib/engine/worldEngine.ts — WorldEngine class
///
/// Unlike the 2400-line TS monolith, orchestration is split into:
/// - SimulationTickRunner: runs all systems for one tick
/// - EpochRunner: manages one epoch lifecycle (growth + simulation)
/// - WorldEngine: top-level loop with era transitions and validation
/// </summary>
public sealed class WorldEngine
{
    private readonly EngineConfig _config;
    private readonly int? _randomSeed;

    /// <summary>
    /// Event raised to report simulation progress.
    /// </summary>
    public event Action<ProgressPayload>? OnProgress;

    public WorldEngine(EngineConfig config, int? randomSeed = null)
    {
        _config = config;
        _randomSeed = randomSeed;
    }

    /// <summary>
    /// Run the complete simulation from start to finish.
    /// This is the only public entry point for running a simulation.
    /// </summary>
    public async Task<WorldResult> RunAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var emitter = _config.Emitter;

        // 1. Validate configuration
        EmitProgress(SimulationPhase.Validating, 0, 0, 0, 0);
        var report = ValidationOrchestrator.Validate(_config);
        if (!report.IsValid)
        {
            var errorSummary = string.Join("; ", report.Errors);
            emitter.Error(new ErrorPayload(
                $"Config validation failed: {errorSummary}",
                null,
                "validation",
                new Dictionary<string, object?>()));
            throw new InvalidOperationException(
                $"Engine config validation failed with {report.Errors.Count} error(s): {errorSummary}");
        }

        // 2. Create WorldRuntime
        var runtime = new WorldRuntime(_config, randomSeed: _randomSeed);

        // 3. Initialize simulation systems via SystemInterpreter
        var systems = SystemInterpreter.LoadSystems(_config.Systems, runtime);
        foreach (var system in systems)
        {
            system.Initialize();
        }

        // 4. Create pressures via PressureInterpreter
        var pressures = PressureInterpreter.LoadPressures(_config.Pressures, runtime);

        // Initialize pressure values in runtime
        foreach (var pressure in pressures)
        {
            runtime.SetPressure(pressure.Id, pressure.Value);
        }

        // 5. Create GrowthSystem
        var weightCalculator = new DynamicWeightCalculator();
        var growthSystem = new GrowthSystem(_config, runtime, weightCalculator);

        // 6. Run EraSpawner to create first era
        var eraSpawner = CreateEraSpawner(runtime);
        eraSpawner.Initialize();
        await eraSpawner.ApplyAsync(1.0);

        // 7. Build runners
        var tickRunner = new SimulationTickRunner(systems, pressures, runtime);
        var epochRunner = new EpochRunner(growthSystem, tickRunner, runtime, _config);

        // 8. Main loop
        EmitProgress(SimulationPhase.Running, 0, _config.MaxTicks, 0, _config.MaxEpochs);
        emitter.Log(LogLevel.Info, "Starting world generation...");

        var epochResults = new List<EpochResult>();
        var currentEpoch = 0;

        while (ShouldContinue(runtime, currentEpoch, ct))
        {
            var currentEra = runtime.CurrentEra;
            if (currentEra is null)
            {
                emitter.Log(LogLevel.Warn, "No current era set — stopping simulation.");
                break;
            }

            var epochResult = await epochRunner.RunEpochAsync(currentEra, currentEpoch);
            epochResults.Add(epochResult);

            // Emit progress for external listeners
            EmitProgress(
                SimulationPhase.Running,
                runtime.CurrentTick,
                _config.MaxTicks,
                currentEpoch,
                _config.MaxEpochs);

            currentEpoch++;
        }

        // 9. Finalize
        stopwatch.Stop();
        EmitProgress(SimulationPhase.Finalizing,
            runtime.CurrentTick, _config.MaxTicks,
            currentEpoch, _config.MaxEpochs);

        emitter.Log(LogLevel.Info,
            $"Generation complete: {runtime.GetEntityCount()} entities, " +
            $"{runtime.GetRelationships().Count} relationships, " +
            $"{runtime.CurrentTick} ticks in {stopwatch.Elapsed.TotalMilliseconds:F0}ms");

        emitter.Complete();

        return new WorldResult(
            TotalEntities: runtime.GetEntityCount(),
            TotalRelationships: runtime.GetRelationships().Count,
            TotalTicks: runtime.CurrentTick,
            TotalEpochs: currentEpoch,
            Elapsed: stopwatch.Elapsed,
            EpochSummaries: epochResults);
    }

    // =========================================================================
    // CONTINUATION CHECK
    // =========================================================================

    private bool ShouldContinue(WorldRuntime runtime, int currentEpoch, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return false;

        if (runtime.CurrentTick >= _config.MaxTicks)
        {
            _config.Emitter.Log(LogLevel.Warn,
                $"Stopped: Hit maximum tick limit ({_config.MaxTicks})");
            return false;
        }

        if (currentEpoch >= _config.MaxEpochs)
        {
            _config.Emitter.Log(LogLevel.Info,
                $"All epochs completed at epoch {currentEpoch}");
            return false;
        }

        return true;
    }

    // =========================================================================
    // ERA SPAWNER
    // =========================================================================

    private static EraSpawner CreateEraSpawner(WorldRuntime runtime)
    {
        var config = new FrameworkSystemConfig(
            Id: "era-spawner",
            Name: "Era Spawner",
            Description: "Creates initial era entity");

        return new EraSpawner(config, runtime);
    }

    // =========================================================================
    // PROGRESS EMISSION
    // =========================================================================

    private void EmitProgress(SimulationPhase phase, int tick, int maxTicks, int epoch, int totalEpochs)
    {
        var payload = new ProgressPayload(
            Phase: phase,
            Tick: tick,
            MaxTicks: maxTicks,
            Epoch: epoch,
            TotalEpochs: totalEpochs,
            EntityCount: 0,
            RelationshipCount: 0);

        _config.Emitter.Progress(payload);
        OnProgress?.Invoke(payload);
    }
}
