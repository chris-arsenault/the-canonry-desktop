using System.Text;

namespace TheCanonry.Desktop.Shared;

/// <summary>
/// Simple debug logger. Keeps entries in memory and writes to file.
/// Remove after debugging is complete.
/// </summary>
internal sealed class DebugLog
{
    /// <summary>Static instance for use by views that aren't DI-constructed.</summary>
    public static DebugLog Static { get; } = new();

    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "canonry-debug.log");

    private readonly StringBuilder _buffer = new();
    private readonly object _lock = new();

    public void Write(string source, string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{source}] {message}";
        lock (_lock)
        {
            _buffer.AppendLine(line);
            try { File.AppendAllText(_path, line + "\n"); }
            catch (IOException) { /* ignore file write failures */ }
        }
    }

    public string GetContents()
    {
        lock (_lock)
        {
            return _buffer.ToString();
        }
    }
}
