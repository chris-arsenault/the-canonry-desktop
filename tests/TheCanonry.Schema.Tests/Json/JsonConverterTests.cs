using System.Text.Json;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Json;

namespace TheCanonry.Schema.Tests.Json;

public class JsonConverterTests
{
    private static readonly JsonSerializerOptions Options = DomainJsonOptions.Default;

    [Fact]
    public void EntityKind_serializes_as_plain_string()
    {
        var kind = new EntityKind("faction");
        var json = JsonSerializer.Serialize(kind, Options);
        Assert.Equal("\"faction\"", json);
    }

    [Fact]
    public void EntityKind_deserializes_from_plain_string()
    {
        var kind = JsonSerializer.Deserialize<EntityKind>("\"faction\"", Options);
        Assert.Equal(new EntityKind("faction"), kind);
    }

    [Fact]
    public void RelationshipKind_round_trips()
    {
        var kind = new RelationshipKind("alliance");
        var json = JsonSerializer.Serialize(kind, Options);
        var back = JsonSerializer.Deserialize<RelationshipKind>(json, Options);
        Assert.Equal(kind, back);
    }

    [Fact]
    public void EntityStatus_round_trips()
    {
        var status = new EntityStatus("dissolved");
        var json = JsonSerializer.Serialize(status, Options);
        var back = JsonSerializer.Deserialize<EntityStatus>(json, Options);
        Assert.Equal(status, back);
    }
}
