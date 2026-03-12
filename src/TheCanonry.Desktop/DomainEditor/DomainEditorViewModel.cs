namespace TheCanonry.Desktop.DomainEditor;

using System.Collections.ObjectModel;
using System.Windows.Input;
using TheCanonry.Desktop.Shared;

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

    private Task LoadAsync()
    {
        // Integration point: load JSON domain config files from ConfigPath
        ValidationMessage = "Load not yet connected to file system.";
        return Task.CompletedTask;
    }

    private Task SaveAsync()
    {
        // Integration point: save edited JSON back to disk
        HasUnsavedChanges = false;
        ValidationMessage = "Saved.";
        return Task.CompletedTask;
    }

    private void Validate()
    {
        // Integration point: run DomainSchemaLoader validation
        ValidationMessage = "Validation not yet connected.";
    }
}
