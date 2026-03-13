using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using TheCanonry.Desktop.Shared;
using TheCanonry.Schema.Config;

namespace TheCanonry.Desktop.Cosmographer;

// ──────────────────────────────────────────────────────────
//  Mutable wrapper types for editing
// ──────────────────────────────────────────────────────────

internal sealed class EditableAxis : ViewModelBase
{
    private string _id = "";
    private string _name = "";
    private string _description = "";
    private string _lowTag = "";
    private string _highTag = "";

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
                Id = ToSnakeCase(value);
        }
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string LowTag
    {
        get => _lowTag;
        set => SetProperty(ref _lowTag, value);
    }

    public string HighTag
    {
        get => _highTag;
        set => SetProperty(ref _highTag, value);
    }

    public AxisDefinition ToDefinition() => new()
    {
        Id = Id,
        Name = Name,
        Description = Description,
        LowTag = LowTag,
        HighTag = HighTag,
    };

    public static EditableAxis FromDefinition(AxisDefinition def) => new()
    {
        _id = def.Id,
        _name = def.Name,
        _description = def.Description,
        _lowTag = def.LowTag,
        _highTag = def.HighTag,
    };

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var cleaned = Regex.Replace(input.Trim(), @"[^a-zA-Z0-9\s]", "");
        var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join("_", parts).ToLowerInvariant();
    }
}

internal sealed class SemanticPlaneDisplay : ViewModelBase
{
    public string EntityKindValue { get; init; } = "";
    public string AxisXId { get; init; } = "";
    public string AxisYId { get; init; } = "";
    public string AxisZId { get; init; } = "";
    public ObservableCollection<RegionDisplay> Regions { get; init; } = [];
}

internal sealed class RegionDisplay
{
    public string Id { get; init; } = "";
    public string Label { get; init; } = "";
    public string Color { get; init; } = "";
    public string Culture { get; init; } = "";
    public string Tags { get; init; } = "";
}

internal sealed class EditableCultureBias : ViewModelBase
{
    private double _x;
    private double _y;
    private double _z;

    public string CultureId { get; init; } = "";
    public string CultureName { get; init; } = "";
    public string EntityKind { get; init; } = "";

    public double X
    {
        get => _x;
        set => SetProperty(ref _x, value);
    }

    public double Y
    {
        get => _y;
        set => SetProperty(ref _y, value);
    }

    public double Z
    {
        get => _z;
        set => SetProperty(ref _z, value);
    }
}

internal sealed class CultureBiasGroup : ViewModelBase
{
    private bool _isExpanded;

    public string CultureId { get; init; } = "";
    public string CultureName { get; init; } = "";
    public string CultureColor { get; init; } = "";
    public ObservableCollection<EditableCultureBias> Biases { get; init; } = [];

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }
}

internal sealed class EditableSeedEntity : ViewModelBase
{
    private string _id = "";
    private string _kind = "";
    private string _subtype = "";
    private string _name = "";
    private string _summary = "";
    private string _description = "";
    private string _status = "active";
    private double _prominence = 2.5;
    private string _culture = "";
    private double _coordX = 50;
    private double _coordY = 50;
    private double _coordZ = 50;
    private string _tagsText = "";

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Kind
    {
        get => _kind;
        set => SetProperty(ref _kind, value);
    }

