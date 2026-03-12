namespace TheCanonry.Desktop.Shared;

using Avalonia.Controls;

public class WindowManager
{
    private readonly IServiceProvider _services;
    private readonly Dictionary<string, Window> _openWindows = [];

    public WindowManager(IServiceProvider services)
    {
        _services = services;
    }

    public void OpenInNewWindow<TViewModel>(string key) where TViewModel : ViewModelBase
    {
        if (_openWindows.TryGetValue(key, out var existing))
        {
            existing.Activate();
            return;
        }

        var vm = (ViewModelBase)_services.GetRequiredService(typeof(TViewModel));
        var window = new Window
        {
            Title = $"The Canonry - {key}",
            Width = 1000,
            Height = 700,
            Content = vm,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };

        window.Closed += (_, _) => _openWindows.Remove(key);
        _openWindows[key] = window;
        window.Show();
    }

    public void CloseAll()
    {
        foreach (var window in _openWindows.Values.ToArray())
            window.Close();
        _openWindows.Clear();
    }
}
