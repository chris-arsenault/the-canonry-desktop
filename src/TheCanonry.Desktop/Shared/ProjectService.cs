using System.IO.Compression;
using System.Text.Json;
using TheCanonry.Schema.Config;
using TheCanonry.Schema.Json;

namespace TheCanonry.Desktop.Shared;

/// <summary>
/// Singleton service that holds the loaded domain configuration.
/// Projects are stored as .zip files. On load, the zip is extracted to a
/// temporary working directory. Saves write back to the zip.
/// </summary>
internal sealed class ProjectService : ViewModelBase
{
    private string _projectFilePath = "";
    private string _workingDir = "";
    private DomainSchema? _schema;
    private string _statusMessage = "No project loaded";
    private bool _isDirty;

    /// <summary>Path to the .zip project file (empty for legacy directory projects).</summary>
    public string ProjectFilePath => _projectFilePath;

    /// <summary>Working directory with extracted files. Used by modules that need file paths.</summary>
    public string ConfigPath => _workingDir;

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

    /// <summary>
    /// Raised after any schema mutation (save, load, create).
    /// VMs subscribe once to refresh their local state.
    /// </summary>
    public event Action? SchemaChanged;

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

    /// <summary>Create a new empty project as a .zip file.</summary>
    public void CreateNew(string zipPath, string projectName)
    {
        DebugLog.Static.Write("ProjectService", $"CreateNew(\"{zipPath}\", \"{projectName}\")");
        CleanupWorkingDir();

        _workingDir = CreateTempDir();
        Directory.CreateDirectory(_workingDir);

        var manifest = new DomainManifest
        {
            Id = Path.GetFileNameWithoutExtension(zipPath),
            Name = projectName,
            Version = "1.0.0"
        };
        var manifestJson = JsonSerializer.Serialize(manifest, DomainJsonOptions.Default);
        File.WriteAllText(Path.Combine(_workingDir, "manifest.json"), manifestJson);

        _projectFilePath = zipPath;
        PackToZip();
        LoadFromWorkingDir();
        StatusMessage = $"Created '{projectName}'";
    }

    /// <summary>Open a .zip project file.</summary>
    public void Load(string path)
    {
        DebugLog.Static.Write("ProjectService", $"Load(\"{path}\")");

        if (!File.Exists(path))
        {
            StatusMessage = $"File not found: {path}";
            DebugLog.Static.Write("ProjectService", "  File not found");
            return;
        }

        try
        {
            CleanupWorkingDir();
            _projectFilePath = path;
            _workingDir = CreateTempDir();
            ZipFile.ExtractToDirectory(path, _workingDir);
            LoadFromWorkingDir();
        }
        catch (InvalidDataException ex)
        {
            StatusMessage = $"Invalid zip file: {ex.Message}";
            DebugLog.Static.Write("ProjectService", $"  Invalid zip: {ex.Message}");
        }
        catch (IOException ex)
        {
            StatusMessage = $"Load failed: {ex.Message}";
            DebugLog.Static.Write("ProjectService", $"  IO error: {ex.Message}");
        }
    }

    private void LoadFromWorkingDir()
    {
        Schema = DomainSchemaLoader.LoadFromDirectory(_workingDir);
        IsDirty = false;
        DebugLog.Static.Write("ProjectService", $"  Loaded OK. IsLoaded={IsLoaded}");
        StatusMessage = $"Loaded '{Schema.Name}' v{Schema.Version}: " +
            $"{Schema.EntityKinds.Count} entity kinds, " +
            $"{Schema.RelationshipKinds.Count} rel kinds, " +
            $"{Schema.Cultures.Count} cultures, " +
            $"{Schema.TagRegistry.Count} tags, " +
            $"{Schema.AxisDefinitions.Count} axes, " +
            $"{Schema.SeedEntities.Count} seed entities";
        SchemaChanged?.Invoke();
    }

    public void SaveFile<T>(string filename, T data)
    {
        if (string.IsNullOrWhiteSpace(_workingDir)) return;
        var path = Path.Combine(_workingDir, filename);

        // Ensure subdirectories exist (e.g. naming/culture.json)
        var dir = Path.GetDirectoryName(path);
        if (dir is not null) Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(data, DomainJsonOptions.Default);
        File.WriteAllText(path, json);

        if (!string.IsNullOrWhiteSpace(_projectFilePath))
            PackToZip();
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

    /// <summary>Clean up the temp working directory on shutdown.</summary>
    public void Cleanup() => CleanupWorkingDir();

    private void Reload()
    {
        if (!string.IsNullOrWhiteSpace(_workingDir) && Directory.Exists(_workingDir))
            LoadFromWorkingDir();
    }

    private void PackToZip()
    {
        if (string.IsNullOrWhiteSpace(_projectFilePath) || string.IsNullOrWhiteSpace(_workingDir))
            return;

        if (File.Exists(_projectFilePath))
            File.Delete(_projectFilePath);
        ZipFile.CreateFromDirectory(_workingDir, _projectFilePath);
        DebugLog.Static.Write("ProjectService", $"  Packed to \"{_projectFilePath}\"");
    }

    private void CleanupWorkingDir()
    {
        if (string.IsNullOrWhiteSpace(_workingDir)) return;
        // Only delete if it's a temp directory we created (not a user directory)
        if (!_workingDir.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase)) return;
        if (!Directory.Exists(_workingDir)) return;

        try
        {
            Directory.Delete(_workingDir, true);
            DebugLog.Static.Write("ProjectService", $"  Cleaned up temp dir \"{_workingDir}\"");
        }
        catch (IOException ex)
        {
            DebugLog.Static.Write("ProjectService", $"  Cleanup failed: {ex.Message}");
        }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "canonry", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
