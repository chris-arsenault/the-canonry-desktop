namespace TheCanonry.Desktop.Shell;

using System.Windows.Input;
using TheCanonry.Desktop.Shared;

public class ShellViewModel : ViewModelBase
{
    private string _statusText = "Ready";
    private string _databaseStatus = "Disconnected";

    public ShellViewModel(NavigationService navigation, WindowManager windowManager)
    {
        Navigation = navigation;
        WindowManager = windowManager;

        NavigateToForgeCommand = new RelayCommand(() => Navigation.NavigateTo<Forge.ForgeViewModel>());
        NavigateToIlluminatorCommand = new RelayCommand(() => Navigation.NavigateTo<Illuminator.IlluminatorViewModel>());
        NavigateToEntityBrowserCommand = new RelayCommand(() => Navigation.NavigateTo<Illuminator.EntityBrowserViewModel>());
        NavigateToChronicleCommand = new RelayCommand(() => Navigation.NavigateTo<Illuminator.ChronicleViewModel>());
        NavigateToImageCurationCommand = new RelayCommand(() => Navigation.NavigateTo<Illuminator.ImageCurationViewModel>());
        NavigateToCatalogCommand = new RelayCommand(() => Navigation.NavigateTo<Illuminator.CatalogViewModel>());
        NavigateToArchivistCommand = new RelayCommand(() => Navigation.NavigateTo<Archivist.ArchivistViewModel>());
        NavigateToDomainEditorCommand = new RelayCommand(() => Navigation.NavigateTo<DomainEditor.DomainEditorViewModel>());
        NavigateToAwsSyncCommand = new RelayCommand(() => Navigation.NavigateTo<AwsSync.AwsSyncViewModel>());
    }

    public NavigationService Navigation { get; }
    public WindowManager WindowManager { get; }

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

    // Navigation commands for menu items
    public ICommand NavigateToForgeCommand { get; }
    public ICommand NavigateToIlluminatorCommand { get; }
    public ICommand NavigateToEntityBrowserCommand { get; }
    public ICommand NavigateToChronicleCommand { get; }
    public ICommand NavigateToImageCurationCommand { get; }
    public ICommand NavigateToCatalogCommand { get; }
    public ICommand NavigateToArchivistCommand { get; }
    public ICommand NavigateToDomainEditorCommand { get; }
    public ICommand NavigateToAwsSyncCommand { get; }
}
