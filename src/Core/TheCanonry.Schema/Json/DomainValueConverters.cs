using System.Text.Json;
using System.Text.Json.Serialization;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;

namespace TheCanonry.Schema.Json;

public class EntityKindConverter : JsonConverter<EntityKind>
{
    public override EntityKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(reader.GetString()!);
    public override void Write(Utf8JsonWriter writer, EntityKind value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}

public class RelationshipKindConverter : JsonConverter<RelationshipKind>
{
    public override RelationshipKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(reader.GetString()!);
    public override void Write(Utf8JsonWriter writer, RelationshipKind value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}

public class EntityStatusConverter : JsonConverter<EntityStatus>
{
    public override EntityStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(reader.GetString()!);
    public override void Write(Utf8JsonWriter writer, EntityStatus value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}

public class CultureIdConverter : JsonConverter<CultureId>
{
    public override CultureId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(reader.GetString()!);
    public override void Write(Utf8JsonWriter writer, CultureId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}

public class EraIdConverter : JsonConverter<EraId>
{
    public override EraId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(reader.GetString()!);
    public override void Write(Utf8JsonWriter writer, EraId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}

public class RegionIdConverter : JsonConverter<RegionId>
{
    public override RegionId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(reader.GetString()!);
    public override void Write(Utf8JsonWriter writer, RegionId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}
