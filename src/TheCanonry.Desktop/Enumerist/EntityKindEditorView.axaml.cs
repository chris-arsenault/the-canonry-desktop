using Avalonia.Controls;
using TheCanonry.Desktop.Shared;

namespace TheCanonry.Desktop.Enumerist;

internal sealed partial class EntityKindEditorView : UserControl
{
    public EntityKindEditorView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
            DebugLog.Static.Write("EntityKindEditor", $"DataContext = {DataContext?.GetType().Name ?? "null"}");
    }
}
