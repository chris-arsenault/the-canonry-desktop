using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Chronicle;

/// <summary>
/// Manages ChronicleVersion snapshots — pure logic, no I/O.
/// </summary>
public class ChronicleVersionManager
{
    private readonly List<ChronicleVersion> _versions = [];
    private string? _activeVersionId;

    public IReadOnlyList<ChronicleVersion> Versions => _versions;
    public string? ActiveVersionId => _activeVersionId;

    /// <summary>
    /// Create a version snapshot from generation output.
    /// </summary>
    public ChronicleVersion CreateVersion(
        string content,
        string model,
        VersionStep step,
        string systemPrompt,
        string userPrompt,
        int? inputTokens = null,
        int? outputTokens = null,
        decimal? estimatedCost = null,
        decimal? actualCost = null)
    {
        var version = new ChronicleVersion
        {
            VersionId = Guid.NewGuid().ToString("N")[..8],
            GeneratedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Content = content,
            WordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            Model = model,
            Step = step,
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            EstimatedCost = estimatedCost,
            ActualCost = actualCost,
        };
        _versions.Add(version);
        _activeVersionId = version.VersionId;
        return version;
    }

    /// <summary>
    /// Get active version content.
    /// </summary>
    public ChronicleVersion? GetActiveVersion() =>
        _versions.FirstOrDefault(v => v.VersionId == _activeVersionId);

    /// <summary>
    /// Switch active version.
    /// </summary>
    public void SetActiveVersion(string versionId)
    {
        if (!_versions.Any(v => v.VersionId == versionId))
            throw new ArgumentException($"Version {versionId} not found");
        _activeVersionId = versionId;
    }

    /// <summary>
    /// Load existing versions (from persistence).
    /// </summary>
    public void LoadVersions(IReadOnlyList<ChronicleVersion> versions, string? activeVersionId)
    {
        _versions.Clear();
        _versions.AddRange(versions);
        _activeVersionId = activeVersionId;
    }
}
