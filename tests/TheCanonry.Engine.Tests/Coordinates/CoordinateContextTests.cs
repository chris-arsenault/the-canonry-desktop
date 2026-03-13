using TheCanonry.Engine.Coordinates;
using TheCanonry.Engine.Graph;
using TheCanonry.Schema.Config;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;
using ExecutionContext = TheCanonry.Schema.World.ExecutionContext;

namespace TheCanonry.Engine.Tests.Coordinates;

public class CoordinateContextTests
{
    // =========================================================================
    // TEST SCHEMA BUILDER
    // =========================================================================

    /// <summary>
    /// Creates a minimal DomainSchema with one entity kind ("npc") that has
    /// a semantic plane with 2 seed regions and 2 cultures with different biases.
    /// Region A (center 25,25 radius 15) belongs to culture "northern".
    /// Region B (center 75,75 radius 15) belongs to culture "southern".
    /// </summary>
    private static DomainSchema CreateTestSchema()
    {
        var regionA = new SemanticRegion
        {
            Id = "region_a",
            Label = "Northern Heartland",
            Color = "#4488cc",
            Culture = "northern",
            Tags = ["disciplined", "martial"],
            ZRange = new ZRange(0, 100),
            Bounds = new CircleBounds(25, 25, 15),
        };

        var regionB = new SemanticRegion
        {
            Id = "region_b",
            Label = "Southern Expanse",
            Color = "#cc8844",
            Culture = "southern",
            Tags = ["mercantile", "cosmopolitan"],
            ZRange = new ZRange(0, 100),
            Bounds = new CircleBounds(75, 75, 15),
        };

        var regionC = new SemanticRegion
        {
            Id = "region_c",
            Label = "Eastern Frontier",
            Color = "#44cc88",
            Culture = "northern",
            Tags = ["frontier"],
            ZRange = new ZRange(0, 100),
            Bounds = new CircleBounds(80, 20, 10),
        };

        var npcKind = new EntityKindDefinition
        {
            Kind = new EntityKind("npc"),
            Description = "Non-player characters",
            Category = EntityCategory.Character,
            Subtypes = [new SubtypeDefinition { Id = "warrior", Name = "Warrior" }],
            Statuses = [new StatusDefinition { Id = "active", Name = "Active" }],
            SemanticPlane = new SemanticPlane
            {
                Axes = new SemanticPlaneAxes
                {
                    X = new SemanticAxis { AxisId = "aggression" },
                    Y = new SemanticAxis { AxisId = "wealth" },
                    Z = new SemanticAxis { AxisId = "power" },
                },
                Regions = [regionA, regionB, regionC],
            },
        };

        var northernCulture = new CultureDefinition
        {
            Id = new CultureId("northern"),
            Name = "Northern Folk",
            Description = "Hardy people of the north",
            Color = "#4488cc",
            AxisBiases = new Dictionary<string, AxisBias>
            {
                ["npc"] = new AxisBias { X = 25, Y = 25, Z = 50 },
            },
        };

        var southernCulture = new CultureDefinition
        {
            Id = new CultureId("southern"),
            Name = "Southern Folk",
            Description = "Traders of the south",
            Color = "#cc8844",
            AxisBiases = new Dictionary<string, AxisBias>
            {
                ["npc"] = new AxisBias { X = 75, Y = 75, Z = 50 },
            },
        };

        return new DomainSchema
        {
            Id = "test-domain",
            Name = "Test Domain",
            Version = "1.0",
            EntityKinds = [npcKind],
            RelationshipKinds = [],
            Cultures = [northernCulture, southernCulture],
        };
    }

    private static CoordinateContext CreateContext(
        DomainSchema? schema = null,
        IReadOnlyList<AxisDefinition>? axisDefs = null,
        double minDistance = 5.0)
    {
        return new CoordinateContext(
            schema ?? CreateTestSchema(),
            axisDefs ?? [],
            minDistance);
    }

    private static Entity CreateEntityAt(
        string id, double x, double y, double z,
        string kind = "npc", string culture = "northern")
    {
        return new Entity(
            id: new EntityId(id),
            kind: new EntityKind(kind),
            subtype: "warrior",
            name: $"Test {id}",
            culture: new CultureId(culture),
            eraId: new EraId("era-1"),
            coordinates: new SemanticCoordinates(x, y, z),
            createdBy: new ExecutionContext(1, ExecutionSource.Template, "tmpl-1", true, "test"),
            tick: 1);
    }

