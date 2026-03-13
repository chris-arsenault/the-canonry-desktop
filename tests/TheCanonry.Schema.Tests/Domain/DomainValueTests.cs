using TheCanonry.Schema.Domain;

namespace TheCanonry.Schema.Tests.Domain;

public class DomainValueTests
{
    [Fact]
    public void EntityKind_wraps_string()
    {
        var kind = new EntityKind("faction");
        Assert.Equal("faction", kind.Value);
    }

    [Fact]
    public void EntityKind_and_RelationshipKind_are_distinct_types()
    {
        var ek = new EntityKind("alliance");
        var rk = new RelationshipKind("alliance");
        Assert.NotEqual(ek.GetType(), rk.GetType());
    }

    [Fact]
    public void Prominence_label_derived_from_value()
    {
        Assert.Equal("Forgotten", new Prominence(0.5).Label);
        Assert.Equal("Marginal", new Prominence(1.5).Label);
        Assert.Equal("Recognized", new Prominence(2.5).Label);
        Assert.Equal("Renowned", new Prominence(3.5).Label);
        Assert.Equal("Mythic", new Prominence(4.5).Label);
    }

    [Fact]
    public void Prominence_equality_is_by_value()
    {
        Assert.Equal(new Prominence(2.0), new Prominence(2.0));
        Assert.NotEqual(new Prominence(1.0), new Prominence(2.0));
    }
}
