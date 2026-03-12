namespace TheCanonry.Engine.Tests.Rules;

using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Rules;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;

using static TestHelpers;

public class FilterEvaluatorTests
{
    // =========================================================================
    // STATUS FILTER
    // =========================================================================

    [Fact]
    public void HasStatusFilter_KeepsMatchingEntities()
    {
        var e1 = CreateTestEntity("f1", status: "active");
        var e2 = CreateTestEntity("f2", status: "historical");
        var graph = CreateGraphWithEntities(e1, e2);
        var ctx = CreateSystemContext(graph);

        var filter = new HasStatusFilter("active");
        var result = FilterEvaluator.ApplyFilter([e1, e2], filter, ctx);

        Assert.Single(result);
        Assert.Equal(new EntityId("f1"), result[0].Id);
    }

    [Fact]
    public void HasStatusFilter_RejectsNonMatching()
    {
        var e1 = CreateTestEntity("f1", status: "historical");
        var graph = CreateGraphWithEntities(e1);
        var ctx = CreateSystemContext(graph);

        var filter = new HasStatusFilter("active");
        var result = FilterEvaluator.ApplyFilter([e1], filter, ctx);

        Assert.Empty(result);
    }

    // =========================================================================
    // TAG FILTERS
    // =========================================================================

    [Fact]
    public void HasTagFilter_KeepsEntitiesWithTag()
    {
        var e1 = CreateTestEntity("f1");
        e1.Tags.Set("warlike", true);
        var e2 = CreateTestEntity("f2");
        var graph = CreateGraphWithEntities(e1, e2);
        var ctx = CreateSystemContext(graph);

        var filter = new HasTagFilter("warlike", "");
        var result = FilterEvaluator.ApplyFilter([e1, e2], filter, ctx);

        Assert.Single(result);
        Assert.Equal(new EntityId("f1"), result[0].Id);
    }

    [Fact]
    public void HasTagFilter_RejectsEntitiesWithoutTag()
    {
        var e1 = CreateTestEntity("f1");
        var graph = CreateGraphWithEntities(e1);
        var ctx = CreateSystemContext(graph);

        var filter = new HasTagFilter("warlike", "");
        var result = FilterEvaluator.ApplyFilter([e1], filter, ctx);

        Assert.Empty(result);
    }

    [Fact]
    public void HasTagsFilter_RequiresAllTags()
    {
        var e1 = CreateTestEntity("f1");
        e1.Tags.Set("warlike", true);
        e1.Tags.Set("aggressive", true);
        var e2 = CreateTestEntity("f2");
        e2.Tags.Set("warlike", true);
        var graph = CreateGraphWithEntities(e1, e2);
        var ctx = CreateSystemContext(graph);

        var filter = new HasTagsFilter(["warlike", "aggressive"]);
        var result = FilterEvaluator.ApplyFilter([e1, e2], filter, ctx);

        Assert.Single(result);
        Assert.Equal(new EntityId("f1"), result[0].Id);
    }

    // =========================================================================
    // KIND FILTER (via HasStatusFilter as proxy, using subtype for discrimination)
    // =========================================================================

    [Fact]
    public void HasProminenceFilter_FiltersCorrectly()
    {
        var e1 = CreateTestEntity("f1", prominence: 3.5); // renowned
        var e2 = CreateTestEntity("f2", prominence: 1.5); // marginal
        var e3 = CreateTestEntity("f3", prominence: 4.5); // mythic
        var graph = CreateGraphWithEntities(e1, e2, e3);
        var ctx = CreateSystemContext(graph);

        // "renowned" threshold = 3.0
        var filter = new HasProminenceFilter("renowned");
        var result = FilterEvaluator.ApplyFilter([e1, e2, e3], filter, ctx);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Id == new EntityId("f1"));
        Assert.Contains(result, e => e.Id == new EntityId("f3"));
    }

    // =========================================================================
    // CULTURE FILTER
    // =========================================================================

    [Fact]
    public void HasCultureFilter_KeepsMatchingCulture()
    {
        var e1 = CreateTestEntity("f1", culture: "northern");
        var e2 = CreateTestEntity("f2", culture: "southern");
        var graph = CreateGraphWithEntities(e1, e2);
        var ctx = CreateSystemContext(graph);

        var filter = new HasCultureFilter("northern");
        var result = FilterEvaluator.ApplyFilter([e1, e2], filter, ctx);

        Assert.Single(result);
        Assert.Equal(new EntityId("f1"), result[0].Id);
    }

    // =========================================================================
    // MULTIPLE FILTERS (AND COMPOSITION)
    // =========================================================================

    [Fact]
    public void ApplyFilters_CombinesFiltersWithAndLogic()
    {
        var e1 = CreateTestEntity("f1", status: "active", prominence: 3.5);
        e1.Tags.Set("warlike", true);
        var e2 = CreateTestEntity("f2", status: "active", prominence: 1.5);
        e2.Tags.Set("warlike", true);
        var e3 = CreateTestEntity("f3", status: "historical", prominence: 4.0);
        e3.Tags.Set("warlike", true);
        var graph = CreateGraphWithEntities(e1, e2, e3);
        var ctx = CreateSystemContext(graph);

        var filters = new List<SelectionFilter>
        {
            new HasStatusFilter("active"),
            new HasTagFilter("warlike", ""),
            new HasProminenceFilter("renowned") // threshold 3.0
        };

        var result = FilterEvaluator.ApplyFilters([e1, e2, e3], filters, ctx);

        // Only e1 is active, has "warlike" tag, and is renowned
        Assert.Single(result);
        Assert.Equal(new EntityId("f1"), result[0].Id);
    }

    // =========================================================================
    // ENTITY PASSES FILTER
    // =========================================================================

    [Fact]
    public void EntityPassesFilter_ReturnsTrueForMatch()
    {
        var e1 = CreateTestEntity("f1", status: "active");
        var graph = CreateGraphWithEntities(e1);
        var ctx = CreateSystemContext(graph);

        Assert.True(FilterEvaluator.EntityPassesFilter(e1, new HasStatusFilter("active"), ctx));
        Assert.False(FilterEvaluator.EntityPassesFilter(e1, new HasStatusFilter("historical"), ctx));
    }

    // =========================================================================
    // LACKS TAG FILTER
    // =========================================================================

    [Fact]
    public void LacksAnyTagFilter_ExcludesEntitiesWithAnyTag()
    {
        var e1 = CreateTestEntity("f1");
        e1.Tags.Set("warlike", true);
        var e2 = CreateTestEntity("f2");
        e2.Tags.Set("peaceful", true);
        var e3 = CreateTestEntity("f3");
        var graph = CreateGraphWithEntities(e1, e2, e3);
        var ctx = CreateSystemContext(graph);

        var filter = new LacksAnyTagFilter(["warlike", "aggressive"]);
        var result = FilterEvaluator.ApplyFilter([e1, e2, e3], filter, ctx);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Id == new EntityId("f2"));
        Assert.Contains(result, e => e.Id == new EntityId("f3"));
    }
}
