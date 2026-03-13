using System.Windows.Input;
using TheCanonry.Desktop.Shared;

namespace TheCanonry.Desktop.Shell;

internal sealed class ShellViewModel : ViewModelBase
{
    private string _statusText = "Ready";
    private string _databaseStatus = "Disconnected";

    public ShellViewModel(NavigationService navigation, WindowManager windowManager, ProjectService projectService)
    {
        Navigation = navigation;
        WindowManager = windowManager;
        ProjectService = projectService;

        // Enumerist section commands
        NavigateToEnumeristEntityKindsCommand = new RelayCommand(() => NavigateToSection<Enumerist.EnumeristViewModel>("entityKinds"));
        NavigateToEnumeristRelationshipsCommand = new RelayCommand(() => NavigateToSection<Enumerist.EnumeristViewModel>("relationships"));
        NavigateToEnumeristRelMatrixCommand = new RelayCommand(() => NavigateToSection<Enumerist.EnumeristViewModel>("relMatrix"));
        NavigateToEnumeristCulturesCommand = new RelayCommand(() => NavigateToSection<Enumerist.EnumeristViewModel>("cultures"));
        NavigateToEnumeristTagsCommand = new RelayCommand(() => NavigateToSection<Enumerist.EnumeristViewModel>("tags"));

        // Name Forge section commands
        NavigateToNameForgeWorkshopCommand = new RelayCommand(() => NavigateToSection<NameForge.NameForgeViewModel>("workshop"));
        NavigateToNameForgeGenerateCommand = new RelayCommand(() => NavigateToSection<NameForge.NameForgeViewModel>("generate"));
        NavigateToNameForgeCoverageCommand = new RelayCommand(() => NavigateToSection<NameForge.NameForgeViewModel>("coverage"));

        // Cosmographer section commands
        NavigateToCosmographerAxesCommand = new RelayCommand(() => NavigateToSection<Cosmographer.CosmographerViewModel>("axes"));
        NavigateToCosmographerPlanesCommand = new RelayCommand(() => NavigateToSection<Cosmographer.CosmographerViewModel>("planes"));
        NavigateToCosmographerCulturesCommand = new RelayCommand(() => NavigateToSection<Cosmographer.CosmographerViewModel>("cultures"));
        NavigateToCosmographerEntitiesCommand = new RelayCommand(() => NavigateToSection<Cosmographer.CosmographerViewModel>("entities"));
        NavigateToCosmographerRelationshipsCommand = new RelayCommand(() => NavigateToSection<Cosmographer.CosmographerViewModel>("relationships"));

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

    /// <summary>
    /// Navigate to a sectioned module and set its active section.
    /// Singleton VMs retain their section state.
    /// </summary>
    private void NavigateToSection<TViewModel>(string section) where TViewModel : ViewModelBase
    {
        Navigation.NavigateTo<TViewModel>();
        if (Navigation.CurrentView is ISectionedViewModel sectioned)
            sectioned.ActiveSection = section;
    }
}
