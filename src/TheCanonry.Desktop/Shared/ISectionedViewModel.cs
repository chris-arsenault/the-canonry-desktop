namespace TheCanonry.Desktop.Shared;

/// <summary>
/// Interface for ViewModels that have multiple sections switchable via menus.
/// </summary>
internal interface ISectionedViewModel
{
    string ActiveSection { get; set; }
}
