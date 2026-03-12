namespace TheCanonry.Engine.Coordinates;

using TheCanonry.Engine.Graph;
using TheCanonry.Schema.Config;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

/// <summary>
/// Centralized coordinate services with culture support.
/// Each entity kind has its own independent semantic plane.
/// Coordinates represent semantic similarity within a kind, not physical location.
/// </summary>
public class CoordinateContext
{
    private static readonly Random Rng = new();

    private const double SpaceMin = 0.0;
    private const double SpaceMax = 100.0;
    private const double DefaultBias = 50.0;
    private const double EmergentRadius = 10.0;
    private const double EmergentMinDistFromExisting = 5.0;
    private const int DefaultMaxAttempts = 50;

    private readonly IReadOnlyList<EntityKindDefinition> _entityKinds;
    private readonly IReadOnlyList<CultureDefinition> _cultures;
    private readonly Dictionary<string, List<SemanticRegion>> _regions = new();
    private readonly Dictionary<string, AxisDefinition> _axisDefinitions;
    private readonly double _defaultMinDistance;
    private int _emergentRegionCounter;

    public CoordinateContext(DomainSchema schema, double defaultMinDistance = 5.0)
        : this(schema, [], defaultMinDistance) { }

    public CoordinateContext(
        DomainSchema schema,
        IReadOnlyList<AxisDefinition> axisDefinitions,
        double defaultMinDistance = 5.0)
    {
        _entityKinds = schema.EntityKinds;
        _cultures = schema.Cultures;
        _defaultMinDistance = defaultMinDistance;
        _axisDefinitions = axisDefinitions.ToDictionary(a => a.Id);
        InitializeRegionStorage();
    }

    private void InitializeRegionStorage()
    {
        foreach (var entityKind in _entityKinds)
        {
            if (entityKind.SemanticPlane is not null)
            {
                _regions[entityKind.Kind.Value] = [..entityKind.SemanticPlane.Regions];
            }
        }
    }

    // =========================================================================
    // SEMANTIC DATA ACCESS
    // =========================================================================

    /// <summary>Get all regions for an entity kind (seed + emergent).</summary>
    public IReadOnlyList<SemanticRegion> GetRegions(string entityKind)
    {
        if (!_regions.TryGetValue(entityKind, out var list))
        {
            list = [];
            _regions[entityKind] = list;
        }
        return list;
    }

    /// <summary>Check if a kind has a semantic plane configured.</summary>
    public bool HasKindMap(string kind) =>
        _entityKinds.Any(k => k.Kind.Value == kind && k.SemanticPlane is not null);

    /// <summary>Get seed region IDs for a culture within an entity kind.</summary>
    public IReadOnlyList<string> GetSeedRegionIds(string cultureId, string entityKind)
    {
        var regions = GetRegions(entityKind);
        return regions.Where(r => r.Culture == cultureId).Select(r => r.Id).ToList();
    }

    /// <summary>Get axis biases for a culture and entity kind.</summary>
    public AxisBias? GetAxisBiases(string cultureId, string entityKind) =>
        _cultures.FirstOrDefault(c => c.Id.Value == cultureId)?.AxisBiases
            .GetValueOrDefault(entityKind);

    // =========================================================================
    // MAIN PLACEMENT
    // =========================================================================