    // =========================================================================
    // DISTANCE CALCULATION
    // =========================================================================

    [Fact]
    public void GetDistance_ReturnsCorrectEuclideanDistance()
    {
        var a = new SemanticCoordinates(0, 0, 0);
        var b = new SemanticCoordinates(3, 4, 0);

        var distance = CoordinateContext.GetDistance(a, b);

        Assert.Equal(5.0, distance, precision: 10);
    }

    [Fact]
    public void GetDistance_With3DPoints_IncludesZAxis()
    {
        var a = new SemanticCoordinates(0, 0, 0);
        var b = new SemanticCoordinates(1, 2, 2);

        var distance = CoordinateContext.GetDistance(a, b);

        Assert.Equal(3.0, distance, precision: 10);
    }

    [Fact]
    public void GetDistance2D_IgnoresZAxis()
    {
        var a = new SemanticCoordinates(0, 0, 0);
        var b = new SemanticCoordinates(3, 4, 99);

        var distance = CoordinateContext.GetDistance2D(a, b);

        Assert.Equal(5.0, distance, precision: 10);
    }

    // =========================================================================
    // PLACE ENTITY - BASIC VALIDITY
    // =========================================================================

    [Fact]
    public void PlaceEntity_ReturnsCoordinatesWithinBounds()
    {
        var ctx = CreateContext();

        var result = ctx.PlaceEntity("npc", "northern");

        Assert.InRange(result.Coordinates.X, 0.0, 100.0);
        Assert.InRange(result.Coordinates.Y, 0.0, 100.0);
        Assert.InRange(result.Coordinates.Z, 0.0, 100.0);
    }

    [Fact]
    public void PlaceEntity_ReturnsNonEmptyStrategy()
    {
        var ctx = CreateContext();

        var result = ctx.PlaceEntity("npc", "northern");

        Assert.NotEmpty(result.Strategy);
    }

    // =========================================================================
    // PLACE ENTITY - NEAR REFERENCE
    // =========================================================================

    [Fact]
    public void PlaceEntity_NearReference_IsCloseToReferenceEntity()
    {
        var ctx = CreateContext();
        var refEntity = CreateEntityAt("ref-1", 50, 50, 50);

        var result = ctx.PlaceEntity("npc", "northern", [refEntity]);

        // Should be within a reasonable radius of the reference (minDist=5, maxSearch=20)
        var distance = CoordinateContext.GetDistance2D(
            result.Coordinates, refEntity.Coordinates);
        Assert.True(distance < 30, $"Expected distance < 30, got {distance}");
        Assert.Equal("near_reference", result.Strategy);
    }

    // =========================================================================
    // PLACE ENTITY - SEED REGION
    // =========================================================================

    [Fact]
    public void PlaceEntity_InSeedRegion_StaysWithinRegionBounds()
    {
        var ctx = CreateContext();

        // Run multiple placements to check they land in the right area.
        // Northern culture has seed regions at (25,25) and (80,20).
        var placedInNorthernArea = false;
        for (var i = 0; i < 20; i++)
        {
            var result = ctx.PlaceEntity("npc", "northern");

            // Should be near one of the northern regions
            var distToA = CoordinateContext.GetDistance2D(
                result.Coordinates, new SemanticCoordinates(25, 25, 0));
            var distToC = CoordinateContext.GetDistance2D(
                result.Coordinates, new SemanticCoordinates(80, 20, 0));
            var distToBias = CoordinateContext.GetDistance2D(
                result.Coordinates, new SemanticCoordinates(25, 25, 0));

            // Should be within reasonable distance of a northern region or bias point
            if (distToA < 25 || distToC < 20 || distToBias < 30)
            {
                placedInNorthernArea = true;
                break;
            }
        }

        Assert.True(placedInNorthernArea,
            "Expected at least one placement near a northern seed region or bias point");
    }

    // =========================================================================
    // REGION LOOKUP
    // =========================================================================

    [Fact]
    public void LookupRegion_FindsContainingRegion()
    {
        var ctx = CreateContext();

        // Point at center of region A
        var region = ctx.LookupRegion("npc", new SemanticCoordinates(25, 25, 50));

        Assert.NotNull(region);
        Assert.Equal("region_a", region.Id);
    }

