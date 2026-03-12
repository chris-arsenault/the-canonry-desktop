namespace TheCanonry.Desktop.DomainEditor;

using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using TheCanonry.Desktop.Shared;
using TheCanonry.Schema.Config;

public class DomainTreeItem : ViewModelBase
{
    private bool _isExpanded;
    private bool _isSelected;

    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    public string? JsonContent { get; init; }
    public ObservableCollection<DomainTreeItem> Children { get; init; } = [];

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public class DomainEditorViewModel : ViewModelBase
{
    private DomainTreeItem? _selectedItem;
    private string _editorContent = "";
    private string _configPath = "";
    private bool _hasUnsavedChanges;
    private string _validationMessage = "";

    // Maps tree item Name to the JSON filename it belongs to
    private static readonly Dictionary<string, string> NameToFile = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Entity Kinds"]      = "entityKinds.json",
        ["Relationship Kinds"] = "relationshipKinds.json",
        ["Cultures"]          = "cultures.json",
        ["Eras"]              = "eras.json",
        ["Templates"]         = "generators.json",
        ["Systems"]           = "systems.json",
        ["Pressures"]         = "pressures.json",
    };

    public DomainEditorViewModel()
    {
        TreeItems = new ObservableCollection<DomainTreeItem>();

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => HasUnsavedChanges);
        ValidateCommand = new RelayCommand(Validate);

        // Initialize tree structure
        TreeItems.Add(new DomainTreeItem
        {
            Name = "Entity Kinds",
            Category = "schema",
            Children = [],
        });
        TreeItems.Add(new DomainTreeItem
        {
            Name = "Relationship Kinds",
            Category = "schema",
            Children = [],
        });
        TreeItems.Add(new DomainTreeItem
        {
            Name = "Cultures",
            Category = "schema",
            Children = [],
        });
        TreeItems.Add(new DomainTreeItem
        {
            Name = "Eras",
            Category = "eras",
            Children = [],
        });
        TreeItems.Add(new DomainTreeItem
        {
            Name = "Templates",
            Category = "generators",
            Children = [],
        });
        TreeItems.Add(new DomainTreeItem
        {
            Name = "Systems",
            Category = "systems",
            Children = [],
        });
        TreeItems.Add(new DomainTreeItem
        {
            Name = "Pressures",
            Category = "pressures",
            Children = [],
        });
    }

    public ObservableCollection<DomainTreeItem> TreeItems { get; }

    public DomainTreeItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value) && value?.JsonContent is not null)
                EditorContent = value.JsonContent;
        }
    }

    public string EditorContent
    {
        get => _editorContent;
        set
        {
            if (SetProperty(ref _editorContent, value))
                HasUnsavedChanges = true;
        }
    }

    public string ConfigPath
    {
        get => _configPath;
        set => SetProperty(ref _configPath, value);
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set
        {
            if (SetProperty(ref _hasUnsavedChanges, value))
                ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();
        }
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public ICommand LoadCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ValidateCommand { get; }

    private async Task LoadAsync()
    {
        if (string.IsNullOrWhiteSpace(ConfigPath))
        {
            ValidationMessage = "ConfigPath is empty — enter a domain directory path.";
            return;
        }

        if (!Directory.Exists(ConfigPath))
        {
            ValidationMessage = $"Directory not found: {ConfigPath}";
            return;
        }

        var filesLoaded = 0;

        foreach (var treeItem in TreeItems)
        {
            if (!NameToFile.TryGetValue(treeItem.Name, out var filename))
                continue;

            var filePath = Path.Combine(ConfigPath, filename);
            if (!File.Exists(filePath))
                continue;

            string json;
            try
            {
                json = await File.ReadAllTextAsync(filePath);
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Failed to read {filename}: {ex.Message}";
                return;
            }

            treeItem.Children.Clear();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    var index = 0;
                    foreach (var element in root.EnumerateArray())
                    {
                        // Prefer "id" or "name" as the child node label, fallback to index
                        var label = TryGetLabel(element) ?? $"[{index}]";
                        var elementJson = element.GetRawText();

                        treeItem.Children.Add(new DomainTreeItem
                        {
                            Name = label,
                            Category = treeItem.Category,
                            JsonContent = elementJson,
                        });
                        index++;
                    }
                }
                else
                {
                    // Single-object file — add as single child
                    treeItem.Children.Add(new DomainTreeItem
                    {
                        Name = TryGetLabel(root) ?? filename,
                        Category = treeItem.Category,
                        JsonContent = root.GetRawText(),
                    });
                }

                filesLoaded++;
            }
            catch (JsonException ex)
            {
                ValidationMessage = $"Failed to parse {filename}: {ex.Message}";
                return;
            }
        }

        HasUnsavedChanges = false;
        ValidationMessage = $"Loaded {filesLoaded} files from {ConfigPath}";
    }

    private async Task SaveAsync()
    {
        if (SelectedItem is null || string.IsNullOrEmpty(SelectedItem.Category))
        {
            ValidationMessage = "No item selected for save.";
            return;
        }

        // Find the parent tree item that owns this child
        var parent = TreeItems.FirstOrDefault(t =>
            t.Children.Contains(SelectedItem) ||
            (NameToFile.ContainsKey(t.Name) && t.Name == SelectedItem.Name));

        if (parent is null || !NameToFile.TryGetValue(parent.Name, out var filename))
        {
            // SelectedItem itself might be the parent-level node
            if (!NameToFile.TryGetValue(SelectedItem.Name, out filename))
            {
                ValidationMessage = "Cannot determine which file to save.";
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(ConfigPath))
        {
            ValidationMessage = "ConfigPath is empty — cannot save.";
            return;
        }

        var filePath = Path.Combine(ConfigPath, filename);

        try
        {
            await File.WriteAllTextAsync(filePath, EditorContent);
            HasUnsavedChanges = false;
            ValidationMessage = $"Saved {filename}";
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Save failed: {ex.Message}";
        }
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConfigPath))
        {
            ValidationMessage = "ConfigPath is empty — enter a domain directory path.";
            return;
        }

        if (!Directory.Exists(ConfigPath))
        {
            ValidationMessage = $"Directory not found: {ConfigPath}";
            return;
        }

        try
        {
            var schema = DomainSchemaLoader.LoadFromDirectory(ConfigPath);
            ValidationMessage =
                $"Valid — schema '{schema.Name}' v{schema.Version}: " +
                $"{schema.EntityKinds.Count} entity kinds, " +
                $"{schema.RelationshipKinds.Count} relationship kinds, " +
                $"{schema.Cultures.Count} cultures";
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Validation failed: {ex.Message}";
        }
    }

    private static string? TryGetLabel(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        if (element.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
            return id.GetString();

        if (element.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString();

        return null;
    }
}
