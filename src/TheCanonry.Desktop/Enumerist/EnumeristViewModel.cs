using System.Collections.ObjectModel;
using System.Windows.Input;
using TheCanonry.Desktop.Shared;
using TheCanonry.Schema.Config;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;

namespace TheCanonry.Desktop.Enumerist;

// ────────────────────────────────────────────────────────────────
// Mutable wrapper classes for data-binding
// ────────────────────────────────────────────────────────────────

internal sealed class EditableSubtype : ViewModelBase
{
    private string _id = "";
    private string _name = "";
    private bool _isAuthority;

    public string Id { get => _id; set => SetProperty(ref _id, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public bool IsAuthority { get => _isAuthority; set => SetProperty(ref _isAuthority, value); }

    public SubtypeDefinition ToDefinition() => new()
    {
        Id = Id,
        Name = Name,
        IsAuthority = IsAuthority
    };

    public static EditableSubtype From(SubtypeDefinition d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        IsAuthority = d.IsAuthority
    };
}

internal sealed class EditableStatus : ViewModelBase
{
    private string _id = "";
    private string _name = "";
    private bool _isTerminal;
    private Polarity _polarity = Polarity.Neutral;
    private string _transitionVerb = "";

    public string Id { get => _id; set => SetProperty(ref _id, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public bool IsTerminal { get => _isTerminal; set => SetProperty(ref _isTerminal, value); }
    public Polarity Polarity { get => _polarity; set => SetProperty(ref _polarity, value); }
    public string TransitionVerb { get => _transitionVerb; set => SetProperty(ref _transitionVerb, value); }

    public StatusDefinition ToDefinition() => new()
    {
        Id = Id,
        Name = Name,
        IsTerminal = IsTerminal,
        Polarity = Polarity,
        TransitionVerb = TransitionVerb
    };

    public static EditableStatus From(StatusDefinition d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        IsTerminal = d.IsTerminal,
        Polarity = d.Polarity,
        TransitionVerb = d.TransitionVerb
    };
}

internal sealed class EditableEntityKind : ViewModelBase
{
    private string _kindValue = "";
    private string _description = "";
    private bool _isFramework;
    private EntityCategory _category = EntityCategory.Character;
    private string _color = "";
    private string _shape = "";
    private string _displayName = "";
    private string _defaultStatusId = "";

    public string KindValue
    {
        get => _kindValue;
        set { if (SetProperty(ref _kindValue, value)) OnPropertyChanged(nameof(Label)); }
    }
    public string Description { get => _description; set => SetProperty(ref _description, value); }
    public bool IsFramework { get => _isFramework; set => SetProperty(ref _isFramework, value); }
    public EntityCategory Category { get => _category; set => SetProperty(ref _category, value); }
    public string Color { get => _color; set => SetProperty(ref _color, value); }
    public string Shape { get => _shape; set => SetProperty(ref _shape, value); }
    public string DisplayName
    {
        get => _displayName;
        set { if (SetProperty(ref _displayName, value)) OnPropertyChanged(nameof(Label)); }
    }
    public string DefaultStatusId { get => _defaultStatusId; set => SetProperty(ref _defaultStatusId, value); }
    public ObservableCollection<EditableSubtype> Subtypes { get; } = [];
    public ObservableCollection<EditableStatus> Statuses { get; } = [];

    /// <summary>Label for the list — shows DisplayName if set, otherwise the kind ID.</summary>
    public string Label => string.IsNullOrWhiteSpace(DisplayName) ? KindValue : DisplayName;

    /// <summary>Children for the Schema Explorer tree view.</summary>
    public IEnumerable<object> TreeChildren
    {
        get
        {
            foreach (var s in Subtypes) yield return s;
            foreach (var s in Statuses) yield return s;
        }
    }

    public EntityKindDefinition ToDefinition() => new()
    {
        Kind = new EntityKind(KindValue),
        Description = Description,
        IsFramework = IsFramework,
        Category = Category,
        Subtypes = Subtypes.Select(s => s.ToDefinition()).ToList(),
        Statuses = Statuses.Select(s => s.ToDefinition()).ToList(),
        DefaultStatus = string.IsNullOrWhiteSpace(DefaultStatusId)
            ? default
            : new EntityStatus(DefaultStatusId),
        Style = new EntityKindStyle
        {
            Color = Color,
            Shape = Shape,
            DisplayName = DisplayName
        }
    };

    public static EditableEntityKind From(EntityKindDefinition d)
    {
        var e = new EditableEntityKind
        {
            KindValue = d.Kind.Value,
            Description = d.Description,
            IsFramework = d.IsFramework,
            Category = d.Category,
            Color = d.Style?.Color ?? "",
            Shape = d.Style?.Shape ?? "",
            DisplayName = d.Style?.DisplayName ?? "",
            DefaultStatusId = d.DefaultStatus.Value ?? ""
        };
        foreach (var s in d.Subtypes) e.Subtypes.Add(EditableSubtype.From(s));
        foreach (var s in d.Statuses) e.Statuses.Add(EditableStatus.From(s));
        return e;
    }
}

internal sealed class EditableRelationshipKind : ViewModelBase
{
    private string _kindValue = "";
    private string _name = "";
    private string _description = "";
    private bool _isFramework;
    private bool _symmetric;
    private string _category = "";
    private bool _cullable;
    private string _decayRate = "none";
    private Polarity _polarity = Polarity.Neutral;
    private string _formed = "";
    private string _ended = "";
    private string _inverseFormed = "";
    private string _inverseEnded = "";

    public string KindValue { get => _kindValue; set => SetProperty(ref _kindValue, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Description { get => _description; set => SetProperty(ref _description, value); }
    public bool IsFramework { get => _isFramework; set => SetProperty(ref _isFramework, value); }
    public bool Symmetric { get => _symmetric; set => SetProperty(ref _symmetric, value); }
    public string Category { get => _category; set => SetProperty(ref _category, value); }
    public bool Cullable { get => _cullable; set => SetProperty(ref _cullable, value); }
    public string DecayRate { get => _decayRate; set => SetProperty(ref _decayRate, value); }
    public Polarity Polarity { get => _polarity; set => SetProperty(ref _polarity, value); }
    public string Formed { get => _formed; set => SetProperty(ref _formed, value); }
    public string Ended { get => _ended; set => SetProperty(ref _ended, value); }
    public string InverseFormed { get => _inverseFormed; set => SetProperty(ref _inverseFormed, value); }
    public string InverseEnded { get => _inverseEnded; set => SetProperty(ref _inverseEnded, value); }

    /// <summary>Comma-separated source entity kind IDs.</summary>
    public ObservableCollection<string> SrcKinds { get; } = [];
    public ObservableCollection<string> DstKinds { get; } = [];

    public string SrcKindsText
    {
        get => string.Join(", ", SrcKinds);
        set
        {
            SrcKinds.Clear();
            foreach (var k in ParseCsv(value)) SrcKinds.Add(k);
            OnPropertyChanged();
        }
    }

    public string DstKindsText
    {
        get => string.Join(", ", DstKinds);
        set
        {
            DstKinds.Clear();
            foreach (var k in ParseCsv(value)) DstKinds.Add(k);
            OnPropertyChanged();
        }
    }

    public string Label => string.IsNullOrWhiteSpace(Name) ? KindValue : Name;

    public RelationshipKindDefinition ToDefinition() => new()
    {
        Kind = new RelationshipKind(KindValue),
        Name = Name,
        Description = Description,
        IsFramework = IsFramework,
        SrcKinds = SrcKinds.ToList(),
        DstKinds = DstKinds.ToList(),
        Symmetric = Symmetric,
        Category = Category,
        Cullable = Cullable,
        DecayRate = DecayRate,
        Polarity = Polarity,
        Verbs = new RelationshipVerbs
        {
            Formed = Formed,
            Ended = Ended,
            InverseFormed = InverseFormed,
            InverseEnded = InverseEnded
        }
    };

    public static EditableRelationshipKind From(RelationshipKindDefinition d)
    {
        var e = new EditableRelationshipKind
        {
            KindValue = d.Kind.Value,
            Name = d.Name,
            Description = d.Description,
            IsFramework = d.IsFramework,
            Symmetric = d.Symmetric,
            Category = d.Category,
            Cullable = d.Cullable,
            DecayRate = d.DecayRate,
            Polarity = d.Polarity,
            Formed = d.Verbs?.Formed ?? "",
            Ended = d.Verbs?.Ended ?? "",
            InverseFormed = d.Verbs?.InverseFormed ?? "",
            InverseEnded = d.Verbs?.InverseEnded ?? ""
        };
        foreach (var k in d.SrcKinds) e.SrcKinds.Add(k);
        foreach (var k in d.DstKinds) e.DstKinds.Add(k);
        return e;
    }

    private static IEnumerable<string> ParseCsv(string text) =>
        (text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(s => s.Length > 0);
}

internal sealed class EditableCulture : ViewModelBase
{
    private string _idValue = "";
    private string _name = "";
    private string _description = "";
    private bool _isFramework;
    private string _homeland = "";
    private string _color = "";

    public string IdValue { get => _idValue; set => SetProperty(ref _idValue, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Description { get => _description; set => SetProperty(ref _description, value); }
    public bool IsFramework { get => _isFramework; set => SetProperty(ref _isFramework, value); }
    public string Homeland { get => _homeland; set => SetProperty(ref _homeland, value); }
    public string Color { get => _color; set => SetProperty(ref _color, value); }

    public string Label => string.IsNullOrWhiteSpace(Name) ? IdValue : Name;

    public CultureDefinition ToDefinition() => new()
    {
        Id = new CultureId(IdValue),
        Name = Name,
        Description = Description,
        IsFramework = IsFramework,
        Homeland = Homeland,
        Color = Color
    };

    public static EditableCulture From(CultureDefinition d) => new()
    {
        IdValue = d.Id.Value,
        Name = d.Name,
        Description = d.Description,
        IsFramework = d.IsFramework,
        Homeland = d.Homeland,
        Color = d.Color
    };
}

internal sealed class EditableTag : ViewModelBase
{
    private string _tag = "";
    private string _category = "trait";
    private string _rarity = "common";
    private string _description = "";
    private bool _isAxis;
    private bool _isFramework;
    private int _minUsage;
    private int _maxUsage = 100;

    public string Tag { get => _tag; set => SetProperty(ref _tag, value); }
    public string Category { get => _category; set => SetProperty(ref _category, value); }
    public string Rarity { get => _rarity; set => SetProperty(ref _rarity, value); }
    public string Description { get => _description; set => SetProperty(ref _description, value); }
    public bool IsAxis { get => _isAxis; set => SetProperty(ref _isAxis, value); }
    public bool IsFramework { get => _isFramework; set => SetProperty(ref _isFramework, value); }
    public int MinUsage
    {
        get => _minUsage;
        set { if (SetProperty(ref _minUsage, value)) OnPropertyChanged(nameof(MinUsageDecimal)); }
    }

    public int MaxUsage
    {
        get => _maxUsage;
        set { if (SetProperty(ref _maxUsage, value)) OnPropertyChanged(nameof(MaxUsageDecimal)); }
    }

    /// <summary>Bridge for Avalonia NumericUpDown which binds to decimal?.</summary>
    public decimal? MinUsageDecimal
    {
        get => MinUsage;
        set { if (value.HasValue) MinUsage = (int)value.Value; }
    }

    /// <summary>Bridge for Avalonia NumericUpDown which binds to decimal?.</summary>
    public decimal? MaxUsageDecimal
    {
        get => MaxUsage;
        set { if (value.HasValue) MaxUsage = (int)value.Value; }
    }

    public string EntityKindsText
    {
        get => string.Join(", ", EntityKinds);
        set
        {
            EntityKinds.Clear();
            foreach (var k in ParseCsv(value)) EntityKinds.Add(k);
            OnPropertyChanged();
        }
    }

    public string RelatedTagsText
    {
        get => string.Join(", ", RelatedTags);
        set
        {
            RelatedTags.Clear();
            foreach (var k in ParseCsv(value)) RelatedTags.Add(k);
            OnPropertyChanged();
        }
    }

    public string ConflictingTagsText
    {
        get => string.Join(", ", ConflictingTags);
        set
        {
            ConflictingTags.Clear();
            foreach (var k in ParseCsv(value)) ConflictingTags.Add(k);
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> EntityKinds { get; } = [];
    public ObservableCollection<string> RelatedTags { get; } = [];
    public ObservableCollection<string> ConflictingTags { get; } = [];

    public TagDefinition ToDefinition() => new()
    {
        Tag = Tag,
        Category = Category,
        Rarity = Rarity,
        Description = Description,
        IsAxis = IsAxis,
        IsFramework = IsFramework,
        MinUsage = MinUsage,
        MaxUsage = MaxUsage,
        EntityKinds = EntityKinds.ToList(),
        RelatedTags = RelatedTags.ToList(),
        ConflictingTags = ConflictingTags.ToList()
    };

    public static EditableTag From(TagDefinition d)
    {
        var e = new EditableTag
        {
            Tag = d.Tag,
            Category = d.Category,
            Rarity = d.Rarity,
            Description = d.Description,
            IsAxis = d.IsAxis,
            IsFramework = d.IsFramework,
            MinUsage = d.MinUsage,
            MaxUsage = d.MaxUsage
        };
        foreach (var k in d.EntityKinds) e.EntityKinds.Add(k);
        foreach (var k in d.RelatedTags) e.RelatedTags.Add(k);
        foreach (var k in d.ConflictingTags) e.ConflictingTags.Add(k);
        return e;
    }

    private static IEnumerable<string> ParseCsv(string text) =>
        (text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(s => s.Length > 0);
}

// ────────────────────────────────────────────────────────────────
// Relationship matrix cell
// ────────────────────────────────────────────────────────────────

internal sealed class MatrixCell
{
    public required string Label { get; init; }
}

internal sealed class MatrixRow
{
    public required string RelKind { get; init; }
    public required IReadOnlyList<MatrixCell> Cells { get; init; }
}

// ────────────────────────────────────────────────────────────────
// ViewModel
// ────────────────────────────────────────────────────────────────

internal sealed class EnumeristViewModel : ViewModelBase, ISectionedViewModel
{
    private readonly ProjectService _projectService;

    private string _activeSection = "entityKinds";
    private string _statusMessage = "";

    // Entity Kinds
    private EditableEntityKind? _selectedEntityKind;
    private EditableSubtype? _selectedSubtype;
    private EditableStatus? _selectedStatus;
    private object? _treeSelectedItem;

    // Relationships
    private EditableRelationshipKind? _selectedRelationshipKind;

    // Cultures
    private EditableCulture? _selectedCulture;

    // Tags
    private EditableTag? _selectedTag;

    // Open document tabs
    private EditableEntityKind? _activeDocument;

    public SelectionService SelectionService { get; }
    public ObservableCollection<EditableEntityKind> OpenDocuments { get; } = [];

    public EditableEntityKind? ActiveDocument
    {
        get => _activeDocument;
        set
        {
            if (SetProperty(ref _activeDocument, value) && value is not null)
                SelectionService.Select(value);
        }
    }

    public EnumeristViewModel(ProjectService projectService, SelectionService selectionService)
    {
        _projectService = projectService;
        SelectionService = selectionService;

        // Section navigation commands
        GoToEntityKindsCommand = new RelayCommand(() => ActiveSection = "entityKinds");
        GoToRelationshipsCommand = new RelayCommand(() => ActiveSection = "relationships");
        GoToRelMatrixCommand = new RelayCommand(() => ActiveSection = "relMatrix");
        GoToCulturesCommand = new RelayCommand(() => ActiveSection = "cultures");
        GoToTagsCommand = new RelayCommand(() => ActiveSection = "tags");

        // Entity Kind commands
        AddEntityKindCommand = new RelayCommand(AddEntityKind, () => _projectService.IsLoaded)
            .ObservesProperty(_projectService, nameof(ProjectService.IsLoaded));
        RemoveEntityKindCommand = new RelayCommand(RemoveEntityKind, () => SelectedEntityKind is { IsFramework: false });
        SaveEntityKindsCommand = new RelayCommand(SaveEntityKinds, () => _projectService.IsLoaded)
            .ObservesProperty(_projectService, nameof(ProjectService.IsLoaded));
        AddSubtypeCommand = new RelayCommand(AddSubtype, () => SelectedEntityKind is { IsFramework: false });
        RemoveSubtypeCommand = new RelayCommand(RemoveSubtype, () => SelectedSubtype is not null && SelectedEntityKind is { IsFramework: false });
        AddStatusCommand = new RelayCommand(AddStatus, () => SelectedEntityKind is { IsFramework: false });
        RemoveStatusCommand = new RelayCommand(RemoveStatus, () => SelectedStatus is not null && SelectedEntityKind is { IsFramework: false });

        // Relationship commands
        AddRelationshipKindCommand = new RelayCommand(AddRelationshipKind, () => _projectService.IsLoaded)
            .ObservesProperty(_projectService, nameof(ProjectService.IsLoaded));
        RemoveRelationshipKindCommand = new RelayCommand(RemoveRelationshipKind, () => SelectedRelationshipKind is { IsFramework: false });
        SaveRelationshipKindsCommand = new RelayCommand(SaveRelationshipKinds, () => _projectService.IsLoaded)
            .ObservesProperty(_projectService, nameof(ProjectService.IsLoaded));

        // Culture commands
        AddCultureCommand = new RelayCommand(AddCulture, () => _projectService.IsLoaded)
            .ObservesProperty(_projectService, nameof(ProjectService.IsLoaded));
        RemoveCultureCommand = new RelayCommand(RemoveCulture, () => SelectedCulture is { IsFramework: false });
        SaveCulturesCommand = new RelayCommand(SaveCultures, () => _projectService.IsLoaded)
            .ObservesProperty(_projectService, nameof(ProjectService.IsLoaded));

        // Tag commands
        AddTagCommand = new RelayCommand(AddTag, () => _projectService.IsLoaded)
            .ObservesProperty(_projectService, nameof(ProjectService.IsLoaded));
        RemoveTagCommand = new RelayCommand(RemoveTag, () => SelectedTag is { IsFramework: false });
        SaveTagsCommand = new RelayCommand(SaveTags, () => _projectService.IsLoaded)
            .ObservesProperty(_projectService, nameof(ProjectService.IsLoaded));

        // Refresh command
        RefreshCommand = new RelayCommand(Refresh, () => _projectService.IsLoaded)
            .ObservesProperty(_projectService, nameof(ProjectService.IsLoaded));

        // Reload data when schema changes (any save from any VM, project load/create)
        _projectService.SchemaChanged += Refresh;

        // Load initial data
        Refresh();
    }

    // ── Section switching ──────────────────────────────────────

    public string ActiveSection
    {
        get => _activeSection;
        set
        {
            if (SetProperty(ref _activeSection, value))
            {
                OnPropertyChanged(nameof(IsEntityKindsActive));
                OnPropertyChanged(nameof(IsRelationshipsActive));
                OnPropertyChanged(nameof(IsRelMatrixActive));
                OnPropertyChanged(nameof(IsCulturesActive));
                OnPropertyChanged(nameof(IsTagsActive));
                if (value == "relMatrix") RebuildMatrix();
            }
        }
    }

    public bool IsEntityKindsActive => ActiveSection == "entityKinds";
    public bool IsRelationshipsActive => ActiveSection == "relationships";
    public bool IsRelMatrixActive => ActiveSection == "relMatrix";
    public bool IsCulturesActive => ActiveSection == "cultures";
    public bool IsTagsActive => ActiveSection == "tags";

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    // ── Section navigation commands ────────────────────────────

    public ICommand GoToEntityKindsCommand { get; }
    public ICommand GoToRelationshipsCommand { get; }
    public ICommand GoToRelMatrixCommand { get; }
    public ICommand GoToCulturesCommand { get; }
    public ICommand GoToTagsCommand { get; }
    public ICommand RefreshCommand { get; }

    // ── Entity Kinds ───────────────────────────────────────────

    public ObservableCollection<EditableEntityKind> EntityKindItems { get; } = [];

    public EditableEntityKind? SelectedEntityKind
    {
        get => _selectedEntityKind;
        set
        {
            if (SetProperty(ref _selectedEntityKind, value))
            {
                RaiseEntityKindCommandStates();
                SelectedSubtype = null;
                SelectedStatus = null;
                if (value is not null)
                {
                    // Open as document tab if not already open
                    if (!OpenDocuments.Contains(value))
                        OpenDocuments.Add(value);
                    ActiveDocument = value;
                }
            }
        }
    }

    /// <summary>
    /// Bound to the Schema Explorer TreeView. Discriminates selection by type:
    /// entity kinds open document tabs, subtypes/statuses push to SelectionService.
    /// </summary>
    public object? TreeSelectedItem
    {
        get => _treeSelectedItem;
        set
        {
            if (!SetProperty(ref _treeSelectedItem, value)) return;
            if (value is EditableEntityKind ek)
                SelectedEntityKind = ek;
            else
                SelectionService.Select(value);
        }
    }

    public EditableSubtype? SelectedSubtype
    {
        get => _selectedSubtype;
        set
        {
            if (SetProperty(ref _selectedSubtype, value))
                ((RelayCommand)RemoveSubtypeCommand).RaiseCanExecuteChanged();
        }
    }

    public EditableStatus? SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value))
                ((RelayCommand)RemoveStatusCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand AddEntityKindCommand { get; }
    public ICommand RemoveEntityKindCommand { get; }
    public ICommand SaveEntityKindsCommand { get; }
    public ICommand AddSubtypeCommand { get; }
    public ICommand RemoveSubtypeCommand { get; }
    public ICommand AddStatusCommand { get; }
    public ICommand RemoveStatusCommand { get; }

    public static IReadOnlyList<EntityCategory> EntityCategories { get; } =
        Enum.GetValues<EntityCategory>();

    public static IReadOnlyList<Polarity> PolarityValues { get; } =
        Enum.GetValues<Polarity>();

    public static IReadOnlyList<string> DecayRates { get; } =
        ["none", "slow", "moderate", "fast"];

    public static IReadOnlyList<string> TagCategories { get; } =
        ["status", "trait", "affiliation", "behavior", "theme", "location", "system"];

    public static IReadOnlyList<string> TagRarities { get; } =
        ["common", "uncommon", "rare", "legendary"];

    // ── Relationships ──────────────────────────────────────────

    public ObservableCollection<EditableRelationshipKind> RelationshipKindItems { get; } = [];

    public EditableRelationshipKind? SelectedRelationshipKind
    {
        get => _selectedRelationshipKind;
        set
        {
            if (SetProperty(ref _selectedRelationshipKind, value))
                RaiseRelationshipCommandStates();
        }
    }

    public ICommand AddRelationshipKindCommand { get; }
    public ICommand RemoveRelationshipKindCommand { get; }
    public ICommand SaveRelationshipKindsCommand { get; }

    // ── Rel. Matrix ────────────────────────────────────────────

    public ObservableCollection<string> MatrixColumnHeaders { get; } = [];
    public ObservableCollection<MatrixRow> MatrixRows { get; } = [];

    // ── Cultures ───────────────────────────────────────────────

    public ObservableCollection<EditableCulture> CultureItems { get; } = [];

    public EditableCulture? SelectedCulture
    {
        get => _selectedCulture;
        set
        {
            if (SetProperty(ref _selectedCulture, value))
                RaiseCultureCommandStates();
        }
    }

    public ICommand AddCultureCommand { get; }
    public ICommand RemoveCultureCommand { get; }
    public ICommand SaveCulturesCommand { get; }

    // ── Tags ───────────────────────────────────────────────────

    public ObservableCollection<EditableTag> TagItems { get; } = [];

    public EditableTag? SelectedTag
    {
        get => _selectedTag;
        set
        {
            if (SetProperty(ref _selectedTag, value))
                RaiseTagCommandStates();
        }
    }

    public ICommand AddTagCommand { get; }
    public ICommand RemoveTagCommand { get; }
    public ICommand SaveTagsCommand { get; }

    // ── Refresh ────────────────────────────────────────────────

    public void Refresh()
    {
        RefreshEntityKinds();
        RefreshRelationshipKinds();
        RefreshCultures();
        RefreshTags();
        if (IsRelMatrixActive) RebuildMatrix();

        if (_projectService.IsLoaded)
            StatusMessage = $"Loaded: {EntityKindItems.Count} entity kinds, " +
                $"{RelationshipKindItems.Count} relationships, " +
                $"{CultureItems.Count} cultures, {TagItems.Count} tags";
        else
            StatusMessage = "No project loaded";
    }

    private void RefreshEntityKinds()
    {
        EntityKindItems.Clear();
        SelectedEntityKind = null;
        var count = 0;
        foreach (var ek in _projectService.EntityKinds)
        {
            EntityKindItems.Add(EditableEntityKind.From(ek));
            count++;
        }
        DebugLog.Static.Write("Enumerist", $"RefreshEntityKinds: loaded {count} entity kinds into collection");
    }

    private void RefreshRelationshipKinds()
    {
        RelationshipKindItems.Clear();
        SelectedRelationshipKind = null;
        foreach (var rk in _projectService.RelationshipKinds)
            RelationshipKindItems.Add(EditableRelationshipKind.From(rk));
    }

    private void RefreshCultures()
    {
        CultureItems.Clear();
        SelectedCulture = null;
        foreach (var c in _projectService.Cultures)
            CultureItems.Add(EditableCulture.From(c));
    }

    private void RefreshTags()
    {
        TagItems.Clear();
        SelectedTag = null;
        foreach (var t in _projectService.TagRegistry)
            TagItems.Add(EditableTag.From(t));
    }

    // ── Entity Kind actions ────────────────────────────────────

    private void AddEntityKind()
    {
        var item = new EditableEntityKind { KindValue = "new_kind" };
        EntityKindItems.Add(item);
        SelectedEntityKind = item;
    }

    private void RemoveEntityKind()
    {
        if (SelectedEntityKind is null || SelectedEntityKind.IsFramework) return;
        EntityKindItems.Remove(SelectedEntityKind);
        SelectedEntityKind = EntityKindItems.FirstOrDefault();
    }

    private void SaveEntityKinds()
    {
        var definitions = EntityKindItems.Select(e => e.ToDefinition()).ToList();
        _projectService.SaveEntityKinds(definitions);
        StatusMessage = $"Saved {definitions.Count} entity kinds";
        Refresh();
    }

    private void AddSubtype()
    {
        if (SelectedEntityKind is null || SelectedEntityKind.IsFramework) return;
        var sub = new EditableSubtype { Id = "new_subtype", Name = "New Subtype" };
        SelectedEntityKind.Subtypes.Add(sub);
        SelectedSubtype = sub;
    }

    private void RemoveSubtype()
    {
        if (SelectedEntityKind is null || SelectedSubtype is null) return;
        SelectedEntityKind.Subtypes.Remove(SelectedSubtype);
        SelectedSubtype = SelectedEntityKind.Subtypes.FirstOrDefault();
    }

    private void AddStatus()
    {
        if (SelectedEntityKind is null || SelectedEntityKind.IsFramework) return;
        var status = new EditableStatus { Id = "new_status", Name = "New Status" };
        SelectedEntityKind.Statuses.Add(status);
        SelectedStatus = status;
    }

    private void RemoveStatus()
    {
        if (SelectedEntityKind is null || SelectedStatus is null) return;
        SelectedEntityKind.Statuses.Remove(SelectedStatus);
        SelectedStatus = SelectedEntityKind.Statuses.FirstOrDefault();
    }

    // ── Relationship actions ───────────────────────────────────

    private void AddRelationshipKind()
    {
        var item = new EditableRelationshipKind { KindValue = "new_rel" };
        RelationshipKindItems.Add(item);
        SelectedRelationshipKind = item;
    }

    private void RemoveRelationshipKind()
    {
        if (SelectedRelationshipKind is null || SelectedRelationshipKind.IsFramework) return;
        RelationshipKindItems.Remove(SelectedRelationshipKind);
        SelectedRelationshipKind = RelationshipKindItems.FirstOrDefault();
    }

    private void SaveRelationshipKinds()
    {
        var definitions = RelationshipKindItems.Select(r => r.ToDefinition()).ToList();
        _projectService.SaveRelationshipKinds(definitions);
        StatusMessage = $"Saved {definitions.Count} relationship kinds";
        Refresh();
    }

    // ── Culture actions ────────────────────────────────────────

    private void AddCulture()
    {
        var item = new EditableCulture { IdValue = "new_culture", Name = "New Culture" };
        CultureItems.Add(item);
        SelectedCulture = item;
    }

    private void RemoveCulture()
    {
        if (SelectedCulture is null || SelectedCulture.IsFramework) return;
        CultureItems.Remove(SelectedCulture);
        SelectedCulture = CultureItems.FirstOrDefault();
    }

    private void SaveCultures()
    {
        var definitions = CultureItems.Select(c => c.ToDefinition()).ToList();
        _projectService.SaveCultures(definitions);
        StatusMessage = $"Saved {definitions.Count} cultures";
        Refresh();
    }

    // ── Tag actions ────────────────────────────────────────────

    private void AddTag()
    {
        var item = new EditableTag { Tag = "new_tag" };
        TagItems.Add(item);
        SelectedTag = item;
    }

    private void RemoveTag()
    {
        if (SelectedTag is null || SelectedTag.IsFramework) return;
        TagItems.Remove(SelectedTag);
        SelectedTag = TagItems.FirstOrDefault();
    }

    private void SaveTags()
    {
        var definitions = TagItems.Select(t => t.ToDefinition()).ToList();
        _projectService.SaveTagRegistry(definitions);
        StatusMessage = $"Saved {definitions.Count} tags";
        Refresh();
    }

    // ── Matrix ─────────────────────────────────────────────────

    private void RebuildMatrix()
    {
        MatrixColumnHeaders.Clear();
        MatrixRows.Clear();

        var entityKindIds = _projectService.EntityKinds.Select(ek => ek.Kind.Value).ToList();
        foreach (var id in entityKindIds)
            MatrixColumnHeaders.Add(id);

        foreach (var rel in _projectService.RelationshipKinds)
        {
            var srcSet = rel.SrcKinds.Count > 0
                ? new HashSet<string>(rel.SrcKinds)
                : null; // null = any kind
            var dstSet = rel.DstKinds.Count > 0
                ? new HashSet<string>(rel.DstKinds)
                : null;

            var cells = entityKindIds.Select(ekId =>
            {
                var isSrc = srcSet is null || srcSet.Contains(ekId);
                var isDst = dstSet is null || dstSet.Contains(ekId);
                var label = (isSrc, isDst) switch
                {
                    (true, true) => "B",
                    (true, false) => "S",
                    (false, true) => "D",
                    _ => "-"
                };
                return new MatrixCell { Label = label };
            }).ToList();

            MatrixRows.Add(new MatrixRow
            {
                RelKind = string.IsNullOrWhiteSpace(rel.Name) ? rel.Kind.Value : rel.Name,
                Cells = cells
            });
        }
    }

    // ── Command state helpers ──────────────────────────────────

    private void RaiseEntityKindCommandStates()
    {
        ((RelayCommand)RemoveEntityKindCommand).RaiseCanExecuteChanged();
        ((RelayCommand)AddSubtypeCommand).RaiseCanExecuteChanged();
        ((RelayCommand)RemoveSubtypeCommand).RaiseCanExecuteChanged();
        ((RelayCommand)AddStatusCommand).RaiseCanExecuteChanged();
        ((RelayCommand)RemoveStatusCommand).RaiseCanExecuteChanged();
    }

    private void RaiseRelationshipCommandStates()
    {
        ((RelayCommand)RemoveRelationshipKindCommand).RaiseCanExecuteChanged();
    }

    private void RaiseCultureCommandStates()
    {
        ((RelayCommand)RemoveCultureCommand).RaiseCanExecuteChanged();
    }

    private void RaiseTagCommandStates()
    {
        ((RelayCommand)RemoveTagCommand).RaiseCanExecuteChanged();
    }
}