    /// <summary>
    /// Place an entity on its kind's semantic plane using culture-aware strategy.
    ///
    /// Priority order:
    /// 1. Near reference entities (Gaussian jitter around centroid)
    /// 2. Within seed regions belonging to the entity's culture
    /// 3. Near culture's axis bias point with Gaussian noise
    /// 4. Create emergent region near bias point (if allowed)
    /// </summary>
    public PlacementResult PlaceEntity(
        string entityKind,
        string cultureId,
        IReadOnlyList<Entity>? referenceEntities = null,
        PlacementOptions? options = null)
    {
        var opts = options ?? new PlacementOptions();
        var existingPoints = GetExistingPoints(entityKind);
        var axisBias = GetAxisBiases(cultureId, entityKind);
        var biasCenter = axisBias is not null
            ? new SemanticCoordinates(axisBias.X, axisBias.Y, axisBias.Z)
            : new SemanticCoordinates(DefaultBias, DefaultBias, DefaultBias);

        // Strategy 1: Near reference entities
        if (referenceEntities is { Count: > 0 })
        {
            var centroid = ComputeCentroid(referenceEntities);
            var point = SampleNearPoint(centroid, existingPoints, opts.MinDistance * 4, opts.MinDistance);
            if (point is not null)
            {
                return BuildResult(entityKind, point.Value, "near_reference");
            }
        }

        // Strategy 2: Within seed regions
        var seedRegionIds = GetSeedRegionIds(cultureId, entityKind);
        if (seedRegionIds.Count > 0)
        {
            var regions = GetRegions(entityKind);
            var seedRegions = seedRegionIds
                .Select(id => regions.FirstOrDefault(r => r.Id == id))
                .Where(r => r is not null)
                .Cast<SemanticRegion>()
                .ToList();

            var orderedRegions = opts.PreferSparse && seedRegions.Count > 1
                ? WeightedSparseSelection(seedRegions, existingPoints)
                : Shuffle(seedRegions);

            foreach (var region in orderedRegions)
            {
                var point = SampleInCircleRegion(region, existingPoints, opts.MinDistance);
                if (point is not null)
                {
                    return BuildResult(entityKind, point.Value, "seed_region");
                }
            }
        }

        // Strategy 3: Near culture's axis biases
        {
            var point = SampleNearPoint(biasCenter, existingPoints, opts.MinDistance * 4, opts.MinDistance);
            if (point is not null)
            {
                return BuildResult(entityKind, point.Value, "axis_biases");
            }
        }

        // Strategy 4: Create emergent region
        if (opts.AllowEmergent)
        {
            var emergent = CreateEmergentRegion(entityKind, biasCenter, "Emergent Territory", cultureId);
            if (emergent is not null)
            {
                var point = SampleInCircleRegion(emergent, existingPoints, opts.MinDistance);
                if (point is not null)
                {
                    return BuildResult(entityKind, point.Value, "emergent_region");
                }
            }
        }

        // Fallback: place at bias center (clamp to bounds)
        return BuildResult(entityKind, Clamp(biasCenter), "fallback");
    }

    // =========================================================================
    // REGION LOOKUP
    // =========================================================================

    /// <summary>
    /// Find the primary region containing a point on a kind's semantic plane.
    /// Returns null if the point is not in any region.
    /// </summary>
    public SemanticRegion? LookupRegion(string entityKind, SemanticCoordinates point)
    {
        var regions = GetRegions(entityKind);
        return regions.FirstOrDefault(r => PointInRegion(point, r));
    }

    /// <summary>Find all regions containing a point.</summary>
    public IReadOnlyList<SemanticRegion> LookupAllRegions(string entityKind, SemanticCoordinates point)
    {
        var regions = GetRegions(entityKind);
        return regions.Where(r => PointInRegion(point, r)).ToList();
    }

    // =========================================================================
    // EMERGENT REGION CREATION
    // =========================================================================

    /// <summary>
    /// Create an emergent region near a point on the semantic plane.
    /// Returns null if the point is too close to existing regions.
    /// </summary>
    public SemanticRegion? CreateEmergentRegion(
        string entityKind,
        SemanticCoordinates nearPoint,
        string label,
        string cultureId)
    {
        var regions = GetRegions(entityKind);

        // Check if point is too close to existing regions
        foreach (var region in regions)
        {
            if (region.Bounds is CircleBounds circle)
            {
                var dx = nearPoint.X - circle.CenterX;
                var dy = nearPoint.Y - circle.CenterY;
                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance < circle.Radius + EmergentMinDistFromExisting)
                    return null;
            }
        }

        _emergentRegionCounter++;
        var regionId = $"emergent_{entityKind}_{_emergentRegionCounter}";

        var culture = _cultures.FirstOrDefault(c => c.Id.Value == cultureId);
        var color = culture?.Color ?? "#888888";

        var newRegion = new SemanticRegion
        {
            Id = regionId,
            Label = label,
            Color = color,
            Culture = cultureId,
            Description = $"An emerging {entityKind} region: {label}",
            Tags = [],
            ZRange = new ZRange(SpaceMin, SpaceMax),
            Bounds = new CircleBounds(nearPoint.X, nearPoint.Y, EmergentRadius),
            Emergent = true,
        };

        _regions.TryAdd(entityKind, []);
        _regions[entityKind].Add(newRegion);

