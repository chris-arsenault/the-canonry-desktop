namespace TheCanonry.Desktop.Shell;

using System.ComponentModel;
using System.Runtime.CompilerServices;

public class ShellViewModel : INotifyPropertyChanged
{
    private string _statusText = "Ready";
    private string _databaseStatus = "Disconnected";

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public string DatabaseStatus
    {
        get => _databaseStatus;
        set { _databaseStatus = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
