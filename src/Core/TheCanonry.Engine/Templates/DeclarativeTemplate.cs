namespace TheCanonry.Engine.Templates;

using TheCanonry.Engine.Rules.Types;

// =============================================================================
// RELATIONSHIP RULE
// =============================================================================

/// <summary>
/// Rules that define relationships to create.
/// Relationship distance is ALWAYS computed from Euclidean distance
/// between src and dst coordinates. It cannot be set directly.
/// </summary>
public sealed record RelationshipRule(
    /// <summary>Relationship kind.</summary>
    string Kind,

    /// <summary>Source entity reference: "$newEntity", "$target", "$faction".</summary>
    string Src,

    /// <summary>Destination entity reference.</summary>
    string Dst,

    /// <summary>Relationship strength (0.0 weak/spatial to 1.0 strong/narrative).</summary>
    double Strength,

    /// <summary>Whether to create the inverse relationship too.</summary>
    bool Bidirectional,

    /// <summary>Entity that catalyzed this relationship.</summary>
    string CatalyzedBy,

    /// <summary>Condition that must hold for this relationship to be created.</summary>
    Condition? Condition);

// =============================================================================
// VARIABLE DEFINITION
// =============================================================================

/// <summary>
/// Named entity reference computed during execution.
/// Variables are resolved in order and can reference previously resolved variables.
/// </summary>
public sealed record VariableDefinition(
    /// <summary>How to select entities for this variable.</summary>
    VariableSelectionRule Select,

    /// <summary>
    /// If true, the template will not run unless this variable resolves
    /// to at least one entity. Use when the template logic depends on
    /// the variable existing.
    /// </summary>
    bool Required);

// =============================================================================
// TEMPLATE VARIANTS
// =============================================================================

/// <summary>Strategy for selecting which variant(s) to apply.</summary>
public enum VariantSelectionMode
{
    /// <summary>Apply only the first matching variant.</summary>
    FirstMatch,

    /// <summary>Apply all matching variants.</summary>
    AllMatching
}

/// <summary>
/// Conditional variants that modify template output based on world state.
/// Allows a single template to produce different outcomes depending on
/// pressures, entity counts, or random chance.
/// </summary>
public sealed record TemplateVariants(
    VariantSelectionMode Selection,
    IReadOnlyList<TemplateVariant> Options);

/// <summary>A single variant option with its condition and effects.</summary>
public sealed record TemplateVariant(
    /// <summary>Human-readable name for this variant.</summary>
    string Name,

    /// <summary>Condition that must be met for this variant to apply.</summary>
    Condition When,

    /// <summary>Effects to apply when this variant is selected.</summary>
    VariantEffects Apply);

/// <summary>
/// Effects applied when a variant is selected.
/// All fields are optional (empty collections mean no effect).
/// </summary>
public sealed record VariantEffects(
    /// <summary>Override subtype for created entities. Key is entityRef (e.g., "$location").</summary>
    IReadOnlyDictionary<string, string> Subtype,

    /// <summary>Additional tags to add to created entities. Key is entityRef.</summary>
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, bool>> Tags,

    /// <summary>Additional relationships to create.</summary>
    IReadOnlyList<RelationshipRule> Relationships,

    /// <summary>Additional state updates to apply.</summary>
    IReadOnlyList<Mutation> StateUpdates);

// =============================================================================
// DECLARATIVE TEMPLATE
// =============================================================================

/// <summary>
/// A declarative template definition. Top-level structure that defines
/// a complete template as JSON data.
///
/// Applicability model:
/// A template can run when:
///   1. All Applicability conditions pass (pressure thresholds, entity counts, era, etc.)
///   2. AND the Selection returns at least one valid target
///
/// This means you don't need to duplicate graph_path rules in both applicability
/// and selection. The selection rule implicitly acts as an applicability check.
/// </summary>
public sealed record DeclarativeTemplate(
    /// <summary>Unique template identifier.</summary>
    string Id,

    /// <summary>Human-readable template name.</summary>
    string Name,

    /// <summary>Whether this template is active.</summary>
    bool Enabled,

    /// <summary>Conditions that must all pass for the template to be applicable.</summary>
    IReadOnlyList<Condition> Applicability,

    /// <summary>How to find target entities.</summary>
    SelectionRule Selection,

    /// <summary>Rules for creating new entities.</summary>
    IReadOnlyList<CreationRule> Creation,

    /// <summary>Rules for creating relationships between entities.</summary>
    IReadOnlyList<RelationshipRule> Relationships,

    /// <summary>State updates (mutations) to apply after creation.</summary>
    IReadOnlyList<Mutation> StateUpdates,

    /// <summary>Named entity references computed during execution.</summary>
    IReadOnlyDictionary<string, VariableDefinition> Variables,

    /// <summary>Conditional variants that modify template output based on world state.</summary>
    TemplateVariants Variants,

    /// <summary>Narration template for narrative-quality text. Empty string if no narration.</summary>
    string NarrationTemplate);
