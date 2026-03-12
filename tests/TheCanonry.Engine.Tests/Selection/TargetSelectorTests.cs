namespace TheCanonry.Engine.Tests.Selection;

using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Selection;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

public class TargetSelectorTests
{
    private static Entity CreateEntity(
        string id, string kind, string subtype,
        string culture = "northern", double prominence = 2.0, int tick = 1)
    {
        var entity = new Entity(
            id: new EntityId(id),
            kind: new EntityKind(kind),
            subtype: subtype,
            name: $"Test {id}",
            culture: new CultureId(culture),
            eraId: new EraId("era-1"),
            coordinates: new SemanticCoordinates(0.5, 0.3, 0.1),
            createdBy: new ExecutionContext(tick, ExecutionSource.Template, "tmpl-1", true, "test"),
            tick: tick);

        if (Math.Abs(prominence - 0.0) > 1e-9)
            entity.SetProminence(new Prominence(prominence), tick);

        return entity;
    }

    private static Relationship CreateRelationship(
        string srcId, string dstId, string kind = "allied_with", int tick = 1)
    {
        return new Relationship(
            sourceId: new EntityId(srcId),
            targetId: new EntityId(dstId),
            kind: new RelationshipKind(kind),
            strength: 0.5,
            distance: 0.0,
            category: "",
            createdBy: new ExecutionContext(tick, ExecutionSource.Template, "tmpl-1", true, "test"),
            tick: tick);
    }

    [Fact]
    public void SelectTargets_EmptyGraph_ReturnsEmpty()
    {
        var selector = new TargetSelector();
        var graph = new GraphStore();

        var result = selector.SelectTargets(graph, new EntityKind("faction"), 3);

        Assert.Empty(result.Existing);
        Assert.Equal(0, result.Diagnostics.CandidatesEvaluated);
    }

    [Fact]
    public void SelectTargets_ReturnsMatchingEntities()
    {
        var selector = new TargetSelector();
        var graph = new GraphStore();
        graph.CreateEntity(CreateEntity("f1", "faction", "merchant"));
        graph.CreateEntity(CreateEntity("f2", "faction", "criminal"));
        graph.CreateEntity(CreateEntity("n1", "npc", "warrior"));

        var result = selector.SelectTargets(graph, new EntityKind("faction"), 5);

        // Should select both factions, not the npc
        Assert.Equal(2, result.Existing.Count);
        Assert.All(result.Existing, e => Assert.Equal(new EntityKind("faction"), e.Kind));
        Assert.Equal(2, result.Diagnostics.CandidatesEvaluated);
    }

    [Fact]
    public void SelectTargets_CountLimitRespected()
    {
        var selector = new TargetSelector();
        var graph = new GraphStore();
        for (var i = 0; i < 10; i++)
            graph.CreateEntity(CreateEntity($"f{i}", "faction", "merchant"));

        var result = selector.SelectTargets(graph, new EntityKind("faction"), 3);

        Assert.Equal(3, result.Existing.Count);
    }

    [Fact]
    public void SelectTargets_CultureFilter_RequireApplied()
    {
        var selector = new TargetSelector();
        var graph = new GraphStore();
        graph.CreateEntity(CreateEntity("f1", "faction", "merchant", culture: "northern"));
        graph.CreateEntity(CreateEntity("f2", "faction", "merchant", culture: "southern"));
        graph.CreateEntity(CreateEntity("f3", "faction", "merchant", culture: "northern"));

        var bias = new SelectionBias
        {
            Culture = new CultureRequirement { Require = "northern" }
        };

        var result = selector.SelectTargets(graph, new EntityKind("faction"), 10, bias);

        Assert.Equal(2, result.Existing.Count);
        Assert.All(result.Existing, e => Assert.Equal(new CultureId("northern"), e.Culture));
    }

    [Fact]
    public void SelectTargets_CultureFilter_ExcludeApplied()
    {
        var selector = new TargetSelector();
        var graph = new GraphStore();
        graph.CreateEntity(CreateEntity("f1", "faction", "merchant", culture: "northern"));
        graph.CreateEntity(CreateEntity("f2", "faction", "merchant", culture: "southern"));
        graph.CreateEntity(CreateEntity("f3", "faction", "merchant", culture: "eastern"));

        var bias = new SelectionBias
        {
            Culture = new CultureRequirement { Exclude = ["southern", "eastern"] }
        };

        var result = selector.SelectTargets(graph, new EntityKind("faction"), 10, bias);

        Assert.Single(result.Existing);
        Assert.Equal(new CultureId("northern"), result.Existing[0].Culture);
    }

