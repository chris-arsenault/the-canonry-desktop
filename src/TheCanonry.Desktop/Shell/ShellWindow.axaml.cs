using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace TheCanonry.Desktop.Shell;

internal sealed partial class ShellWindow : Window
{
    private DebugLogWindow? _debugLogWindow;

    public ShellWindow()
    {
        InitializeComponent();
    }

    private static readonly FilePickerFileType ZipFileType = new("Canonry Project")
    {
        Patterns = ["*.zip"],
        MimeTypes = ["application/zip"],
    };

    private void Exit_Click(object? sender, RoutedEventArgs e) => Close();

    private async void NewProject_Click(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "New Canonry Project",
            DefaultExtension = "zip",
            FileTypeChoices = [ZipFileType],
            SuggestedFileName = "my-project",
        });

        if (file is not null && DataContext is ShellViewModel vm)
        {
            var zipPath = file.Path.LocalPath;
            var projectName = Path.GetFileNameWithoutExtension(zipPath);
            vm.NewProject(zipPath, projectName);
        }
    }

    private async void OpenProject_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Canonry Project",
            AllowMultiple = false,
            FileTypeFilter = [ZipFileType],
        });

        if (files.Count > 0 && DataContext is ShellViewModel vm)
        {
            var zipPath = files[0].Path.LocalPath;
            vm.OpenProject(zipPath);
        }
    }

    private void DebugLog_Click(object? sender, RoutedEventArgs e)
    {
        if (_debugLogWindow is { IsVisible: true })
        {
            _debugLogWindow.Activate();
            return;
        }

        var log = (DataContext as ShellViewModel)?.DebugLog;
        if (log is null) return;

        _debugLogWindow = new DebugLogWindow(log);
        _debugLogWindow.Closed += (_, _) => _debugLogWindow = null;
        _debugLogWindow.Show();
    }
}
