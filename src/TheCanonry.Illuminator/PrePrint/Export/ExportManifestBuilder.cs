namespace TheCanonry.Illuminator.PrePrint.Export;

/// <summary>
/// Builds export manifests containing file lists and stats for export ZIPs.
/// </summary>
public static class ExportManifestBuilder
{
    /// <summary>
    /// Build export manifest with file list and stats.
    /// </summary>
    public static ExportManifest Build(
        IReadOnlyList<FlattenedNode> flatNodes,
        int totalImages)
    {
        var filePaths = flatNodes
            .Where(n => n.Type != ContentNodeType.Folder)
            .Select(n =>
            {
                var filename = MarkdownExporter.Slugify(n.Name) + ".md";
                return string.IsNullOrEmpty(n.Path) ? filename : $"{n.Path}/{filename}";
            })
            .ToList();

        return new ExportManifest(
            GeneratedAt: DateTime.UtcNow,
            TotalFiles: filePaths.Count,
            TotalImages: totalImages,
            FilePaths: filePaths);
    }
}
