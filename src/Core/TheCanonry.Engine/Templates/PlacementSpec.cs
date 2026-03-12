namespace TheCanonry.Engine.Templates;

// =============================================================================
// PLACEMENT ANCHOR
// =============================================================================

/// <summary>
/// Anchor determines the primary placement strategy.
/// Each entity kind has its own independent coordinate space (0-100 scale).
/// </summary>
public abstract record PlacementAnchor;

/// <summary>Place near a referenced entity. Reference must be the SAME KIND as the entity being created.</summary>
public sealed record EntityAnchor(
    string Ref,
    bool StickToRegion) : PlacementAnchor;

/// <summary>Place at a culture's center coordinates.</summary>
public sealed record CultureAnchor(
    string Id) : PlacementAnchor;

/// <summary>Place at the centroid of multiple referenced entities with jitter.</summary>
public sealed record RefsCentroidAnchor(
    IReadOnlyList<string> Refs,
    double Jitter) : PlacementAnchor;

/// <summary>Place sparsely, optionally preferring the periphery.</summary>
public sealed record SparseAnchor(
    bool PreferPeriphery) : PlacementAnchor;

/// <summary>Place within explicit coordinate bounds.</summary>
public sealed record BoundsAnchor(
    (double Min, double Max) X,
    (double Min, double Max) Y,
    (double Min, double Max) Z) : PlacementAnchor;

// =============================================================================
// PLACEMENT SPACING
// =============================================================================

/// <summary>Spacing constraints for placement.</summary>
public sealed record PlacementSpacing(
    double MinDistance,
    IReadOnlyList<string> AvoidRefs);

// =============================================================================
// PLACEMENT REGION POLICY
// =============================================================================

/// <summary>Region creation and selection policy.</summary>
public sealed record PlacementRegionPolicy(
    /// <summary>Allow emergent region creation when seed regions are at capacity.</summary>
    bool AllowEmergent,
    /// <summary>Create a new region centered on the placed entity.</summary>
    bool CreateRegion,
    /// <summary>Bias region selection toward sparser regions.</summary>
    bool PreferSparse);

// =============================================================================
// PLACEMENT STEPS
// =============================================================================

/// <summary>Ordered placement steps to attempt if the primary anchor placement fails.</summary>
public enum PlacementStep
{
    AnchorRegion,
    SeedRegion,
    Sparse,
    Random
}

// =============================================================================
// PLACEMENT SPEC
// =============================================================================

/// <summary>
/// How to place an entity in semantic space.
/// Defines the anchor strategy, spacing constraints, region policy, and fallback steps.
/// </summary>
public sealed record PlacementSpec(
    PlacementAnchor Anchor,
    PlacementSpacing Spacing,
    PlacementRegionPolicy RegionPolicy,
    IReadOnlyList<PlacementStep> Steps);
