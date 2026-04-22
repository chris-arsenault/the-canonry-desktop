using Avalonia.Controls;
using TheCanonry.Desktop.Shared;

namespace TheCanonry.Desktop.Cosmographer;

internal sealed partial class CosmographerView : UserControl
{
    public CosmographerView()
    {
        DebugLog.Static.Write("CosmographerView", "Constructor called");
        try
        {
            InitializeComponent();
            DebugLog.Static.Write("CosmographerView", "InitializeComponent succeeded");
        }
        catch (System.Exception ex)
        {
            DebugLog.Static.Write("CosmographerView", $"InitializeComponent FAILED: {ex}");
            throw;
        }
    }
}
