using Avalonia.Controls;
using TheCanonry.Desktop.Shared;

namespace TheCanonry.Desktop.Enumerist;

internal sealed partial class PropertiesPanelView : UserControl
{
    public PropertiesPanelView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
            DebugLog.Static.Write("PropertiesPanel", $"DataContext = {DataContext?.GetType().Name ?? "null"}");
    }
}
