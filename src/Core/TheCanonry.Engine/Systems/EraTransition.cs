namespace TheCanonry.Engine.Systems;

using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Runtime;

/// <summary>
/// Handles transitions between eras based on exit/entry conditions defined per-era.
/// Placeholder: Apply logic will be implemented in Task 12.
/// </summary>
public sealed class EraTransition : ISimulationSystem
{
    private readonly EraTransitionConfig _config;

    public EraTransition(EraTransitionConfig config, WorldRuntime runtime)
    {
        _config = config;
        Runtime = runtime;
    }

    public string Id => _config.Id;
    public string Name => _config.Name;

    /// <summary>The runtime this system operates on.</summary>
    internal WorldRuntime Runtime { get; }

    /// <summary>The typed configuration for this system.</summary>
    internal EraTransitionConfig Config => _config;

    public void Initialize() { }

    public Task<SystemResult> ApplyAsync(double modifier)
    {
        return Task.FromResult(SystemResult.Empty);
    }
}
