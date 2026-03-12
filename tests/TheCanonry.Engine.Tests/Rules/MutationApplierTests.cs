namespace TheCanonry.Engine.Tests.Rules;

using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Rules;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;

using static TestHelpers;

public class MutationApplierTests
{
    // =========================================================================
    // SET TAG
    // =========================================================================

    [Fact]
    public void SetTagMutation_AddsTagToEntity()
    {
        var entity = CreateTestEntity("f1");
        var graph = CreateGraphWithEntities(entity);
        var ctx = CreateSystemContext(graph, self: entity);

        var mutation = new SetTagMutation("$self", "warlike", "true", "");
        var applied = MutationApplier.Apply(mutation, ctx);

        Assert.True(applied);
        Assert.True(entity.Tags.Contains("warlike"));
    }

    [Fact]
    public void SetTagMutation_SetsStringValue()
    {
        var entity = CreateTestEntity("f1");
        var graph = CreateGraphWithEntities(entity);
        var ctx = CreateSystemContext(graph, self: entity);

        var mutation = new SetTagMutation("$self", "role", "leader", "");
        var applied = MutationApplier.Apply(mutation, ctx);

        Assert.True(applied);
        Assert.Equal("leader", entity.Tags.GetString("role"));
    }

    // =========================================================================
    // REMOVE TAG
    // =========================================================================

    [Fact]
    public void RemoveTagMutation_RemovesTag()
    {
        var entity = CreateTestEntity("f1");
        entity.Tags.Set("warlike", true);
        var graph = CreateGraphWithEntities(entity);
        var ctx = CreateSystemContext(graph, self: entity);

        var mutation = new RemoveTagMutation("$self", "warlike");
        var applied = MutationApplier.Apply(mutation, ctx);

        Assert.True(applied);
        Assert.False(entity.Tags.Contains("warlike"));
    }

    // =========================================================================
    // CHANGE STATUS
    // =========================================================================

    [Fact]
    public void ChangeStatusMutation_ChangesEntityStatus()
    {
        var entity = CreateTestEntity("f1");
        var graph = CreateGraphWithEntities(entity);
        var ctx = CreateSystemContext(graph, self: entity);

        var mutation = new ChangeStatusMutation("$self", "historical");
        var applied = MutationApplier.Apply(mutation, ctx);

        Assert.True(applied);
        Assert.Equal(new EntityStatus("historical"), entity.Status);
    }

    // =========================================================================
    // MODIFY PRESSURE
    // =========================================================================

    [Fact]
    public void ModifyPressureMutation_AdjustsPressure()
    {
        var graph = new GraphStore();
        graph.Pressures["conflict"] = 0.5;
        var ctx = CreateSystemContext(graph);

        var mutation = new ModifyPressureMutation("conflict", Delta: 0.3);
        var applied = MutationApplier.Apply(mutation, ctx);

        Assert.True(applied);
        Assert.Equal(0.8, graph.Pressures["conflict"], 3);
    }

    [Fact]
    public void ModifyPressureMutation_CreatesNewPressure()
    {
        var graph = new GraphStore();
        var ctx = CreateSystemContext(graph);

        var mutation = new ModifyPressureMutation("newPressure", Delta: 1.5);
        var applied = MutationApplier.Apply(mutation, ctx);

        Assert.True(applied);
        Assert.Equal(1.5, graph.Pressures["newPressure"], 3);
    }

    // =========================================================================
    // ADJUST PROMINENCE
    // =========================================================================

    [Fact]
    public void AdjustProminenceMutation_IncreasesProminence()
    {
        var entity = CreateTestEntity("f1", prominence: 2.0);
        var graph = CreateGraphWithEntities(entity);
        var ctx = CreateSystemContext(graph, self: entity);

        var mutation = new AdjustProminenceMutation("$self", Delta: 1.0);
        var applied = MutationApplier.Apply(mutation, ctx);

        Assert.True(applied);
        Assert.Equal(3.0, entity.Prominence.Value, 3);
    }

