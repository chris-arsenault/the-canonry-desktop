using System.Collections.ObjectModel;
using System.Windows.Input;
using TheCanonry.Desktop.Shared;
using TheCanonry.Persistence;
using TheCanonry.Schema.NarrativeStyles;
using Microsoft.EntityFrameworkCore;

namespace TheCanonry.Desktop.Illuminator;

// =============================================================================
// Wizard Step Enumeration
// =============================================================================

internal enum WizardStep
{
    Style,
    EntryPoint,
    RoleAssignment,
    Direction,
    Review,
}

// =============================================================================
// List Item Types
// =============================================================================

internal sealed class StyleListItem : ViewModelBase
{
    private bool _isSelected;

    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Format { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

internal sealed class WizardEntityItem : ViewModelBase
{
    private bool _isSelected;
    private string? _assignedRole;

    public required string EntityId { get; init; }
    public required string Name { get; init; }
    public required string Kind { get; init; }
    public required string Subtype { get; init; }
    public required double Prominence { get; init; }
    public required string Culture { get; init; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string? AssignedRole
    {
        get => _assignedRole;
        set => SetProperty(ref _assignedRole, value);
    }
}

internal sealed class RoleSlotItem : ViewModelBase
{
    private WizardEntityItem? _assignedEntity;

    public required string Role { get; init; }
    public required string Description { get; init; }
    public required int MinCount { get; init; }
    public required int MaxCount { get; init; }

    public WizardEntityItem? AssignedEntity
    {
        get => _assignedEntity;
        set => SetProperty(ref _assignedEntity, value);
    }
}

// =============================================================================
// Wizard ViewModel
// =============================================================================

internal sealed class ChronicleWizardViewModel : ViewModelBase
{
    private readonly IDbContextFactory<CanonryDbContext> _dbFactory;
    private readonly NarrativeStyleLibrary _styleLibrary;
    private WizardStep _currentStep = WizardStep.Style;
    private StyleListItem? _selectedStyle;
    private WizardEntityItem? _selectedEntryPoint;
    private string _narrativeDirection = "";
    private string _entitySearchText = "";
    private string _entityKindFilter = "";
    private bool _isLoading;

    public ChronicleWizardViewModel(IDbContextFactory<CanonryDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        _styleLibrary = NarrativeStyleLibrary.CreateDefault();

        Styles = new ObservableCollection<StyleListItem>();
        Entities = new ObservableCollection<WizardEntityItem>();
        FilteredEntities = new ObservableCollection<WizardEntityItem>();
        RoleSlots = new ObservableCollection<RoleSlotItem>();
        CastAssignments = new ObservableCollection<WizardEntityItem>();

        NextCommand = new RelayCommand(GoNext, () => CanGoNext);
        BackCommand = new RelayCommand(GoBack, () => CurrentStep != WizardStep.Style);
        LoadEntitiesCommand = new AsyncRelayCommand(LoadEntitiesAsync);

        LoadStyles();
    }

    // =========================================================================
    // Properties
    // =========================================================================

    public WizardStep CurrentStep
    {
        get => _currentStep;
        private set
        {
            if (SetProperty(ref _currentStep, value))
            {
                OnPropertyChanged(nameof(StepTitle));
                OnPropertyChanged(nameof(StepIndex));
                OnPropertyChanged(nameof(IsStyleStep));
                OnPropertyChanged(nameof(IsEntryPointStep));
                OnPropertyChanged(nameof(IsRoleAssignmentStep));
                OnPropertyChanged(nameof(IsDirectionStep));
                OnPropertyChanged(nameof(IsReviewStep));
                ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
                ((RelayCommand)BackCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string StepTitle => CurrentStep switch
    {
        WizardStep.Style => "1. Select Narrative Style",
        WizardStep.EntryPoint => "2. Choose Entry Point Entity",
        WizardStep.RoleAssignment => "3. Assign Roles",
        WizardStep.Direction => "4. Narrative Direction",
        WizardStep.Review => "5. Review & Create",
        _ => "",
    };

    public int StepIndex => (int)CurrentStep + 1;

    public bool IsStyleStep => CurrentStep == WizardStep.Style;
    public bool IsEntryPointStep => CurrentStep == WizardStep.EntryPoint;
    public bool IsRoleAssignmentStep => CurrentStep == WizardStep.RoleAssignment;
    public bool IsDirectionStep => CurrentStep == WizardStep.Direction;
    public bool IsReviewStep => CurrentStep == WizardStep.Review;

    public ObservableCollection<StyleListItem> Styles { get; }
    public ObservableCollection<WizardEntityItem> Entities { get; }
    public ObservableCollection<WizardEntityItem> FilteredEntities { get; }
    public ObservableCollection<RoleSlotItem> RoleSlots { get; }
    public ObservableCollection<WizardEntityItem> CastAssignments { get; }

    public StyleListItem? SelectedStyle
    {
        get => _selectedStyle;
        set
        {
            if (SetProperty(ref _selectedStyle, value))
            {
                UpdateRoleSlots();
                ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public WizardEntityItem? SelectedEntryPoint
    {
        get => _selectedEntryPoint;
        set
        {
            if (SetProperty(ref _selectedEntryPoint, value))
                ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
        }
    }

    public string NarrativeDirection
    {
        get => _narrativeDirection;
        set => SetProperty(ref _narrativeDirection, value);
    }

    public string EntitySearchText
    {
        get => _entitySearchText;
        set
        {
            if (SetProperty(ref _entitySearchText, value))
                ApplyEntityFilter();
        }
    }

    public string EntityKindFilter
    {
        get => _entityKindFilter;
        set
        {
            if (SetProperty(ref _entityKindFilter, value))
                ApplyEntityFilter();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    // =========================================================================
    // Commands
    // =========================================================================

    public ICommand NextCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand LoadEntitiesCommand { get; }

    private bool CanGoNext => CurrentStep switch
    {
        WizardStep.Style => SelectedStyle is not null,
        WizardStep.EntryPoint => SelectedEntryPoint is not null,
        WizardStep.RoleAssignment => true,
        WizardStep.Direction => true,
        WizardStep.Review => false, // Final step — no next
        _ => false,
    };

    private void GoNext()
    {
        if (CurrentStep == WizardStep.Style && Entities.Count == 0)
            _ = LoadEntitiesAsync();

        CurrentStep = CurrentStep switch
        {
            WizardStep.Style => WizardStep.EntryPoint,
            WizardStep.EntryPoint => WizardStep.RoleAssignment,
            WizardStep.RoleAssignment => WizardStep.Direction,
            WizardStep.Direction => WizardStep.Review,
            _ => CurrentStep,
        };
    }

    private void GoBack()
    {
        CurrentStep = CurrentStep switch
        {
            WizardStep.EntryPoint => WizardStep.Style,
            WizardStep.RoleAssignment => WizardStep.EntryPoint,
            WizardStep.Direction => WizardStep.RoleAssignment,
            WizardStep.Review => WizardStep.Direction,
            _ => CurrentStep,
        };
    }

    // =========================================================================
    // Data Loading
    // =========================================================================

    private void LoadStyles()
    {
        foreach (var style in _styleLibrary.All)
        {
            Styles.Add(new StyleListItem
            {
                Id = style.Id,
                Name = style.Name,
                Description = style.Description,
                Format = style.Format.ToString(),
                Tags = style.Tags.ToList(),
            });
        }
    }

    private async Task LoadEntitiesAsync()
    {
        IsLoading = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var entities = await db.Entities
                .OrderBy(e => e.Name)
                .Take(500)
                .ToListAsync();

            Entities.Clear();
            foreach (var e in entities)
            {
                Entities.Add(new WizardEntityItem
                {
                    EntityId = e.Id,
                    Name = e.Name,
                    Kind = e.Kind,
                    Subtype = e.Subtype,
                    Prominence = e.Prominence,
                    Culture = e.Culture,
                });
            }

            ApplyEntityFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyEntityFilter()
    {
        FilteredEntities.Clear();
        foreach (var entity in Entities)
        {
            var matchesSearch = string.IsNullOrWhiteSpace(EntitySearchText)
                || entity.Name.Contains(EntitySearchText, StringComparison.OrdinalIgnoreCase);
            var matchesKind = string.IsNullOrWhiteSpace(EntityKindFilter)
                || string.Equals(entity.Kind, EntityKindFilter, StringComparison.OrdinalIgnoreCase);

            if (matchesSearch && matchesKind)
                FilteredEntities.Add(entity);
        }
    }

    private void UpdateRoleSlots()
    {
        RoleSlots.Clear();
        if (SelectedStyle is null) return;

        var style = _styleLibrary.FindById(SelectedStyle.Id);
        if (style is null) return;

        foreach (var role in style.Roles)
        {
            RoleSlots.Add(new RoleSlotItem
            {
                Role = role.Role,
                Description = role.Description,
                MinCount = role.Count.Min,
                MaxCount = role.Count.Max,
            });
        }
    }
}
