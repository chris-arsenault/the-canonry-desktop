using TheCanonry.Engine.Rules.Types;
using TheCanonry.Schema.Ids;

namespace TheCanonry.Engine.Engine;

// =============================================================================
// ERA TRANSITION EFFECTS
// =============================================================================

/// <summary>
/// Effects applied during era transitions (entry or exit).
/// Expressed as pressure modification mutations.
/// </summary>
public sealed record EraTransitionEffects(
    IReadOnlyList<ModifyPressureMutation> Mutations)
{
    public static EraTransitionEffects Empty { get; } = new([]);
}

// =============================================================================
// ERA DEFINITION
// =============================================================================

/// <summary>
/// Era configuration defines a historical period in world generation.
///
/// Transition model:
/// - ExitConditions: Criteria that must ALL be met for this era to end
/// - EntryConditions: Criteria that must ALL be met for this era to START
/// - NextEra: Optional explicit next era ID (supports divergent era paths)
/// - ExitEffects: Effects applied when transitioning OUT of this era
/// - EntryEffects: Effects applied when transitioning INTO this era
///
/// Era entities are created lazily when transitioned into (not spawned at init).
/// If no NextEra is specified, the system finds the first candidate era
/// whose EntryConditions are met.
/// </summary>
public sealed record Era(
    EraId Id,
    string Name,
    string Summary,
    IReadOnlyDictionary<string, double> TemplateWeights,
    IReadOnlyDictionary<string, double> SystemModifiers,
    IReadOnlyDictionary<string, double> PressureModifiers,
    IReadOnlyList<Condition> ExitConditions,
    IReadOnlyList<Condition> EntryConditions,
    EraId? NextEra,
    EraTransitionEffects ExitEffects,
    EraTransitionEffects EntryEffects);
