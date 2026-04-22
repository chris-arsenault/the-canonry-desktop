namespace TheCanonry.Desktop.Shared;

/// <summary>
/// Shared selection context. Tool windows (Properties panel, etc.) subscribe
/// to SelectionChanged and update their content based on what is selected
/// anywhere in the application.
/// </summary>
internal sealed class SelectionService : ViewModelBase
{
    private object? _selectedObject;

    public object? SelectedObject
    {
        get => _selectedObject;
        private set => SetProperty(ref _selectedObject, value);
    }

    public void Select(object? item) => SelectedObject = item;
}
