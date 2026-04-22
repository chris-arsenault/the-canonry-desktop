using System.Windows.Input;
using TheCanonry.Desktop.Shared;

namespace TheCanonry.Desktop.Shell;

internal sealed class ShellViewModel : ViewModelBase
{
    private readonly DebugLog _log;
    private string _statusText = "Ready";
    private string _databaseStatus = "Disconnected";

    public ShellViewModel(NavigationService navigation, WindowManager windowManager, ProjectService projectService, DebugLog log)
    {
        Navigation = navigation;
        WindowManager = windowManager;
        ProjectService = projectService;
        _log = log;

        // Enumerist section commands
        NavigateToEnumeristEntityKindsCommand = new RelayCommand(() => NavigateToSection<Enumerist.EnumeristViewModel>("entityKinds", "Entity Kinds"));
        NavigateToEnumeristRelationshipsCommand = new RelayCommand(() => NavigateToSection<Enumerist.EnumeristViewModel>("relationships", "Relationships"));
        NavigateToEnumeristRelMatrixCommand = new RelayCommand(() => NavigateToSection<Enumerist.EnumeristViewModel>("relMatrix", "Rel. Matrix"));
        NavigateToEnumeristCulturesCommand = new RelayCommand(() => NavigateToSection<Enumerist.EnumeristViewModel>("cultures", "Cultures"));
        NavigateToEnumeristTagsCommand = new RelayCommand(() => NavigateToSection<Enumerist.EnumeristViewModel>("tags", "Tags"));

        // Name Forge section commands
        NavigateToNameForgeWorkshopCommand = new RelayCommand(() => NavigateToSection<NameForge.NameForgeViewModel>("workshop", "Workshop"));
        NavigateToNameForgeGenerateCommand = new RelayCommand(() => NavigateToSection<NameForge.NameForgeViewModel>("generate", "Generate"));
        NavigateToNameForgeCoverageCommand = new RelayCommand(() => NavigateToSection<NameForge.NameForgeViewModel>("coverage", "Coverage"));

        // Cosmographer section commands
        NavigateToCosmographerAxesCommand = new RelayCommand(() => NavigateToSection<Cosmographer.CosmographerViewModel>("axes", "Axis Registry"));
        NavigateToCosmographerPlanesCommand = new RelayCommand(() => NavigateToSection<Cosmographer.CosmographerViewModel>("planes", "Semantic Planes"));
        NavigateToCosmographerCulturesCommand = new RelayCommand(() => NavigateToSection<Cosmographer.CosmographerViewModel>("cultures", "Culture Biases"));
        NavigateToCosmographerEntitiesCommand = new RelayCommand(() => NavigateToSection<Cosmographer.CosmographerViewModel>("entities", "Entities"));
        NavigateToCosmographerRelationshipsCommand = new RelayCommand(() => NavigateToSection<Cosmographer.CosmographerViewModel>("relationships", "Relationships"));

        // Existing navigation commands
        NavigateToLoreWeaveCommand = new RelayCommand(() => Navigation.NavigateTo<LoreWeave.LoreWeaveViewModel>());
        NavigateToIlluminatorCommand = new RelayCommand(() => Navigation.NavigateTo<Illuminator.IlluminatorViewModel>());
        NavigateToEntityBrowserCommand = new RelayCommand(() => Navigation.NavigateTo<Illuminator.EntityBrowserViewModel>());
        NavigateToChronicleCommand = new RelayCommand(() => Navigation.NavigateTo<Illuminator.ChronicleViewModel>());
        NavigateToImageCurationCommand = new RelayCommand(() => Navigation.NavigateTo<Illuminator.ImageCurationViewModel>());
        NavigateToCatalogCommand = new RelayCommand(() => Navigation.NavigateTo<Illuminator.CatalogViewModel>());
        NavigateToArchivistCommand = new RelayCommand(() => Navigation.NavigateTo<Archivist.ArchivistViewModel>());
        NavigateToAwsSyncCommand = new RelayCommand(() => Navigation.NavigateTo<AwsSync.AwsSyncViewModel>());
        NavigateToChronicleWizardCommand = new RelayCommand(() => Navigation.NavigateTo<Illuminator.ChronicleWizardViewModel>());
        NavigateToEditionComparisonCommand = new RelayCommand(() => Navigation.NavigateTo<Illuminator.EditionComparisonViewModel>());
    }

