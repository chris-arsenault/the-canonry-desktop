using Avalonia.Controls;
using TheCanonry.Desktop.Shared;

namespace TheCanonry.Desktop.Enumerist;

internal sealed partial class SchemaExplorerView : UserControl
{
    public SchemaExplorerView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
            DebugLog.Static.Write("SchemaExplorer", $"DataContext = {DataContext?.GetType().Name ?? "null"}");
    }
}
