using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TheCanonry.Desktop.Shell;

internal sealed partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();
    }

    private void Exit_Click(object? sender, RoutedEventArgs e) => Close();
}
