namespace TheCanonry.Engine.Engine;

/// <summary>
/// Interface for simulation systems that modify world state each tick.
/// Systems create relationships between existing entities and modify their states.
/// </summary>
public interface ISimulationSystem
{
    /// <summary>Unique identifier for this system.</summary>
    string Id { get; }

    /// <summary>Human-readable name.</summary>
    string Name { get; }

    /// <summary>
    /// Optional initialization called once before the first tick.
    /// Use to set up initial state (e.g., allocate grid arrays).
    /// </summary>
    void Initialize();

    /// <summary>
    /// Run one tick of this system.
    /// </summary>
    /// <param name="modifier">Era-specific modifier for this system's intensity.</param>
    /// <returns>Result describing all side effects.</returns>
    Task<SystemResult> ApplyAsync(double modifier);
}
