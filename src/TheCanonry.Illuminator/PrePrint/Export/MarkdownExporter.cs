namespace TheCanonry.Illuminator.PrePrint.Export;

using System.IO.Compression;
using System.Text;

/// <summary>
/// Builds a ZIP archive from the flattened content tree.
/// Each FlattenedNode becomes a .md file in the ZIP at its slugified path.
/// </summary>
public static class MarkdownExporter
{
    /// <summary>
    /// Build ZIP archive from flattened content tree.
    /// Each FlattenedNode becomes a .md file in the ZIP at its slugified path.
    /// Folder nodes create directories; content nodes create .md files.
    /// </summary>
    public static byte[] BuildExportZip(
        IReadOnlyList<FlattenedNode> flatNodes,
        Func<FlattenedNode, string> contentResolver)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var node in flatNodes)
            {
                if (node.Type == ContentNodeType.Folder)
                {
                    // Create a placeholder entry to ensure the directory exists
                    // (ZIP doesn't require explicit directory entries, so we skip empty ones)
                    continue;
                }

                var markdown = contentResolver(node);
                if (string.IsNullOrEmpty(markdown)) continue;

                var filename = Slugify(node.Name) + ".md";
                var entryPath = string.IsNullOrEmpty(node.Path)
                    ? filename
                    : $"{node.Path}/{filename}";

                var entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
                using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
                writer.Write(markdown);
            }
        }

        return ms.ToArray();
    }

    // =========================================================================
    // Slugify (matches TypeScript markdownHelpers.slugify)
    // =========================================================================

    internal static string Slugify(string name)
    {
        if (string.IsNullOrEmpty(name)) return "untitled";

        var slug = name.ToLowerInvariant();

        // Replace spaces and underscores with hyphens
        slug = slug.Replace(' ', '-').Replace('_', '-');

        // Remove characters that are not alphanumeric or hyphens
        var sb = new StringBuilder();
        foreach (var c in slug)
        {
            if (char.IsAsciiLetterOrDigit(c) || c == '-')
                sb.Append(c);
        }

        // Collapse multiple hyphens
        var result = sb.ToString();
        while (result.Contains("--"))
            result = result.Replace("--", "-");

        return result.Trim('-');
    }
}