    public NavigationService Navigation { get; }
    public WindowManager WindowManager { get; }
    public ProjectService ProjectService { get; }
    public DebugLog DebugLog => _log;

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string DatabaseStatus
    {
        get => _databaseStatus;
        set => SetProperty(ref _databaseStatus, value);
    }

    // Enumerist
    public ICommand NavigateToEnumeristEntityKindsCommand { get; }
    public ICommand NavigateToEnumeristRelationshipsCommand { get; }
    public ICommand NavigateToEnumeristRelMatrixCommand { get; }
    public ICommand NavigateToEnumeristCulturesCommand { get; }
    public ICommand NavigateToEnumeristTagsCommand { get; }

    // Name Forge
    public ICommand NavigateToNameForgeWorkshopCommand { get; }
    public ICommand NavigateToNameForgeGenerateCommand { get; }
    public ICommand NavigateToNameForgeCoverageCommand { get; }

    // Cosmographer
    public ICommand NavigateToCosmographerAxesCommand { get; }
    public ICommand NavigateToCosmographerPlanesCommand { get; }
    public ICommand NavigateToCosmographerCulturesCommand { get; }
    public ICommand NavigateToCosmographerEntitiesCommand { get; }
    public ICommand NavigateToCosmographerRelationshipsCommand { get; }

    // Existing
    public ICommand NavigateToLoreWeaveCommand { get; }
    public ICommand NavigateToIlluminatorCommand { get; }
    public ICommand NavigateToEntityBrowserCommand { get; }
    public ICommand NavigateToChronicleCommand { get; }
    public ICommand NavigateToImageCurationCommand { get; }
    public ICommand NavigateToCatalogCommand { get; }
    public ICommand NavigateToArchivistCommand { get; }
    public ICommand NavigateToAwsSyncCommand { get; }
    public ICommand NavigateToChronicleWizardCommand { get; }
    public ICommand NavigateToEditionComparisonCommand { get; }

    /// <summary>Navigate to the default view on startup.</summary>
    public void NavigateToDefault() => Navigation.NavigateTo<Illuminator.EntityBrowserViewModel>();

    public void NewProject(string zipPath, string projectName)
    {
        _log.Write("Shell", $"NewProject(\"{zipPath}\", \"{projectName}\")");
        ProjectService.CreateNew(zipPath, projectName);
        StatusText = ProjectService.StatusMessage;
    }

    public void OpenProject(string zipPath)
    {
        _log.Write("Shell", $"OpenProject(\"{zipPath}\")");
        ProjectService.Load(zipPath);
        StatusText = ProjectService.StatusMessage;
    }

    /// <summary>
    /// Navigate to a sectioned module and set its active section.
    /// Singleton VMs retain their section state.
    /// </summary>
    private void NavigateToSection<TViewModel>(string section, string sectionLabel) where TViewModel : ViewModelBase
    {
        _log.Write("Shell", $"NavigateToSection<{typeof(TViewModel).Name}>(\"{section}\")");
        try
        {
            Navigation.NavigateTo<TViewModel>();
            _log.Write("Shell", $"  After NavigateTo: CurrentView={Navigation.CurrentView?.GetType().Name ?? "null"}, Name={Navigation.CurrentViewName}");

            if (Navigation.CurrentView is ISectionedViewModel sectionedVm)
            {
                _log.Write("Shell", $"  Setting ActiveSection to \"{section}\"");
                sectionedVm.ActiveSection = section;
                // Update status bar to show module > section
                var baseName = Navigation.CurrentViewName.Split(" > ")[0];
                Navigation.CurrentViewName = $"{baseName} > {sectionLabel}";
            }
            else
            {
                _log.Write("Shell", $"  CurrentView is NOT ISectionedViewModel (type: {Navigation.CurrentView?.GetType().Name ?? "null"})");
            }
        }
        catch (InvalidOperationException ex)
        {
            _log.Write("Shell", $"  EXCEPTION: {ex}");
        }
    }
}
