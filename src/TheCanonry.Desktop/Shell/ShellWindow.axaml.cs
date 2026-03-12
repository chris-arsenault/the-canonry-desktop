namespace TheCanonry.Desktop.Shell;

using Avalonia.Controls;
using Avalonia.Interactivity;

public partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();
    }

    private void Exit_Click(object? sender, RoutedEventArgs e) => Close();
}
