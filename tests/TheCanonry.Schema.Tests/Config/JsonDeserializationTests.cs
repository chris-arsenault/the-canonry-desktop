using System.Text.Json;
using TheCanonry.Schema.Config;
using TheCanonry.Schema.Json;

namespace TheCanonry.Schema.Tests.Config;

public class JsonDeserializationTests
{
    private static readonly string DomainDir = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "domain", "default-project"));

    [SkippableFact]
    public void Can_deserialize_entityKinds_json()
    {
        var path = Path.Combine(DomainDir, "entityKinds.json");
        Skip.IfNot(File.Exists(path), "Domain files not yet copied");

        var json = File.ReadAllText(path);
        var kinds = JsonSerializer.Deserialize<List<EntityKindDefinition>>(json, DomainJsonOptions.Default);

        Assert.NotNull(kinds);
        Assert.NotEmpty(kinds);
    }

    [SkippableFact]
    public void Can_deserialize_relationshipKinds_json()
    {
        var path = Path.Combine(DomainDir, "relationshipKinds.json");
        Skip.IfNot(File.Exists(path), "Domain files not yet copied");

        var json = File.ReadAllText(path);
        var kinds = JsonSerializer.Deserialize<List<RelationshipKindDefinition>>(json, DomainJsonOptions.Default);

        Assert.NotNull(kinds);
        Assert.NotEmpty(kinds);
    }

    [SkippableFact]
    public void Can_deserialize_cultures_json()
    {
        var path = Path.Combine(DomainDir, "cultures.json");
        Skip.IfNot(File.Exists(path), "Domain files not yet copied");

        var json = File.ReadAllText(path);
        var cultures = JsonSerializer.Deserialize<List<CultureDefinition>>(json, DomainJsonOptions.Default);

        Assert.NotNull(cultures);
        Assert.NotEmpty(cultures);
    }

    [SkippableFact]
    public void Can_load_full_domain_schema()
    {
        Skip.IfNot(Directory.Exists(DomainDir), "Domain files not yet copied");

        var schema = DomainSchemaLoader.LoadFromDirectory(DomainDir);

        Assert.NotNull(schema);
        Assert.NotEmpty(schema.EntityKinds);
        Assert.NotEmpty(schema.RelationshipKinds);
        Assert.NotEmpty(schema.Cultures);
    }
}
