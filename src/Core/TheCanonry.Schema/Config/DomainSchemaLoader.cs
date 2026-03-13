using System.Text.Json;
using TheCanonry.Schema.Json;

namespace TheCanonry.Schema.Config;

public static class DomainSchemaLoader
{
    public static DomainSchema LoadFromDirectory(string domainDir)
    {
        var manifestPath = Path.Combine(domainDir, "manifest.json");
        var manifest = File.Exists(manifestPath)
            ? JsonSerializer.Deserialize<DomainManifest>(File.ReadAllText(manifestPath), DomainJsonOptions.Default)
            : null;

        var entityKinds = LoadFile<List<EntityKindDefinition>>(domainDir, "entityKinds.json") ?? [];
        var relationshipKinds = LoadFile<List<RelationshipKindDefinition>>(domainDir, "relationshipKinds.json") ?? [];
        var cultures = LoadFile<List<CultureDefinition>>(domainDir, "cultures.json") ?? [];
        var tagRegistry = LoadFile<List<TagDefinition>>(domainDir, "tagRegistry.json") ?? [];
        var axisDefinitions = LoadFile<List<AxisDefinition>>(domainDir, "axisDefinitions.json") ?? [];
        var seedEntities = LoadFile<List<SeedEntity>>(domainDir, "seedEntities.json") ?? [];
        var seedRelationships = LoadFile<List<SeedRelationship>>(domainDir, "seedRelationships.json") ?? [];

        return new DomainSchema
        {
            Id = manifest?.Id ?? Path.GetFileName(domainDir),
            Name = manifest?.Name ?? Path.GetFileName(domainDir),
            Version = manifest?.Version ?? "0.0.0",
            EntityKinds = entityKinds,
            RelationshipKinds = relationshipKinds,
            Cultures = cultures,
            TagRegistry = tagRegistry,
            AxisDefinitions = axisDefinitions,
            SeedEntities = seedEntities,
            SeedRelationships = seedRelationships,
        };
    }

    private static T? LoadFile<T>(string dir, string filename)
    {
        var path = Path.Combine(dir, filename);
        if (!File.Exists(path)) return default;
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), DomainJsonOptions.Default);
    }
}

public class DomainManifest
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Version { get; init; } = "";
}