    [Fact]
    public void SelectTargets_HubPenalty_ReducesScoreForHighlyConnected()
    {
        var selector = new TargetSelector();
        var graph = new GraphStore();

        // Create a hub entity with many connections
        graph.CreateEntity(CreateEntity("hub", "faction", "merchant"));
        graph.CreateEntity(CreateEntity("leaf", "faction", "merchant"));

        // Create connection targets
        for (var i = 0; i < 10; i++)
        {
            var targetId = $"t{i}";
            graph.CreateEntity(CreateEntity(targetId, "npc", "warrior"));
            graph.AddRelationship(CreateRelationship("hub", targetId, "member_of"));
        }

        // Select 1 faction - the leaf should score higher than the hub
        var bias = new SelectionBias
        {
            Avoid = new SelectionAvoidance
            {
                RelationshipKinds = ["member_of"],
                HubPenaltyStrength = 1.0
            }
        };

        var result = selector.SelectTargets(graph, new EntityKind("faction"), 1, bias);

        Assert.Single(result.Existing);
        Assert.Equal(new EntityId("leaf"), result.Existing[0].Id);
    }

    [Fact]
    public void SelectTargets_PreferenceBoost_ForMatchingSubtypes()
    {
        var selector = new TargetSelector();
        var graph = new GraphStore();
        graph.CreateEntity(CreateEntity("f1", "faction", "merchant"));
        graph.CreateEntity(CreateEntity("f2", "faction", "criminal"));

        var bias = new SelectionBias
        {
            Prefer = new SelectionPreferences
            {
                Subtypes = ["criminal"],
                PreferenceBoost = 10.0  // Very high boost to make it deterministic
            }
        };

        var result = selector.SelectTargets(graph, new EntityKind("faction"), 1, bias);

        Assert.Single(result.Existing);
        Assert.Equal("criminal", result.Existing[0].Subtype);
    }

    [Fact]
    public void SelectTargets_PreferenceBoost_ForMatchingTags()
    {
        var selector = new TargetSelector();
        var graph = new GraphStore();

        var tagged = CreateEntity("f1", "faction", "merchant");
        tagged.Tags.Set("mystic", true);
        graph.CreateEntity(tagged);

        var untagged = CreateEntity("f2", "faction", "merchant");
        graph.CreateEntity(untagged);

        var bias = new SelectionBias
        {
            Prefer = new SelectionPreferences
            {
                Tags = ["mystic"],
                PreferenceBoost = 10.0
            }
        };

        var result = selector.SelectTargets(graph, new EntityKind("faction"), 1, bias);

        Assert.Single(result.Existing);
        Assert.Equal(new EntityId("f1"), result.Existing[0].Id);
    }

    [Fact]
    public void SelectTargets_PreferenceBoost_ForMatchingProminence()
    {
        var selector = new TargetSelector();
        var graph = new GraphStore();

        // Recognized (2.5) and Forgotten (0.5)
        graph.CreateEntity(CreateEntity("f1", "faction", "merchant", prominence: 2.5));
        graph.CreateEntity(CreateEntity("f2", "faction", "merchant", prominence: 0.5));

        var bias = new SelectionBias
        {
            Prefer = new SelectionPreferences
            {
                Prominence = ["Recognized"],
                PreferenceBoost = 10.0
            }
        };

        var result = selector.SelectTargets(graph, new EntityKind("faction"), 1, bias);

        Assert.Single(result.Existing);
        Assert.Equal(new EntityId("f1"), result.Existing[0].Id);
    }

    [Fact]
    public void SelectTargets_MaxTotalRelationships_HardCap()
    {
        var selector = new TargetSelector();
        var graph = new GraphStore();
        graph.CreateEntity(CreateEntity("f1", "faction", "merchant"));
        graph.CreateEntity(CreateEntity("f2", "faction", "merchant"));

        // Give f1 3 relationships
        for (var i = 0; i < 3; i++)
        {
            var tid = $"t{i}";
            graph.CreateEntity(CreateEntity(tid, "npc", "warrior"));
            graph.AddRelationship(CreateRelationship("f1", tid));
        }

        var bias = new SelectionBias
        {
            Avoid = new SelectionAvoidance { MaxTotalRelationships = 3 }
        };

        var result = selector.SelectTargets(graph, new EntityKind("faction"), 10, bias);

        // f1 has 3 relationships (>= cap of 3), so only f2 should be selected
        Assert.Single(result.Existing);
        Assert.Equal(new EntityId("f2"), result.Existing[0].Id);
    }

    [Fact]
    public void SelectTargets_Diagnostics_ReportsScoreStats()
    {
        var selector = new TargetSelector();
        var graph = new GraphStore();
        graph.CreateEntity(CreateEntity("f1", "faction", "merchant"));
        graph.CreateEntity(CreateEntity("f2", "faction", "criminal"));

        var result = selector.SelectTargets(graph, new EntityKind("faction"), 2);

        Assert.Equal(2, result.Diagnostics.CandidatesEvaluated);
        Assert.True(result.Diagnostics.BestScore > 0);
        Assert.True(result.Diagnostics.AvgScore > 0);
    }
}
