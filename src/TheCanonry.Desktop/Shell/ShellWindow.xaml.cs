namespace TheCanonry.Desktop.Shell;

using System.Windows;

public partial class ShellWindow : Window
{
    public ShellWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}
