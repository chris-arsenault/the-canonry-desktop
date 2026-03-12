namespace TheCanonry.Engine.Tests.Graph;

using TheCanonry.Engine.Graph;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

public class RelationshipBuilderTests
{
    private static readonly ExecutionContext TestContext =
        new(1, ExecutionSource.Template, "tmpl-1", true, "test");

    [Fact]
    public void Add_CreatesRelationship()
    {
        var builder = new RelationshipBuilder(1, TestContext);

        builder.Add(
            new RelationshipKind("allied_with"),
            new EntityId("f1"),
            new EntityId("f2"),
            0.7);

        var rels = builder.Build();

        Assert.Single(rels);
        Assert.Equal(new RelationshipKind("allied_with"), rels[0].Kind);
        Assert.Equal(new EntityId("f1"), rels[0].SourceId);
        Assert.Equal(new EntityId("f2"), rels[0].TargetId);
        Assert.Equal(0.7, rels[0].Strength);
    }

    [Fact]
    public void AddBidirectional_CreatesTwoRelationships()
    {
        var builder = new RelationshipBuilder(1, TestContext);

        builder.AddBidirectional(
            new RelationshipKind("trade_with"),
            new EntityId("f1"),
            new EntityId("f2"),
            0.5);

        var rels = builder.Build();

        Assert.Equal(2, rels.Count);
        Assert.Equal(new EntityId("f1"), rels[0].SourceId);
        Assert.Equal(new EntityId("f2"), rels[0].TargetId);
        Assert.Equal(new EntityId("f2"), rels[1].SourceId);
        Assert.Equal(new EntityId("f1"), rels[1].TargetId);
    }

    [Fact]
    public void FluentChaining_Works()
    {
        var builder = new RelationshipBuilder(1, TestContext);

        var rels = builder
            .Add(new RelationshipKind("allied_with"), new EntityId("f1"), new EntityId("f2"))
            .Add(new RelationshipKind("rival_of"), new EntityId("f1"), new EntityId("f3"))
            .AddBidirectional(new RelationshipKind("trade_with"), new EntityId("f2"), new EntityId("f3"))
            .Build();

        Assert.Equal(4, rels.Count);
    }

    [Fact]
    public void Build_ClearsList()
    {
        var builder = new RelationshipBuilder(1, TestContext);

        builder.Add(new RelationshipKind("allied_with"), new EntityId("f1"), new EntityId("f2"));
        var first = builder.Build();
        var second = builder.Build();

        Assert.Single(first);
        Assert.Empty(second);
    }

    [Fact]
    public void Count_TracksAccurately()
    {
        var builder = new RelationshipBuilder(1, TestContext);

        Assert.Equal(0, builder.Count);

        builder.Add(new RelationshipKind("a"), new EntityId("f1"), new EntityId("f2"));
        Assert.Equal(1, builder.Count);

        builder.AddBidirectional(new RelationshipKind("b"), new EntityId("f3"), new EntityId("f4"));
        Assert.Equal(3, builder.Count);
    }

    [Fact]
    public void DefaultStrength_Is_0_5()
    {
        var builder = new RelationshipBuilder(1, TestContext);

        builder.Add(new RelationshipKind("allied_with"), new EntityId("f1"), new EntityId("f2"));
        var rels = builder.Build();

        Assert.Equal(0.5, rels[0].Strength);
    }
}
