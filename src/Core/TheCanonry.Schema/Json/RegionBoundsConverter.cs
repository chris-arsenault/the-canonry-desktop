namespace TheCanonry.Schema.Json;

using System.Text.Json;
using System.Text.Json.Serialization;
using TheCanonry.Schema.Config;

/// <summary>
/// Deserializes region bounds from the domain JSON format where circles use
/// <c>{"shape": "circle", "center": {"x": N, "y": N}, "radius": N}</c>.
/// </summary>
public class RegionBoundsConverter : JsonConverter<RegionBounds>
{
    public override RegionBounds? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var shape = root.GetProperty("shape").GetString();

        return shape switch
        {
            "circle" => ReadCircle(root),
            "rect" => ReadRect(root),
            "polygon" => ReadPolygon(root),
            _ => throw new JsonException($"Unknown region bounds shape: {shape}")
        };
    }

    public override void Write(Utf8JsonWriter writer, RegionBounds value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("shape", value.Shape);

        switch (value)
        {
            case CircleBounds c:
                writer.WriteStartObject("center");
                writer.WriteNumber("x", c.CenterX);
                writer.WriteNumber("y", c.CenterY);
                writer.WriteEndObject();
                writer.WriteNumber("radius", c.Radius);
                break;

            case RectBounds r:
                writer.WriteNumber("x1", r.X1);
                writer.WriteNumber("y1", r.Y1);
                writer.WriteNumber("x2", r.X2);
                writer.WriteNumber("y2", r.Y2);
                break;

            case PolygonBounds p:
                writer.WriteStartArray("points");
                foreach (var pt in p.Points)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("x", pt.X);
                    writer.WriteNumber("y", pt.Y);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                break;
        }

        writer.WriteEndObject();
    }

    private static CircleBounds ReadCircle(JsonElement root)
    {
        var center = root.GetProperty("center");
        var x = center.GetProperty("x").GetDouble();
        var y = center.GetProperty("y").GetDouble();
        var radius = root.GetProperty("radius").GetDouble();
        return new CircleBounds(x, y, radius);
    }

    private static RectBounds ReadRect(JsonElement root)
    {
        return new RectBounds(
            root.GetProperty("x1").GetDouble(),
            root.GetProperty("y1").GetDouble(),
            root.GetProperty("x2").GetDouble(),
            root.GetProperty("y2").GetDouble());
    }

    private static PolygonBounds ReadPolygon(JsonElement root)
    {
        var points = new List<PolygonPoint>();
        foreach (var pt in root.GetProperty("points").EnumerateArray())
        {
            points.Add(new PolygonPoint(pt.GetProperty("x").GetDouble(), pt.GetProperty("y").GetDouble()));
        }
        return new PolygonBounds(points);
    }
}
