namespace TheCanonry.Engine.Systems;

using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Runtime;

/// <summary>
/// Propagates/diverges tags based on entity connectivity.
/// Placeholder: Apply logic will be implemented in Task 13.
/// </summary>
public sealed class TagDiffusion : ISimulationSystem
{
    private readonly IReadOnlyDictionary<string, object?> _config;

    public TagDiffusion(
        string id, string name,
        IReadOnlyDictionary<string, object?> config,
        WorldRuntime runtime)
    {
        Id = id;
        Name = name;
        _config = config;
        Runtime = runtime;
    }

    public string Id { get; }
    public string Name { get; }

    /// <summary>The runtime this system operates on.</summary>
    internal WorldRuntime Runtime { get; }

    /// <summary>The raw configuration dictionary for this system.</summary>
    internal IReadOnlyDictionary<string, object?> Config => _config;

    public void Initialize() { }

    public Task<SystemResult> ApplyAsync(double modifier)
    {
        return Task.FromResult(SystemResult.Empty);
    }
}
