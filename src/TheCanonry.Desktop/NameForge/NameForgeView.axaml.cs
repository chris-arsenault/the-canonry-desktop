using Avalonia.Controls;
using TheCanonry.Desktop.Shared;

namespace TheCanonry.Desktop.NameForge;

internal sealed partial class NameForgeView : UserControl
{
    public NameForgeView()
    {
        DebugLog.Static.Write("NameForgeView", "Constructor called");
        try
        {
            InitializeComponent();
            DebugLog.Static.Write("NameForgeView", "InitializeComponent succeeded");
        }
        catch (System.Exception ex)
        {
            DebugLog.Static.Write("NameForgeView", $"InitializeComponent FAILED: {ex}");
            throw;
        }
    }
}
