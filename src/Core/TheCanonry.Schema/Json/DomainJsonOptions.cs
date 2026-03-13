using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheCanonry.Schema.Json;

public static class DomainJsonOptions
{
    public static readonly JsonSerializerOptions Default = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        // Nominal value object converters
        options.Converters.Add(new EntityKindConverter());
        options.Converters.Add(new RelationshipKindConverter());
        options.Converters.Add(new EntityStatusConverter());
        options.Converters.Add(new CultureIdConverter());
        options.Converters.Add(new EraIdConverter());
        options.Converters.Add(new RegionIdConverter());

        // Polymorphic domain converters
        options.Converters.Add(new RegionBoundsConverter());

        // Enum converters — JSON uses lowercase strings
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }
}
