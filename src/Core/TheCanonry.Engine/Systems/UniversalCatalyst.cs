namespace TheCanonry.Engine.Systems;

using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Runtime;

/// <summary>
/// Enables agents to perform domain-defined actions.
/// Success chance is based on entity prominence.
/// Placeholder: Apply logic will be implemented in Task 12.
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
        return Task.FromResult(SystemResult.Empty);
    }
}