    [Fact]
    public void AdjustProminenceMutation_ClampsToRange()
    {
        var entity = CreateTestEntity("f1", prominence: 4.5);
        var graph = CreateGraphWithEntities(entity);
        var ctx = CreateSystemContext(graph, self: entity);

        // Try to push above 5.0
        var mutation = new AdjustProminenceMutation("$self", Delta: 2.0);
        var applied = MutationApplier.Apply(mutation, ctx);

        Assert.True(applied);
        Assert.Equal(5.0, entity.Prominence.Value, 3);
    }

    [Fact]
    public void AdjustProminenceMutation_SkipsLockedProminence()
    {
        var entity = CreateTestEntity("f1", prominence: 2.0);
        entity.Tags.Set("prominence_locked", true);
        var graph = CreateGraphWithEntities(entity);
        var ctx = CreateSystemContext(graph, self: entity);

        var mutation = new AdjustProminenceMutation("$self", Delta: 1.0);
        var applied = MutationApplier.Apply(mutation, ctx);

        Assert.False(applied);
        Assert.Equal(2.0, entity.Prominence.Value, 3);
    }

    // =========================================================================
    // CREATE RELATIONSHIP
    // =========================================================================

    [Fact]
    public void CreateRelationshipMutation_AddsRelationship()
    {
        var e1 = CreateTestEntity("f1");
        var e2 = CreateTestEntity("f2");
        var graph = CreateGraphWithEntities(e1, e2);
        var ctx = CreateSystemContext(graph, self: e1);

        var mutation = new CreateRelationshipMutation(
            "$self", "f2", "allied_with", 0.8, Bidirectional: false, "");
        var applied = MutationApplier.Apply(mutation, ctx);

        Assert.True(applied);
        Assert.True(graph.HasRelationship(new EntityId("f1"), new EntityId("f2"),
            new RelationshipKind("allied_with")));
    }

    [Fact]
    public void CreateRelationshipMutation_Bidirectional_AddsBothDirections()
    {
        var e1 = CreateTestEntity("f1");
        var e2 = CreateTestEntity("f2");
        var graph = CreateGraphWithEntities(e1, e2);
        var ctx = CreateSystemContext(graph, self: e1);

        var mutation = new CreateRelationshipMutation(
            "$self", "f2", "allied_with", 0.8, Bidirectional: true, "");
        var applied = MutationApplier.Apply(mutation, ctx);

        Assert.True(applied);
        Assert.True(graph.HasRelationship(new EntityId("f1"), new EntityId("f2")));
        Assert.True(graph.HasRelationship(new EntityId("f2"), new EntityId("f1")));
    }

    // =========================================================================
    // CONDITIONAL ACTION
    // =========================================================================

    [Fact]
    public void ConditionalAction_ExecutesThenBranch()
    {
        var entity = CreateTestEntity("f1");
        var graph = CreateGraphWithEntities(entity);
        graph.Pressures["conflict"] = 0.8;
        var ctx = CreateSystemContext(graph, self: entity);

        var mutation = new ConditionalAction(
            Condition: new PressureCondition("conflict", Min: 0.5, Max: 1.0),
            ThenActions: [new SetTagMutation("$self", "war_footing", "true", "")],
            ElseActions: [new SetTagMutation("$self", "peaceful", "true", "")]);

        MutationApplier.Apply(mutation, ctx);

        Assert.True(entity.Tags.Contains("war_footing"));
        Assert.False(entity.Tags.Contains("peaceful"));
    }

    [Fact]
    public void ConditionalAction_ExecutesElseBranch()
    {
        var entity = CreateTestEntity("f1");
        var graph = CreateGraphWithEntities(entity);
        graph.Pressures["conflict"] = 0.1;
        var ctx = CreateSystemContext(graph, self: entity);

        var mutation = new ConditionalAction(
            Condition: new PressureCondition("conflict", Min: 0.5, Max: 1.0),
            ThenActions: [new SetTagMutation("$self", "war_footing", "true", "")],
            ElseActions: [new SetTagMutation("$self", "peaceful", "true", "")]);

        MutationApplier.Apply(mutation, ctx);

        Assert.False(entity.Tags.Contains("war_footing"));
        Assert.True(entity.Tags.Contains("peaceful"));
    }
}