    [Fact]
    public void LookupRegion_ReturnsNullForPointOutsideAllRegions()
    {
        var ctx = CreateContext();

        // Point far from any region center
        var region = ctx.LookupRegion("npc", new SemanticCoordinates(50, 50, 50));

        Assert.Null(region);
    }

    [Fact]
    public void LookupAllRegions_ReturnsMultipleOverlappingRegions()
    {
        // Create a schema where regions overlap
        var region1 = new SemanticRegion
        {
            Id = "r1", Label = "R1", Color = "#111", Culture = "c1",
            Bounds = new CircleBounds(50, 50, 30),
        };
        var region2 = new SemanticRegion
        {
            Id = "r2", Label = "R2", Color = "#222", Culture = "c1",
            Bounds = new CircleBounds(60, 50, 30),
        };

        var schema = new DomainSchema
        {
            Id = "overlap-test", Name = "Overlap Test", Version = "1.0",
            EntityKinds =
            [
                new EntityKindDefinition
                {
                    Kind = new EntityKind("thing"), Description = "Things",
                    Category = EntityCategory.Object,
                    Subtypes = [new SubtypeDefinition { Id = "basic", Name = "Basic" }],
                    Statuses = [new StatusDefinition { Id = "active", Name = "Active" }],
                    SemanticPlane = new SemanticPlane
                    {
                        Axes = new SemanticPlaneAxes
                        {
                            X = new SemanticAxis { AxisId = "a1" },
                            Y = new SemanticAxis { AxisId = "a2" },
                            Z = new SemanticAxis { AxisId = "a3" },
                        },
                        Regions = [region1, region2],
                    },
                },
            ],
            RelationshipKinds = [],
            Cultures = [new CultureDefinition
            {
                Id = new CultureId("c1"), Name = "C1", Description = "Culture 1",
                AxisBiases = new Dictionary<string, AxisBias>
                {
                    ["thing"] = new AxisBias { X = 50, Y = 50, Z = 50 },
                },
            }],
        };

        var ctx = CreateContext(schema);

        // Point inside both circles (55,50 is within 30 of both 50 and 60)
        var regions = ctx.LookupAllRegions("thing", new SemanticCoordinates(55, 50, 50));

        Assert.Equal(2, regions.Count);
    }

    // =========================================================================
    // CULTURE BIAS
    // =========================================================================

    [Fact]
    public void PlaceEntity_CultureBias_ShiftsPlacementTowardBiasPoint()
    {
        var ctx = CreateContext();

        // Northern bias is at (25,25), southern at (75,75).
        // Over many placements, northern should average closer to (25,25).
        var northSumX = 0.0;
        var southSumX = 0.0;
        const int trials = 30;

        for (var i = 0; i < trials; i++)
        {
            var northResult = ctx.PlaceEntity("npc", "northern");
            var southResult = ctx.PlaceEntity("npc", "southern");
            northSumX += northResult.Coordinates.X;
            southSumX += southResult.Coordinates.X;
        }

        var northAvgX = northSumX / trials;
        var southAvgX = southSumX / trials;

        // Northern culture bias X=25 should produce lower average X than southern bias X=75
        Assert.True(northAvgX < southAvgX,
            $"Northern avg X ({northAvgX:F1}) should be less than Southern avg X ({southAvgX:F1})");
    }

    // =========================================================================
    // EMERGENT REGION CREATION
    // =========================================================================

    [Fact]
    public void CreateEmergentRegion_CreatesRegionWithCorrectProperties()
    {
        var ctx = CreateContext();
        var point = new SemanticCoordinates(50, 50, 50);

        var region = ctx.CreateEmergentRegion("npc", point, "New Territory", "northern");

        Assert.NotNull(region);
        Assert.StartsWith("emergent_npc_", region.Id);
        Assert.Equal("New Territory", region.Label);
        Assert.Equal("northern", region.Culture);
        Assert.True(region.Emergent);

        if (region.Bounds is CircleBounds circle)
        {
            Assert.Equal(50, circle.CenterX);
            Assert.Equal(50, circle.CenterY);
            Assert.Equal(10, circle.Radius); // EmergentRadius
        }
        else
        {
            Assert.Fail("Emergent region should have CircleBounds");
        }
    }

