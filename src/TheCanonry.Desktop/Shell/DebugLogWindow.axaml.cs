using Avalonia.Controls;
using Avalonia.Interactivity;
using TheCanonry.Desktop.Shared;

namespace TheCanonry.Desktop.Shell;

internal sealed partial class DebugLogWindow : Window
{
    private readonly DebugLog _log;

    public DebugLogWindow(DebugLog log)
    {
        _log = log;
        InitializeComponent();
        RefreshLog();
    }

    private void RefreshLog()
    {
        LogText.Text = _log.GetContents();
        LogText.CaretIndex = LogText.Text?.Length ?? 0;
    }

    private async void CopyToClipboard_Click(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null && LogText.Text is not null)
            await clipboard.SetTextAsync(LogText.Text);
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        RefreshLog();
    }
}
