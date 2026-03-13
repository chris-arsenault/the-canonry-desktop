using System.Collections.ObjectModel;

namespace TheCanonry.Desktop.Shared;

internal sealed class NavigationService : ViewModelBase
{
    private readonly IServiceProvider _services;
    private ViewModelBase? _currentView;
    private string _currentViewName = "";
    private NavigationItem? _selectedItem;

    public NavigationService(IServiceProvider services)
    {
        _services = services;
        NavigationItems = new ObservableCollection<NavigationItem>();
    }

    public ViewModelBase? CurrentView
    {
        get => _currentView;
        private set => SetProperty(ref _currentView, value);
    }

    public string CurrentViewName
    {
        get => _currentViewName;
        private set => SetProperty(ref _currentViewName, value);
    }

    public NavigationItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value) && value is not null)
                NavigateToItem(value);
        }
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    public void RegisterView<TViewModel>(string name, string icon) where TViewModel : ViewModelBase
    {
        NavigationItems.Add(new NavigationItem(name, icon, typeof(TViewModel)));
    }

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        var item = NavigationItems.FirstOrDefault(i => i.ViewModelType == typeof(TViewModel));
        if (item is not null)
            SelectedItem = item;
    }

    private void NavigateToItem(NavigationItem item)
    {
        var vm = (ViewModelBase)_services.GetRequiredService(item.ViewModelType);
        CurrentView = vm;
        CurrentViewName = item.Name;
    }
}

internal static class ServiceProviderExtensions
{
    public static object GetRequiredService(this IServiceProvider provider, Type serviceType)
    {
        return provider.GetService(serviceType)
            ?? throw new InvalidOperationException($"Service of type {serviceType.FullName} is not registered.");
    }
}
