using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Chronicle.PerspectiveSynthesis;

/// <summary>
/// How a canon fact is used in perspective synthesis.
/// WorldTruth facts are faceted by the LLM; GenerationConstraint facts
/// are always included verbatim and never sent to the LLM for faceting.
/// </summary>
public enum FactType
{
    WorldTruth,
    GenerationConstraint,
}

/// <summary>
/// Culture distribution classification for a constellation.
/// </summary>
public enum CultureBalance
{
    Single,
    Dominant,
    Mixed,
}

/// <summary>
/// Dominant entity-kind focus for a constellation.
/// </summary>
public enum KindFocus
{
    Character,
    Place,
    Object,
    Event,
    Mixed,
}

/// <summary>
/// Spatial spread of entities in a constellation.
/// </summary>
public enum SpatialSpread
{
    Tight,
    Dispersed,
}

/// <summary>
/// Whether entities span a single era or multiple eras.
/// </summary>
public enum EraSpan
{
    Single,
    Multiple,
}

/// <summary>
/// A canon fact with metadata controlling how it is used in synthesis.
/// </summary>
public sealed record CanonFactWithMetadata(
    string Id,
    string Text,
    FactType Type = FactType.WorldTruth,
    bool Required = false,
    bool Disabled = false);

/// <summary>
/// A fact selected for this constellation with a context-specific interpretation.
/// </summary>
public sealed record FactFacet(string FactId, string Interpretation);

/// <summary>
/// Per-entity writing directive: concrete guidance for writing this entity
/// in the context of this specific chronicle.
/// </summary>
public sealed record EntityDirective(string EntityId, string EntityName, string Directive);

/// <summary>
/// Configuration controlling how many world-truth facts are faceted.
/// </summary>
public sealed record FactSelectionConfig(int? MinCount = null, int? MaxCount = null);

/// <summary>
/// Core prose style guidance fragments for tone assembly.
/// </summary>
public sealed record ToneFragments(string Core);

/// <summary>
/// Result of constellation analysis — structural signals derived from the entity set.
/// </summary>
public sealed record EntityConstellation
{
    public required IReadOnlyDictionary<string, int> Cultures { get; init; }
    public string? DominantCulture { get; init; }
    public required CultureBalance CultureBalance { get; init; }
    public required IReadOnlyDictionary<string, int> Kinds { get; init; }
    public string? DominantKind { get; init; }
    public required KindFocus KindFocus { get; init; }
    public required IReadOnlyDictionary<string, int> TagFrequency { get; init; }
    public required IReadOnlyList<string> ProminentTags { get; init; }
    public required IReadOnlyDictionary<string, int> RelationshipKinds { get; init; }
    public string? FocalEraId { get; init; }
    public required EraSpan EraSpan { get; init; }
    public (double X, double Y)? CoordinateCentroid { get; init; }
    public required SpatialSpread SpatialSpread { get; init; }
    public required string FocusSummary { get; init; }
}

/// <summary>
/// LLM-generated perspective synthesis output.
/// </summary>
public sealed record PerspectiveSynthesis
{
    /// <summary>150-200 words of perspective guidance for this chronicle.</summary>
    public required string Brief { get; init; }

    /// <summary>Selected world-truth facts with their constellation-specific interpretations.</summary>
    public required IReadOnlyList<FactFacet> Facets { get; init; }

    /// <summary>2-3 short phrases that might echo through this chronicle.</summary>
    public required IReadOnlyList<string> SuggestedMotifs { get; init; }

    /// <summary>3-5 atmospheric voice keys appropriate to this narrative style.</summary>
    public required IReadOnlyDictionary<string, string> NarrativeVoice { get; init; }

    /// <summary>Per-entity writing directives pre-applying cultural traits and prose hints.</summary>
    public required IReadOnlyList<EntityDirective> EntityDirectives { get; init; }

    /// <summary>
    /// Optional 2-4 sentence synthesis of world dynamics into story-specific stakes.
    /// Only present when world dynamics were provided.
    /// </summary>
    public string? TemporalNarrative { get; init; }
}

/// <summary>
/// Era context used as focal frame in perspective synthesis.
/// </summary>
public sealed record EraContext(string Id, string Name, string? Description = null);

/// <summary>
/// Narrative style — minimal subset needed by perspective synthesis.
/// </summary>
public sealed record NarrativeStyle(
    string Id,
    string Name,
    string? Format = null,
    string? Description = null,
    IReadOnlyList<string>? Tags = null,
    string? ProseGuidance = null,
    string? CraftPosture = null);

/// <summary>
/// A world dynamic — higher-level narrative context statement about inter-group
/// forces and behaviors.
/// </summary>
public sealed record WorldDynamic
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public IReadOnlyList<string>? Cultures { get; init; }
    public IReadOnlyList<string>? Kinds { get; init; }
    public IReadOnlyDictionary<string, WorldDynamicEraOverride>? EraOverrides { get; init; }
}

/// <summary>
/// Era-specific override for a world dynamic.
/// </summary>
public sealed record WorldDynamicEraOverride(string Text, bool Replace);

/// <summary>
/// Full input to the perspective synthesizer.
/// </summary>
public sealed record PerspectiveSynthesisInput
{
    public required EntityConstellation Constellation { get; init; }
    public required IReadOnlyList<EntityContext> Entities { get; init; }
    public EraContext? FocalEra { get; init; }
    public required IReadOnlyList<CanonFactWithMetadata> FactsWithMetadata { get; init; }
    public required ToneFragments ToneFragments { get; init; }

    /// <summary>Cultural identity traits for all cultures present in this constellation.</summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? CulturalIdentities { get; init; }

    public NarrativeStyle? NarrativeStyle { get; init; }

    /// <summary>Prose hints per entity kind (kind → guidance text).</summary>
    public IReadOnlyDictionary<string, string>? ProseHints { get; init; }

    /// <summary>World dynamics — higher-level narrative context statements.</summary>
    public IReadOnlyList<WorldDynamic>? WorldDynamics { get; init; }

    /// <summary>Fact selection settings for perspective synthesis.</summary>
    public FactSelectionConfig? FactSelection { get; init; }

    /// <summary>Optional free-text narrative direction — primary constraint when present.</summary>
    public string? NarrativeDirection { get; init; }

    /// <summary>Role assignments from the chronicle wizard.</summary>
    public IReadOnlyList<ChronicleRoleAssignment>? RoleAssignments { get; init; }
}

/// <summary>
/// Full synthesis result including assembled tone, faceted facts, and token usage.
/// </summary>
public sealed record PerspectiveSynthesisResult
{
    public required PerspectiveSynthesis Synthesis { get; init; }

    /// <summary>Assembled tone string (core tone fragment).</summary>
    public required string AssembledTone { get; init; }

    /// <summary>
    /// All facts formatted for generation: world-truth facts with faceted interpretations
    /// followed by generation constraints verbatim.
    /// </summary>
    public required IReadOnlyList<string> FacetedFacts { get; init; }

    /// <summary>World dynamics actually injected into the synthesis prompt (post-filter/override).</summary>
    public required IReadOnlyList<ResolvedWorldDynamic> ResolvedWorldDynamics { get; init; }

    public required int InputTokens { get; init; }
    public required int OutputTokens { get; init; }
    public required decimal ActualCost { get; init; }
}

/// <summary>
/// A resolved world dynamic after filtering and era overrides.
/// </summary>
public sealed record ResolvedWorldDynamic(string Id, string Text);