    public string Subtype
    {
        get => _subtype;
        set => SetProperty(ref _subtype, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Summary
    {
        get => _summary;
        set => SetProperty(ref _summary, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public double Prominence
    {
        get => _prominence;
        set => SetProperty(ref _prominence, value);
    }

    public string Culture
    {
        get => _culture;
        set => SetProperty(ref _culture, value);
    }

    public double CoordX
    {
        get => _coordX;
        set => SetProperty(ref _coordX, value);
    }

    public double CoordY
    {
        get => _coordY;
        set => SetProperty(ref _coordY, value);
    }

    public double CoordZ
    {
        get => _coordZ;
        set => SetProperty(ref _coordZ, value);
    }

    public string TagsText
    {
        get => _tagsText;
        set => SetProperty(ref _tagsText, value);
    }

    public SeedEntity ToDefinition()
    {
        var tags = new Dictionary<string, bool>();
        if (!string.IsNullOrWhiteSpace(TagsText))
        {
            foreach (var tag in TagsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                tags[tag] = true;
        }

        return new SeedEntity
        {
            Id = Id,
            Kind = Kind,
            Subtype = Subtype,
            Name = Name,
            Summary = Summary,
            Description = Description,
            Status = Status,
            Prominence = Prominence,
            Culture = Culture,
            Tags = tags,
            Coordinates = new SeedCoordinates { X = CoordX, Y = CoordY, Z = CoordZ },
            CreatedAt = 0,
            UpdatedAt = 0,
        };
    }

    public static EditableSeedEntity FromDefinition(SeedEntity def) => new()
    {
        _id = def.Id,
        _kind = def.Kind,
        _subtype = def.Subtype,
        _name = def.Name,
        _summary = def.Summary,
        _description = def.Description,
        _status = def.Status,
        _prominence = def.Prominence,
        _culture = def.Culture,
        _coordX = def.Coordinates.X,
        _coordY = def.Coordinates.Y,
        _coordZ = def.Coordinates.Z,
        _tagsText = string.Join(", ", def.Tags.Where(kv => kv.Value).Select(kv => kv.Key)),
    };
}

internal sealed class EditableSeedRelationship : ViewModelBase
{
    private string _kind = "";
    private string _src = "";
    private string _dst = "";
    private double _strength = 0.5;

    public string Kind
    {
        get => _kind;
        set => SetProperty(ref _kind, value);
    }

    public string Src
    {
        get => _src;
        set => SetProperty(ref _src, value);
    }

    public string Dst
    {
        get => _dst;
        set => SetProperty(ref _dst, value);
    }

    public double Strength
    {
        get => _strength;
        set => SetProperty(ref _strength, value);
    }

    public SeedRelationship ToDefinition() => new()
    {
        Kind = Kind,
        Src = Src,
        Dst = Dst,
        Strength = Strength,
    };

    public static EditableSeedRelationship FromDefinition(SeedRelationship def) => new()
    {
        _kind = def.Kind,
        _src = def.Src,
        _dst = def.Dst,
        _strength = def.Strength,
    };

    /// <summary>Display string for the list: "Source -> Destination"</summary>
    public string DisplayRoute => $"{Src} \u2192 {Dst}";
}

internal sealed class ProminenceOption
{
    public string Label { get; init; } = "";
    public double Value { get; init; }
}

// ──────────────────────────────────────────────────────────
//  Main ViewModel
// ──────────────────────────────────────────────────────────

internal sealed class CosmographerViewModel : ViewModelBase, ISectionedViewModel
{
    private readonly ProjectService _projectService;
    private string _activeSection = "axes";
    private string _statusMessage = "";

    // Axis Registry
    private EditableAxis? _selectedAxis;

    // Semantic Planes
    private SemanticPlaneDisplay? _selectedPlane;

    // Culture Biases (nothing extra needed — groups are in collection)

    // Entities
    private EditableSeedEntity? _selectedEntity;
    private string _entityKindFilter = "";

    // Relationships
    private EditableSeedRelationship? _selectedRelationship;
    private string _relationshipKindFilter = "";

    // New-relationship form fields
    private string _newRelKind = "";
    private string _newRelSrc = "";
    private string _newRelDst = "";
    private double _newRelStrength = 0.5;

    public CosmographerViewModel(ProjectService projectService)
    {
        _projectService = projectService;

        Axes = [];
        Planes = [];
        CultureBiasGroups = [];
        Entities = [];
        Relationships = [];

        AvailableEntityKindValues = [];
        EntityKindFilterOptions = [];
        AvailableSubtypes = [];
        AvailableStatuses = [];
        AvailableCultureNames = [];
        AvailableRelationshipKindValues = [];
        RelationshipKindFilterOptions = [];
        AvailableSourceEntities = [];
        AvailableDestinationEntities = [];

        ProminenceOptions =
        [
            new ProminenceOption { Label = "Forgotten (0.5)", Value = 0.5 },
            new ProminenceOption { Label = "Marginal (1.5)", Value = 1.5 },
            new ProminenceOption { Label = "Recognized (2.5)", Value = 2.5 },
            new ProminenceOption { Label = "Renowned (3.5)", Value = 3.5 },
            new ProminenceOption { Label = "Mythic (4.5)", Value = 4.5 },
        ];

        // Section navigation
        GoToAxesCommand = new RelayCommand(() => ActiveSection = "axes");
        GoToPlanesCommand = new RelayCommand(() => ActiveSection = "planes");
        GoToCulturesCommand = new RelayCommand(() => ActiveSection = "cultures");
        GoToEntitiesCommand = new RelayCommand(() => ActiveSection = "entities");
        GoToRelationshipsCommand = new RelayCommand(() => ActiveSection = "relationships");

        // Axis commands
        AddAxisCommand = new RelayCommand(AddAxis);
        RemoveAxisCommand = new RelayCommand(RemoveAxis, () => SelectedAxis is not null);
        SaveAxesCommand = new RelayCommand(SaveAxes);

        // Culture bias commands
        SaveCultureBiasesCommand = new RelayCommand(SaveCultureBiases);

        // Entity commands
        AddEntityCommand = new RelayCommand(AddEntity);
        RemoveEntityCommand = new RelayCommand(RemoveEntity, () => SelectedEntity is not null);
        SaveEntitiesCommand = new RelayCommand(SaveEntities);

        // Relationship commands
        AddRelationshipCommand = new RelayCommand(AddRelationship);
        RemoveRelationshipCommand = new RelayCommand(RemoveRelationship, () => SelectedRelationship is not null);
        SaveRelationshipsCommand = new RelayCommand(SaveRelationships);

        _projectService.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ProjectService.IsLoaded))
                Refresh();
        };

        if (_projectService.IsLoaded)
            Refresh();
    }

    // ── Section Navigation ───────────────────────────────

    public string ActiveSection
    {
        get => _activeSection;
        set
        {
            if (SetProperty(ref _activeSection, value))
            {
                OnPropertyChanged(nameof(IsAxesActive));
                OnPropertyChanged(nameof(IsPlanesActive));
                OnPropertyChanged(nameof(IsCulturesActive));
                OnPropertyChanged(nameof(IsEntitiesActive));
                OnPropertyChanged(nameof(IsRelationshipsActive));
            }
        }
    }

    public bool IsAxesActive => ActiveSection == "axes";
    public bool IsPlanesActive => ActiveSection == "planes";
    public bool IsCulturesActive => ActiveSection == "cultures";
    public bool IsEntitiesActive => ActiveSection == "entities";
    public bool IsRelationshipsActive => ActiveSection == "relationships";

    public ICommand GoToAxesCommand { get; }
    public ICommand GoToPlanesCommand { get; }
    public ICommand GoToCulturesCommand { get; }
    public ICommand GoToEntitiesCommand { get; }
    public ICommand GoToRelationshipsCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    // ── Axis Registry ────────────────────────────────────

    public ObservableCollection<EditableAxis> Axes { get; }

    public EditableAxis? SelectedAxis
    {
        get => _selectedAxis;
        set
        {
            if (SetProperty(ref _selectedAxis, value))
                ((RelayCommand)RemoveAxisCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand AddAxisCommand { get; }
    public ICommand RemoveAxisCommand { get; }
    public ICommand SaveAxesCommand { get; }

    private void AddAxis()
    {
        var axis = new EditableAxis { Name = "New Axis", LowTag = "low", HighTag = "high" };
        Axes.Add(axis);
        SelectedAxis = axis;
    }

    private void RemoveAxis()
    {
        if (SelectedAxis is null) return;
        Axes.Remove(SelectedAxis);
        SelectedAxis = Axes.Count > 0 ? Axes[0] : null;
    }

    private void SaveAxes()
    {
        var definitions = Axes.Select(a => a.ToDefinition()).ToList();
        _projectService.SaveAxisDefinitions(definitions);
        StatusMessage = $"Saved {definitions.Count} axis definitions.";
        RefreshPlanes();
    }

    // ── Semantic Planes (read-only) ──────────────────────

    public ObservableCollection<SemanticPlaneDisplay> Planes { get; }

    public SemanticPlaneDisplay? SelectedPlane
    {
        get => _selectedPlane;
        set => SetProperty(ref _selectedPlane, value);
    }

    // ── Culture Biases ───────────────────────────────────

    public ObservableCollection<CultureBiasGroup> CultureBiasGroups { get; }
    public ICommand SaveCultureBiasesCommand { get; }

    private void SaveCultureBiases()
    {
        var cultures = _projectService.Cultures;
        var updated = new List<CultureDefinition>(cultures.Count);

        foreach (var culture in cultures)
        {
            // Build updated AxisBiases dictionary from the editable biases
            var group = CultureBiasGroups.FirstOrDefault(g => g.CultureId == culture.Id.Value);
            var newBiases = new Dictionary<string, AxisBias>();

            if (group is not null)
            {
                foreach (var bias in group.Biases)
                {
                    newBiases[bias.EntityKind] = new AxisBias { X = bias.X, Y = bias.Y, Z = bias.Z };
                }
            }
            else
            {
                // Preserve existing biases for framework cultures not shown in UI
                foreach (var kv in culture.AxisBiases)
                    newBiases[kv.Key] = kv.Value;
            }

            // Reconstruct the CultureDefinition preserving all other fields
            updated.Add(new CultureDefinition
            {
                Id = culture.Id,
                Name = culture.Name,
                Description = culture.Description,
                IsFramework = culture.IsFramework,
                Homeland = culture.Homeland,
                Color = culture.Color,
                AxisBiases = newBiases,
                HomeRegions = new Dictionary<string, List<string>>(culture.HomeRegions),
                DefaultArtisticStyleId = culture.DefaultArtisticStyleId,
                DefaultCompositionStyles = new Dictionary<string, string>(culture.DefaultCompositionStyles),
                StyleKeywords = culture.StyleKeywords.ToList(),
                VisualIdentity = new Dictionary<string, string>(culture.VisualIdentity),
                Naming = culture.Naming,
            });
        }

        _projectService.SaveCultures(updated);
        StatusMessage = $"Saved culture biases for {CultureBiasGroups.Count} cultures.";
    }

    // ── Entities (Seed Entities) ─────────────────────────

    public ObservableCollection<EditableSeedEntity> Entities { get; }
    public ObservableCollection<string> AvailableEntityKindValues { get; }
    public ObservableCollection<string> EntityKindFilterOptions { get; }
    public ObservableCollection<string> AvailableSubtypes { get; }
    public ObservableCollection<string> AvailableStatuses { get; }
    public ObservableCollection<string> AvailableCultureNames { get; }
    public ObservableCollection<ProminenceOption> ProminenceOptions { get; }

    public EditableSeedEntity? SelectedEntity
    {
        get => _selectedEntity;
        set
        {
            var previous = _selectedEntity;
            if (SetProperty(ref _selectedEntity, value))
            {
                if (previous is not null)
                    previous.PropertyChanged -= OnSelectedEntityPropertyChanged;
                if (_selectedEntity is not null)
                    _selectedEntity.PropertyChanged += OnSelectedEntityPropertyChanged;

                ((RelayCommand)RemoveEntityCommand).RaiseCanExecuteChanged();
                RefreshSubtypesAndStatuses();
                OnPropertyChanged(nameof(SelectedProminenceOption));
            }
        }
    }

    private void OnSelectedEntityPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditableSeedEntity.Kind))
            RefreshSubtypesAndStatuses();
    }

    public ProminenceOption? SelectedProminenceOption
    {
        get => SelectedEntity is null
            ? null
            : ProminenceOptions.FirstOrDefault(p => Math.Abs(p.Value - SelectedEntity.Prominence) < 0.01);
        set
        {
            if (SelectedEntity is not null && value is not null)
            {
                SelectedEntity.Prominence = value.Value;
                OnPropertyChanged();
            }
        }
    }

    public string EntityKindFilter
    {
        get => _entityKindFilter;
        set
        {
            if (SetProperty(ref _entityKindFilter, value))
                RefreshEntityList();
        }
    }

    public ICommand AddEntityCommand { get; }
    public ICommand RemoveEntityCommand { get; }
    public ICommand SaveEntitiesCommand { get; }

    private void AddEntity()
    {
        var kindValue = AvailableEntityKindValues.Count > 0 ? AvailableEntityKindValues[0] : "unknown";
        var entity = new EditableSeedEntity
        {
            Id = $"seed_{Guid.NewGuid():N}"[..16],
            Kind = kindValue,
            Name = "New Entity",
        };
        Entities.Add(entity);
        SelectedEntity = entity;
    }

    private void RemoveEntity()
    {
        if (SelectedEntity is null) return;
        Entities.Remove(SelectedEntity);
        SelectedEntity = Entities.Count > 0 ? Entities[0] : null;
    }

    private void SaveEntities()
    {
        var definitions = Entities.Select(e => e.ToDefinition()).ToList();
        _projectService.SaveSeedEntities(definitions);
        StatusMessage = $"Saved {definitions.Count} seed entities.";
    }

    private void RefreshSubtypesAndStatuses()
    {
        AvailableSubtypes.Clear();
        AvailableStatuses.Clear();

        if (SelectedEntity is null) return;

        var kindDef = _projectService.EntityKinds
            .FirstOrDefault(ek => ek.Kind.Value == SelectedEntity.Kind);
        if (kindDef is null) return;

        AvailableSubtypes.Add("");
        foreach (var st in kindDef.Subtypes)
            AvailableSubtypes.Add(st.Id);

        foreach (var s in kindDef.Statuses)
            AvailableStatuses.Add(s.Id);
    }

    private void RefreshEntityList()
    {
        // Re-populate entities from source with optional filter
        Entities.Clear();
        var source = _projectService.SeedEntities;
        foreach (var def in source)
        {
            if (!string.IsNullOrEmpty(EntityKindFilter) && def.Kind != EntityKindFilter)
                continue;
            Entities.Add(EditableSeedEntity.FromDefinition(def));
        }

        SelectedEntity = Entities.Count > 0 ? Entities[0] : null;
    }

    // ── Relationships (Seed Relationships) ───────────────

    public ObservableCollection<EditableSeedRelationship> Relationships { get; }
    public ObservableCollection<string> AvailableRelationshipKindValues { get; }
    public ObservableCollection<string> RelationshipKindFilterOptions { get; }
    public ObservableCollection<string> AvailableSourceEntities { get; }
    public ObservableCollection<string> AvailableDestinationEntities { get; }

    public EditableSeedRelationship? SelectedRelationship
    {
        get => _selectedRelationship;
        set
        {
            if (SetProperty(ref _selectedRelationship, value))
                ((RelayCommand)RemoveRelationshipCommand).RaiseCanExecuteChanged();
        }
    }

    public string RelationshipKindFilter
    {
        get => _relationshipKindFilter;
        set
        {
            if (SetProperty(ref _relationshipKindFilter, value))
                RefreshRelationshipList();
        }
    }

    // New-relationship form
    public string NewRelKind
    {
        get => _newRelKind;
        set
        {
            if (SetProperty(ref _newRelKind, value))
                RefreshSourceAndDestOptions();
        }
    }

    public string NewRelSrc
    {
        get => _newRelSrc;
        set
        {
            if (SetProperty(ref _newRelSrc, value))
                RefreshDestOptions();
        }
    }

    public string NewRelDst
    {
        get => _newRelDst;
        set => SetProperty(ref _newRelDst, value);
    }

    public double NewRelStrength
    {
        get => _newRelStrength;
        set => SetProperty(ref _newRelStrength, value);
    }

    public ICommand AddRelationshipCommand { get; }
    public ICommand RemoveRelationshipCommand { get; }
    public ICommand SaveRelationshipsCommand { get; }

    private void AddRelationship()
    {
        if (string.IsNullOrWhiteSpace(NewRelKind) ||
            string.IsNullOrWhiteSpace(NewRelSrc) ||
            string.IsNullOrWhiteSpace(NewRelDst))
        {
            StatusMessage = "Select kind, source, and destination to add a relationship.";
            return;
        }

        if (NewRelSrc == NewRelDst)
        {
            StatusMessage = "Source and destination must be different entities.";
            return;
        }

        var rel = new EditableSeedRelationship
        {
            Kind = NewRelKind,
            Src = NewRelSrc,
            Dst = NewRelDst,
            Strength = NewRelStrength,
        };
        Relationships.Add(rel);
        SelectedRelationship = rel;
        StatusMessage = $"Added {NewRelKind}: {NewRelSrc} \u2192 {NewRelDst}";
    }

    private void RemoveRelationship()
    {
        if (SelectedRelationship is null) return;
        Relationships.Remove(SelectedRelationship);
        SelectedRelationship = Relationships.Count > 0 ? Relationships[0] : null;
    }

    private void SaveRelationships()
    {
        var definitions = Relationships.Select(r => r.ToDefinition()).ToList();
        _projectService.SaveSeedRelationships(definitions);
        StatusMessage = $"Saved {definitions.Count} seed relationships.";
    }

    private void RefreshRelationshipList()
    {
        Relationships.Clear();
        var source = _projectService.SeedRelationships;
        foreach (var def in source)
        {
            if (!string.IsNullOrEmpty(RelationshipKindFilter) && def.Kind != RelationshipKindFilter)
                continue;
            Relationships.Add(EditableSeedRelationship.FromDefinition(def));
        }

        SelectedRelationship = Relationships.Count > 0 ? Relationships[0] : null;
    }

    private void RefreshSourceAndDestOptions()
    {
        AvailableSourceEntities.Clear();
        AvailableDestinationEntities.Clear();

        if (string.IsNullOrWhiteSpace(NewRelKind)) return;

        var relKindDef = _projectService.RelationshipKinds
            .FirstOrDefault(rk => rk.Kind.Value == NewRelKind);

        var allEntities = _projectService.SeedEntities;

        // Filter source entities by SrcKinds constraint
        var srcEntities = relKindDef is not null && relKindDef.SrcKinds.Count > 0
            ? allEntities.Where(e => relKindDef.SrcKinds.Contains(e.Kind))
            : allEntities;

        foreach (var e in srcEntities)
            AvailableSourceEntities.Add(e.Id);

        // Populate destination with same logic for DstKinds
        RefreshDestOptions();
    }

    private void RefreshDestOptions()
    {
        AvailableDestinationEntities.Clear();

        if (string.IsNullOrWhiteSpace(NewRelKind)) return;

        var relKindDef = _projectService.RelationshipKinds
            .FirstOrDefault(rk => rk.Kind.Value == NewRelKind);

        var allEntities = _projectService.SeedEntities;

        var dstEntities = relKindDef is not null && relKindDef.DstKinds.Count > 0
            ? allEntities.Where(e => relKindDef.DstKinds.Contains(e.Kind))
            : allEntities;

        // Exclude selected source
        foreach (var e in dstEntities)
        {
            if (e.Id != NewRelSrc)
                AvailableDestinationEntities.Add(e.Id);
        }
    }

    // ── Refresh All ──────────────────────────────────────

    private void Refresh()
    {
        if (!_projectService.IsLoaded) return;

        RefreshAxes();
        RefreshPlanes();
        RefreshCultureBiases();
        RefreshEntityDropdowns();
        RefreshEntityList();
        RefreshRelationshipDropdowns();
        RefreshRelationshipList();

        StatusMessage = "Data loaded from project.";
    }

    private void RefreshAxes()
    {
        Axes.Clear();
        foreach (var def in _projectService.AxisDefinitions)
            Axes.Add(EditableAxis.FromDefinition(def));
        SelectedAxis = Axes.Count > 0 ? Axes[0] : null;
    }

    private void RefreshPlanes()
    {
        Planes.Clear();
        foreach (var ekDef in _projectService.EntityKinds)
        {
            if (ekDef.SemanticPlane is null) continue;

            var plane = ekDef.SemanticPlane;
            var display = new SemanticPlaneDisplay
            {
                EntityKindValue = ekDef.Kind.Value,
                AxisXId = plane.Axes.X.AxisId,
                AxisYId = plane.Axes.Y.AxisId,
                AxisZId = plane.Axes.Z.AxisId,
            };

            foreach (var region in plane.Regions)
            {
                display.Regions.Add(new RegionDisplay
                {
                    Id = region.Id,
                    Label = region.Label,
                    Color = region.Color,
                    Culture = region.Culture,
                    Tags = string.Join(", ", region.Tags),
                });
            }

            Planes.Add(display);
        }

        SelectedPlane = Planes.Count > 0 ? Planes[0] : null;
    }

    private void RefreshCultureBiases()
    {
        CultureBiasGroups.Clear();

        var entityKindValues = _projectService.EntityKinds
            .Where(ek => ek.SemanticPlane is not null)
            .Select(ek => ek.Kind.Value)
            .ToList();

        foreach (var culture in _projectService.Cultures)
        {
            if (culture.IsFramework) continue;

            var group = new CultureBiasGroup
            {
                CultureId = culture.Id.Value,
                CultureName = culture.Name,
                CultureColor = culture.Color,
            };

            foreach (var kindValue in entityKindValues)
            {
                culture.AxisBiases.TryGetValue(kindValue, out var existing);
                group.Biases.Add(new EditableCultureBias
                {
                    CultureId = culture.Id.Value,
                    CultureName = culture.Name,
                    EntityKind = kindValue,
                    X = existing?.X ?? 50,
                    Y = existing?.Y ?? 50,
                    Z = existing?.Z ?? 50,
                });
            }

            CultureBiasGroups.Add(group);
        }
    }

    private void RefreshEntityDropdowns()
    {
        AvailableEntityKindValues.Clear();
        EntityKindFilterOptions.Clear();
        EntityKindFilterOptions.Add(""); // "All" option
        foreach (var ek in _projectService.EntityKinds)
        {
            AvailableEntityKindValues.Add(ek.Kind.Value);
            EntityKindFilterOptions.Add(ek.Kind.Value);
        }

        AvailableCultureNames.Clear();
        AvailableCultureNames.Add("");
        foreach (var c in _projectService.Cultures)
            AvailableCultureNames.Add(c.Id.Value);
    }

    private void RefreshRelationshipDropdowns()
    {
        AvailableRelationshipKindValues.Clear();
        RelationshipKindFilterOptions.Clear();
        RelationshipKindFilterOptions.Add(""); // "All" option
        foreach (var rk in _projectService.RelationshipKinds)
        {
            AvailableRelationshipKindValues.Add(rk.Kind.Value);
            RelationshipKindFilterOptions.Add(rk.Kind.Value);
        }
    }
}
