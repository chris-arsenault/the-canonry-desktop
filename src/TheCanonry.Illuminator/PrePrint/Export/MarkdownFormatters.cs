namespace TheCanonry.Illuminator.PrePrint.Export;

/// <summary>
/// Per-content-type markdown formatting.
/// Each method produces a complete markdown document with YAML frontmatter.
/// </summary>
public static class MarkdownFormatters
{
    // =========================================================================
    // YAML Helpers
    // =========================================================================

    /// <summary>Wrap a string value for safe use in YAML, quoting if needed.</summary>
    private static string YamlString(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        // Quote if contains special YAML characters
        if (value.Contains(':') || value.Contains('"') || value.Contains('\'') ||
            value.Contains('\n') || value.StartsWith(' ') || value.EndsWith(' '))
        {
            return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
        }
        return value;
    }

    private static int CountWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length;
    }

    // =========================================================================
    // Entity Formatter
    // =========================================================================

    /// <summary>
    /// Format entity as Markdown: YAML frontmatter + description body + historian notes as footnotes.
    /// </summary>
    public static string FormatEntity(
        string name,
        string kind,
        string? subtype,
        string? culture,
        string prominence,
        string? description,
        IReadOnlyList<string>? historianNotes)
    {
        var lines = new List<string>();

        // Frontmatter
        lines.Add("---");
        lines.Add($"title: {YamlString(name)}");
        lines.Add("type: entity");
        lines.Add($"entity_kind: {kind}");
        if (!string.IsNullOrEmpty(subtype)) lines.Add($"entity_subtype: {subtype}");
        if (!string.IsNullOrEmpty(culture)) lines.Add($"culture: {culture}");
        lines.Add($"prominence: {prominence}");
        lines.Add($"word_count: {CountWords(description)}");
        lines.Add("---");
        lines.Add("");

        // Body
        lines.Add($"# {name}");
        lines.Add("");
        if (!string.IsNullOrEmpty(description))
        {
            lines.Add(description);
            lines.Add("");
        }

        // Historian notes as footnotes
        AppendHistorianNotes(lines, historianNotes);

        return string.Join("\n", lines);
    }

    // =========================================================================
    // Chronicle Formatter
    // =========================================================================

    /// <summary>
    /// Format chronicle as Markdown: YAML frontmatter + body + historian notes.
    /// </summary>
    public static string FormatChronicle(
        string title,
        string format,
        string? summary,
        string content,
        IReadOnlyList<string>? historianNotes)
    {
        var lines = new List<string>();

        // Frontmatter
        lines.Add("---");
        lines.Add($"title: {YamlString(title)}");
        lines.Add("type: chronicle");
        lines.Add($"format: {format}");
        lines.Add($"word_count: {CountWords(content)}");
        lines.Add("---");
        lines.Add("");

        // Body
        lines.Add($"# {title}");
        lines.Add("");
        if (!string.IsNullOrEmpty(summary))
        {
            lines.Add($"*{summary}*");
            lines.Add("");
        }

        if (!string.IsNullOrEmpty(content))
        {
            lines.Add(content);
            lines.Add("");
        }

        AppendHistorianNotes(lines, historianNotes);

        return string.Join("\n", lines);
    }

    // =========================================================================
    // Static Page Formatter
    // =========================================================================

    /// <summary>
    /// Format static page as Markdown: YAML frontmatter + body.
    /// </summary>
    public static string FormatStaticPage(string title, string? summary, string content)
    {
        var lines = new List<string>();

        lines.Add("---");
        lines.Add($"title: {YamlString(title)}");
        lines.Add("type: static_page");
        lines.Add($"word_count: {CountWords(content)}");
        lines.Add("---");
        lines.Add("");

        lines.Add(content ?? "");
        lines.Add("");

        return string.Join("\n", lines);
    }

    // =========================================================================
    // Era Narrative Formatter
    // =========================================================================

    /// <summary>
    /// Format era narrative as Markdown: YAML frontmatter + body.
    /// </summary>
    public static string FormatEraNarrative(string eraName, string content)
    {
        var lines = new List<string>();

        lines.Add("---");
        lines.Add($"title: {YamlString(eraName)}");
        lines.Add("type: era_narrative");
        lines.Add($"word_count: {CountWords(content)}");
        lines.Add("---");
        lines.Add("");

        lines.Add($"# {eraName}");
        lines.Add("");
        if (!string.IsNullOrEmpty(content))
        {
            lines.Add(content);
            lines.Add("");
        }

        return string.Join("\n", lines);
    }

    // =========================================================================
    // Shared Helpers
    // =========================================================================

    private static void AppendHistorianNotes(List<string> lines, IReadOnlyList<string>? notes)
    {
        if (notes == null || notes.Count == 0) return;

        lines.Add("---");
        lines.Add("");
        lines.Add("## Historian Notes");
        lines.Add("");
        for (int i = 0; i < notes.Count; i++)
        {
            lines.Add($"[^{i + 1}]: {notes[i]}");
        }
        lines.Add("");
    }
}
