namespace TheCanonry.Engine.Systems;

using TheCanonry.Schema.World;

/// <summary>
/// Shared helper methods for catalyst/action evaluation.
/// Ported from TS systems/catalystHelpers.ts.
/// </summary>
public static class CatalystHelpers
{
    /// <summary>
    /// Calculate action attempt chance based on entity prominence.
    /// Higher prominence = higher chance of attempting an action.
    /// </summary>
    /// <param name="entity">The entity attempting action.</param>
    /// <param name="baseRate">Base action attempt rate from system parameters.</param>
    /// <returns>Probability of action attempt this tick, clamped to [0, 1].</returns>
    public static double CalculateAttemptChance(Entity entity, double baseRate)
    {
        if (!entity.Catalyst.CanAct)
            return 0.0;

        var multiplier = GetProminenceMultiplier(entity.Prominence.Value);
        var chance = baseRate * multiplier;

        return Math.Max(0.0, Math.Min(1.0, chance));
    }

    /// <summary>
    /// Get a prominence-based multiplier for action rate or success chance.
    /// Maps prominence levels to multipliers that increase with prominence.
    /// </summary>
    /// <param name="prominenceValue">The entity's numeric prominence value.</param>
    /// <returns>A multiplier >= 0.5.</returns>
    public static double GetProminenceMultiplier(double prominenceValue) => prominenceValue switch
    {
        < 1.0 => 0.5,   // forgotten
        < 2.0 => 0.75,  // marginal
        < 3.0 => 1.0,   // recognized
        < 4.0 => 1.5,   // renowned
        _ => 2.0         // mythic
    };

    /// <summary>
    /// Check if an entity can perform actions.
    /// </summary>
    public static bool CanPerformAction(Entity entity)
    {
        return entity.Catalyst.CanAct;
    }

    /// <summary>
    /// Calculate action weight based on pressure levels.
    /// Formula: weight = baseWeight * (1 + sum of (pressure/100 * multiplier))
    /// </summary>
    /// <param name="baseWeight">The action's base weight.</param>
    /// <param name="pressureContribution">Sum of (pressure/100 * multiplier) for all pressure modifiers.</param>
    /// <returns>Final weight, minimum 0.1.</returns>
    public static double CalculateActionWeight(double baseWeight, double pressureContribution)
    {
        return Math.Max(0.1, baseWeight * (1.0 + pressureContribution));
    }
}