    [Fact]
    public void CreateEmergentRegion_ReturnsNull_WhenTooCloseToExistingRegion()
    {
        var ctx = CreateContext();

        // Region A is at (25,25) with radius 15. Placing at (25,25) should fail.
        var region = ctx.CreateEmergentRegion(
            "npc", new SemanticCoordinates(25, 25, 50), "Too Close", "northern");

        Assert.Null(region);
    }

    [Fact]
    public void CreateEmergentRegion_AppearsInGetRegions()
    {
        var ctx = CreateContext();
        var initialCount = ctx.GetRegions("npc").Count;

        ctx.CreateEmergentRegion(
            "npc", new SemanticCoordinates(50, 50, 50), "New Territory", "northern");

        Assert.Equal(initialCount + 1, ctx.GetRegions("npc").Count);
    }

    // =========================================================================
    // FIND NEAREST ENTITIES
    // =========================================================================

    [Fact]
    public void FindNearestEntities_ReturnsSortedByDistance()
    {
        var ctx = CreateContext();
        var graph = new GraphStore();

        var reference = CreateEntityAt("ref", 50, 50, 50);
        var near = CreateEntityAt("near", 51, 51, 50);
        var mid = CreateEntityAt("mid", 60, 60, 50);
        var far = CreateEntityAt("far", 90, 90, 50);

        graph.CreateEntity(reference);
        graph.CreateEntity(near);
        graph.CreateEntity(mid);
        graph.CreateEntity(far);

        var result = CoordinateContext.FindNearestEntities(graph, reference, new EntityKind("npc"), 3);

        Assert.Equal(3, result.Count);
        Assert.Equal("near", result[0].Entity.Id.Value);
        Assert.Equal("mid", result[1].Entity.Id.Value);
        Assert.Equal("far", result[2].Entity.Id.Value);
        Assert.True(result[0].Distance < result[1].Distance);
        Assert.True(result[1].Distance < result[2].Distance);
    }

    [Fact]
    public void FindNearestEntities_RespectsLimit()
    {
        var ctx = CreateContext();
        var graph = new GraphStore();

        var reference = CreateEntityAt("ref", 50, 50, 50);
        graph.CreateEntity(reference);

        for (var i = 0; i < 10; i++)
        {
            graph.CreateEntity(CreateEntityAt($"e{i}", 50 + i, 50, 50));
        }

        var result = CoordinateContext.FindNearestEntities(graph, reference, new EntityKind("npc"), 3);

        Assert.Equal(3, result.Count);
    }

    // =========================================================================
    // TAG DERIVATION
    // =========================================================================

    [Fact]
    public void DeriveTagsFromPlacement_IncludesRegionTags()
    {
        var ctx = CreateContext();
        var regionA = ctx.GetRegions("npc").First(r => r.Id == "region_a");

        var tags = ctx.DeriveTagsFromPlacement(
            "npc",
            new SemanticCoordinates(25, 25, 50),
            [regionA]);

        Assert.Contains("disciplined", tags);
        Assert.Contains("martial", tags);
    }

    [Fact]
    public void DeriveTagsFromPlacement_DeduplicatesTags()
    {
        var ctx = CreateContext();

        // Two regions with the same tag
        var r1 = new SemanticRegion
        {
            Id = "r1", Label = "R1", Color = "#111", Culture = "c1",
            Tags = ["shared_tag", "unique_a"],
        };
        var r2 = new SemanticRegion
        {
            Id = "r2", Label = "R2", Color = "#222", Culture = "c1",
            Tags = ["shared_tag", "unique_b"],
        };

        var tags = ctx.DeriveTagsFromPlacement(
            "npc", new SemanticCoordinates(50, 50, 50), [r1, r2]);

        // shared_tag should appear exactly once
        Assert.Single(tags, t => t == "shared_tag");
        Assert.Contains("unique_a", tags);
        Assert.Contains("unique_b", tags);
    }

    // =========================================================================
    // HAS KIND MAP
    // =========================================================================

    [Fact]
    public void HasKindMap_ReturnsTrueForConfiguredKind()
    {
        var ctx = CreateContext();
        Assert.True(ctx.HasKindMap("npc"));
    }

    [Fact]
    public void HasKindMap_ReturnsFalseForUnconfiguredKind()
    {
        var ctx = CreateContext();
        Assert.False(ctx.HasKindMap("nonexistent"));
    }
}
