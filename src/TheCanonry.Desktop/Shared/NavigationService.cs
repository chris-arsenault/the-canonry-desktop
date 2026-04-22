using System.Collections.ObjectModel;

namespace TheCanonry.Desktop.Shared;

internal sealed class NavigationService : ViewModelBase
{
    private readonly IServiceProvider _services;
    private readonly DebugLog _log;
    private ViewModelBase? _currentView;
    private string _currentViewName = "";
    private NavigationItem? _selectedItem;

    public NavigationService(IServiceProvider services, DebugLog log)
    {
        _services = services;
        _log = log;
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
        set => SetProperty(ref _currentViewName, value);
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
        _log.Write("Nav", $"NavigateTo<{typeof(TViewModel).Name}>()");
        var item = NavigationItems.FirstOrDefault(i => i.ViewModelType == typeof(TViewModel));
        _log.Write("Nav", $"  Found item: {(item is null ? "NULL" : $"Name={item.Name}, Type={item.ViewModelType.Name}")}");
        if (item is not null)
        {
            _log.Write("Nav", $"  Current SelectedItem: {(_selectedItem is null ? "null" : $"Name={_selectedItem.Name}, Type={_selectedItem.ViewModelType.Name}")}");
            _log.Write("Nav", $"  Same reference? {ReferenceEquals(_selectedItem, item)}");
            SelectedItem = item;
            _log.Write("Nav", $"  After set: CurrentView={CurrentView?.GetType().Name ?? "null"}, CurrentViewName={CurrentViewName}");
        }
        else
        {
            _log.Write("Nav", $"  NO ITEM FOUND! Registered items:");
            foreach (var ni in NavigationItems)
                _log.Write("Nav", $"    - {ni.Name}: {ni.ViewModelType.FullName}");
        }
    }

    private void NavigateToItem(NavigationItem item)
    {
        _log.Write("Nav", $"NavigateToItem: {item.Name} ({item.ViewModelType.Name})");
        try
        {
            var vm = (ViewModelBase)_services.GetRequiredService(item.ViewModelType);
            _log.Write("Nav", $"  Resolved VM: {vm.GetType().Name} (hash={vm.GetHashCode()})");
            CurrentView = vm;
            CurrentViewName = item.Name;
        }
        catch (InvalidOperationException ex)
        {
            _log.Write("Nav", $"  EXCEPTION resolving VM: {ex}");
        }
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