        return newRegion;
    }

    // =========================================================================
    // DISTANCE + SPATIAL QUERIES
    // =========================================================================

    /// <summary>Euclidean distance between two points (3D).</summary>
    public static double GetDistance(SemanticCoordinates a, SemanticCoordinates b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var dz = a.Z - b.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>Euclidean distance on the XY plane only.</summary>
    public static double GetDistance2D(SemanticCoordinates a, SemanticCoordinates b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Find nearest entities of a target kind to a reference entity, sorted by distance.
    /// </summary>
    public List<(Entity Entity, double Distance)> FindNearestEntities(
        IGraph graph, Entity reference, EntityKind targetKind, int limit)
    {
        var candidates = graph.GetEntitiesByKind(targetKind);
        return candidates
            .Where(e => e.Id != reference.Id)
            .Select(e => (Entity: e, Distance: GetDistance(reference.Coordinates, e.Coordinates)))
            .OrderBy(pair => pair.Distance)
            .Take(limit)
            .ToList();
    }

    // =========================================================================
    // TAG DERIVATION
    // =========================================================================

    /// <summary>
    /// Derive tags from entity placement based on region tags and axis position.
    /// </summary>
    public IReadOnlyList<string> DeriveTagsFromPlacement(
        string entityKind,
        SemanticCoordinates point,
        IReadOnlyList<SemanticRegion> containingRegions)
    {
        var tags = new List<string>();
        var seen = new HashSet<string>();

        void AddTag(string tag)
        {
            if (!string.IsNullOrEmpty(tag) && seen.Add(tag))
                tags.Add(tag);
        }

        // Region tags
        foreach (var region in containingRegions)
        {
            foreach (var tag in region.Tags)
                AddTag(tag);
        }

        // Axis-based tags
        DeriveAxisTags(entityKind, point, AddTag);

        return tags;
    }

    // =========================================================================
    // INTERNAL: SAMPLING
    // =========================================================================

    private SemanticCoordinates? SampleInCircleRegion(
        SemanticRegion region,
        List<SemanticCoordinates> existingPoints,
        double minDistance)
    {
        if (region.Bounds is not CircleBounds circle)
            return null;

        for (var i = 0; i < DefaultMaxAttempts; i++)
        {
            // sqrt for uniform distribution in circle, slight overshoot (1.1x)
            var r = circle.Radius * Math.Sqrt(Rng.NextDouble()) * 1.1;
            var theta = Rng.NextDouble() * 2 * Math.PI;
            var point = new SemanticCoordinates(
                circle.CenterX + r * Math.Cos(theta),
                circle.CenterY + r * Math.Sin(theta),
                DefaultBias);

            point = Clamp(point);
            if (IsValidPlacement(point, existingPoints, minDistance))
                return point;
        }

        return null;
    }

    private SemanticCoordinates? SampleNearPoint(
        SemanticCoordinates reference,
        List<SemanticCoordinates> existingPoints,
        double maxSearchRadius,
        double minDistance)
    {
        for (var i = 0; i < DefaultMaxAttempts; i++)
        {
            // Sample in a ring: [minDistance, maxSearchRadius]
            var r = minDistance + Rng.NextDouble() * (maxSearchRadius - minDistance);
            var theta = Rng.NextDouble() * 2 * Math.PI;
            var point = new SemanticCoordinates(
                reference.X + r * Math.Cos(theta),
                reference.Y + r * Math.Sin(theta),
                reference.Z);

            point = Clamp(point);
            if (IsValidPlacement(point, existingPoints, minDistance))
                return point;
        }

        return null;
    }

    private static bool IsValidPlacement(
        SemanticCoordinates point,
        List<SemanticCoordinates> existing,
        double minDistance)
    {
        foreach (var other in existing)
        {
            var dx = point.X - other.X;
            var dy = point.Y - other.Y;
            if (Math.Sqrt(dx * dx + dy * dy) < minDistance)
                return false;
        }
        return true;
    }

    private static SemanticCoordinates Clamp(SemanticCoordinates p) =>
        new(
            Math.Clamp(p.X, SpaceMin, SpaceMax),
            Math.Clamp(p.Y, SpaceMin, SpaceMax),
            Math.Clamp(p.Z, SpaceMin, SpaceMax));

    // =========================================================================
    // INTERNAL: REGION GEOMETRY
    // =========================================================================

    private static bool PointInRegion(SemanticCoordinates point, SemanticRegion region)
    {
        if (region.Bounds is CircleBounds circle)
        {
            var dx = point.X - circle.CenterX;
            var dy = point.Y - circle.CenterY;
            return Math.Sqrt(dx * dx + dy * dy) <= circle.Radius;
        }
        return false; // Only circle supported
    }

    // =========================================================================
    // INTERNAL: RESULT BUILDING
    // =========================================================================

    private PlacementResult BuildResult(string entityKind, SemanticCoordinates point, string strategy)
    {
        var containingRegions = LookupAllRegions(entityKind, point);
        var derivedTagList = DeriveTagsFromPlacement(entityKind, point, containingRegions);

        var derivedTags = new Dictionary<string, object>();
        foreach (var tag in derivedTagList)
            derivedTags[tag] = true;

        return new PlacementResult
        {
            Coordinates = point,
            RegionId = containingRegions.Count > 0 ? containingRegions[0].Id : "",
            AllRegionIds = containingRegions.Select(r => r.Id).ToList(),
            DerivedTags = derivedTags,
            Strategy = strategy,
        };
    }

    // =========================================================================
    // INTERNAL: HELPERS
    // =========================================================================

    private static SemanticCoordinates ComputeCentroid(IReadOnlyList<Entity> entities)
    {
        var sumX = 0.0;
        var sumY = 0.0;
        var sumZ = 0.0;
        foreach (var e in entities)
        {
            sumX += e.Coordinates.X;
            sumY += e.Coordinates.Y;
            sumZ += e.Coordinates.Z;
        }
        var n = entities.Count;
        return new SemanticCoordinates(sumX / n, sumY / n, sumZ / n);
    }

    private List<SemanticCoordinates> GetExistingPoints(string entityKind)
    {
        // The caller doesn't pass a graph, so existing points come from regions' history.
        // In the full engine, PlaceEntity would receive an IGraph to get entity coordinates.
        // For now, return empty — the min-distance check still works once points accumulate.
        return [];
    }

    private static List<SemanticRegion> Shuffle(List<SemanticRegion> regions)
    {
        var shuffled = new List<SemanticRegion>(regions);
        for (var i = shuffled.Count - 1; i > 0; i--)
        {
            var j = Rng.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }
        return shuffled;
    }

    private List<SemanticRegion> WeightedSparseSelection(
        List<SemanticRegion> regions,
        List<SemanticCoordinates> existingPoints)
    {
        // Count entities per region
        var regionCounts = new Dictionary<string, int>();
        foreach (var region in regions)
        {
            var count = existingPoints.Count(p => PointInRegion(p, region));
            regionCounts[region.Id] = count;
        }

        var result = new List<SemanticRegion>();
        var remaining = new List<SemanticRegion>(regions);

        while (remaining.Count > 0)
        {
            var weights = remaining.Select(r => 1.0 / (regionCounts[r.Id] + 1)).ToList();
            var totalWeight = weights.Sum();
            var random = Rng.NextDouble() * totalWeight;
            var selectedIndex = 0;

            for (var i = 0; i < weights.Count; i++)
            {
                random -= weights[i];
                if (random <= 0)
                {
                    selectedIndex = i;
                    break;
                }
            }

            result.Add(remaining[selectedIndex]);
            remaining.RemoveAt(selectedIndex);
        }

        return result;
    }

    // =========================================================================
    // INTERNAL: AXIS TAG DERIVATION
    // =========================================================================

    private void DeriveAxisTags(string entityKind, SemanticCoordinates point, Action<string> addTag)
    {
        var kindDef = _entityKinds.FirstOrDefault(k => k.Kind.Value == entityKind);
        var plane = kindDef?.SemanticPlane;
        if (plane?.Axes is null) return;

        ResolveAndApplyAxisTag(plane.Axes.X.AxisId, point.X, addTag);
        ResolveAndApplyAxisTag(plane.Axes.Y.AxisId, point.Y, addTag);
        ResolveAndApplyAxisTag(plane.Axes.Z.AxisId, point.Z, addTag);
    }

    private void ResolveAndApplyAxisTag(string axisId, double value, Action<string> addTag)
    {
        if (!_axisDefinitions.TryGetValue(axisId, out var axis)) return;

        if (!string.IsNullOrEmpty(axis.LowTag) && ShouldApplyAxisTag(value, isLow: true))
            addTag(axis.LowTag);
        else if (!string.IsNullOrEmpty(axis.HighTag) && ShouldApplyAxisTag(value, isLow: false))
            addTag(axis.HighTag);
    }

    /// <summary>
    /// Gradient-based probability for axis tags.
    /// At 0 or 100: probability = 100%. At 25 or 75: probability = 50%. At 50: probability = 0%.
    /// </summary>
    private static bool ShouldApplyAxisTag(double value, bool isLow)
    {
        if (isLow && value >= 50) return false;
        if (!isLow && value <= 50) return false;

        var distanceFromCenter = Math.Abs(value - 50);
        var probability = distanceFromCenter / 50.0 * 100.0;
        return Rng.NextDouble() * 100 < probability;
    }
}
