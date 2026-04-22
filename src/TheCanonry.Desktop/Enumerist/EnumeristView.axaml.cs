using Avalonia.Controls;
using TheCanonry.Desktop.Shared;

namespace TheCanonry.Desktop.Enumerist;

internal sealed partial class EnumeristView : UserControl
{
    public EnumeristView()
    {
        DebugLog.Static.Write("EnumeristView", "Constructor called");
        try
        {
            InitializeComponent();
            DebugLog.Static.Write("EnumeristView", "InitializeComponent succeeded");
        }
        catch (System.Exception ex)
        {
            DebugLog.Static.Write("EnumeristView", $"InitializeComponent FAILED: {ex}");
            throw;
        }
    }
}
