using System.Text.Json;
using TheCanonry.Schema.Config;
using TheCanonry.Schema.Json;

namespace TheCanonry.Desktop.Shared;

/// <summary>
/// Singleton service that holds the loaded domain configuration.
/// All modules (Enumerist, Name Forge, Cosmographer, etc.) read/write through this.
/// </summary>
internal sealed class ProjectService : ViewModelBase
{
    private string _configPath = "";
    private DomainSchema? _schema;
    private string _statusMessage = "No project loaded";
    private bool _isDirty;

    public string ConfigPath
    {
        get => _configPath;
        set => SetProperty(ref _configPath, value);
    }

    public DomainSchema? Schema
    {
        get => _schema;
        private set
        {
            if (SetProperty(ref _schema, value))
            {
                OnPropertyChanged(nameof(IsLoaded));
                OnPropertyChanged(nameof(EntityKinds));
                OnPropertyChanged(nameof(RelationshipKinds));
                OnPropertyChanged(nameof(Cultures));
                OnPropertyChanged(nameof(TagRegistry));
                OnPropertyChanged(nameof(AxisDefinitions));
                OnPropertyChanged(nameof(SeedEntities));
                OnPropertyChanged(nameof(SeedRelationships));
            }
        }
    }

    public bool IsLoaded => _schema is not null;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsDirty
    {
        get => _isDirty;
        private set => SetProperty(ref _isDirty, value);
    }

    // Convenience accessors
    public IReadOnlyList<EntityKindDefinition> EntityKinds => _schema?.EntityKinds ?? [];
    public IReadOnlyList<RelationshipKindDefinition> RelationshipKinds => _schema?.RelationshipKinds ?? [];
    public IReadOnlyList<CultureDefinition> Cultures => _schema?.Cultures ?? [];
    public IReadOnlyList<TagDefinition> TagRegistry => _schema?.TagRegistry ?? [];
    public IReadOnlyList<AxisDefinition> AxisDefinitions => _schema?.AxisDefinitions ?? [];
    public IReadOnlyList<SeedEntity> SeedEntities => _schema?.SeedEntities ?? [];
    public IReadOnlyList<SeedRelationship> SeedRelationships => _schema?.SeedRelationships ?? [];

    public void Load(string configPath)
    {
        if (!Directory.Exists(configPath))
        {
            StatusMessage = $"Directory not found: {configPath}";
            return;
        }

        try
        {
            ConfigPath = configPath;
            Schema = DomainSchemaLoader.LoadFromDirectory(configPath);
            IsDirty = false;
            StatusMessage = $"Loaded '{Schema.Name}' v{Schema.Version}: " +
                $"{Schema.EntityKinds.Count} entity kinds, " +
                $"{Schema.RelationshipKinds.Count} rel kinds, " +
                $"{Schema.Cultures.Count} cultures, " +
                $"{Schema.TagRegistry.Count} tags, " +
                $"{Schema.AxisDefinitions.Count} axes, " +
                $"{Schema.SeedEntities.Count} seed entities";
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            StatusMessage = $"Load failed: {ex.Message}";
        }
    }

    public void SaveFile<T>(string filename, T data)
    {
        if (string.IsNullOrWhiteSpace(ConfigPath)) return;
        var path = Path.Combine(ConfigPath, filename);
        var json = JsonSerializer.Serialize(data, DomainJsonOptions.Default);
        File.WriteAllText(path, json);
    }

    /// <summary>Save a specific collection back to its JSON file and reload.</summary>
    public void SaveEntityKinds(IReadOnlyList<EntityKindDefinition> entityKinds)
    {
        SaveFile("entityKinds.json", entityKinds);
        Reload();
    }

    public void SaveRelationshipKinds(IReadOnlyList<RelationshipKindDefinition> relationshipKinds)
    {
        SaveFile("relationshipKinds.json", relationshipKinds);
        Reload();
    }

    public void SaveCultures(IReadOnlyList<CultureDefinition> cultures)
    {
        SaveFile("cultures.json", cultures);
        Reload();
    }

    public void SaveTagRegistry(IReadOnlyList<TagDefinition> tags)
    {
        SaveFile("tagRegistry.json", tags);
        Reload();
    }

    public void SaveAxisDefinitions(IReadOnlyList<AxisDefinition> axes)
    {
        SaveFile("axisDefinitions.json", axes);
        Reload();
    }

    public void SaveSeedEntities(IReadOnlyList<SeedEntity> entities)
    {
        SaveFile("seedEntities.json", entities);
        Reload();
    }

    public void SaveSeedRelationships(IReadOnlyList<SeedRelationship> relationships)
    {
        SaveFile("seedRelationships.json", relationships);
        Reload();
    }

    public void MarkDirty() => IsDirty = true;

    private void Reload()
    {
        if (!string.IsNullOrWhiteSpace(ConfigPath))
            Load(ConfigPath);
    }
}
