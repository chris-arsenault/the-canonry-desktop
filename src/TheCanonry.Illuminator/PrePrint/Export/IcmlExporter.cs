using System.Text;
using System.Text.RegularExpressions;

namespace TheCanonry.Illuminator.PrePrint.Export;

/// <summary>
/// Generates ICML (InCopy Markup Language) — a single story XML file.
/// The resulting .icml can be placed in InDesign via File > Place.
/// Markdown content is converted to ICML paragraphs with appropriate style references.
/// </summary>
public static partial class IcmlExporter
{
    private const string IcmlHeader = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <?aid style="50" type="snippet" readerVersion="6.0" featureSet="513" product="8.0(370)" ?>
        <?aid SnippetType="InCopyInterchange"?>
        <Document DOMVersion="8.0" Self="loreweave_doc">
        """;

    private const string IcmlFooter = """
          </Story>
        </Document>
        """;

    // =========================================================================
    // Public API
    // =========================================================================

    /// <summary>
    /// Export content as ICML XML. Each FlattenedNode contributes paragraphs
    /// to a single story with paragraph/character styles applied.
    /// </summary>
    public static string Export(
        IReadOnlyList<FlattenedNode> flatNodes,
        Func<FlattenedNode, string> contentResolver)
    {
        var allParagraphs = new List<IcmlParagraph>();

        foreach (var node in flatNodes)
        {
            if (node.Type == ContentNodeType.Folder)
            {
                // Section heading for folders
                allParagraphs.Add(new IcmlParagraph(IcmlStyles.SectionHeading,
                    [new IcmlRun("", node.Name)]));
                continue;
            }

            var markdown = contentResolver(node);
            if (string.IsNullOrEmpty(markdown)) continue;

            // Title paragraph
            allParagraphs.Add(new IcmlParagraph(IcmlStyles.ItemTitle,
                [new IcmlRun("", node.Name)]));

            // Convert markdown body to paragraphs
            var paras = MarkdownToIcmlParagraphs(markdown);
            allParagraphs.AddRange(paras);

            // Separator between entries
            allParagraphs.Add(new IcmlParagraph(IcmlStyles.ItemSeparator,
                [new IcmlRun("", "* * *")]));
        }

        var sb = new StringBuilder();
        sb.AppendLine(IcmlHeader);
        sb.AppendLine(IcmlStyles.GenerateStyleDefinitions());
        sb.AppendLine(BuildStoryOpen());
        sb.AppendLine(RenderParagraphs(allParagraphs));
        sb.Append(IcmlFooter);
        return sb.ToString();
    }

    // =========================================================================
    // Story Opening
    // =========================================================================

    private static string BuildStoryOpen() =>
        """
          <Story Self="loreweave_story" TrackChanges="false" StoryTitle="" AppliedTOCStyle="n" AppliedNamedGrid="n">
            <StoryPreference OpticalMarginAlignment="true" OpticalMarginSize="12" />
        """;

    // =========================================================================
    // Markdown -> ICML Paragraphs
    // =========================================================================

    /// <summary>
    /// Convert markdown text to ICML paragraphs.
    /// Handles: headings (# ## ###), blockquotes (>), horizontal rules, image markers,
    /// and inline formatting (**bold**, *italic*, ***both***, `code`).
    /// </summary>
    internal static List<IcmlParagraph> MarkdownToIcmlParagraphs(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return [];

        var paras = new List<IcmlParagraph>();
        var lines = markdown.Split('\n');
        var blockLines = new List<string>();
        var afterHeading = false;

        void FlushBlock()
        {
            var text = string.Join(" ", blockLines).Trim();
            if (!string.IsNullOrEmpty(text))
            {
                var style = afterHeading ? IcmlStyles.BodyFirst : IcmlStyles.BodyText;
                paras.Add(new IcmlParagraph(style, ParseInlineRuns(text)));
                afterHeading = false;
            }
            blockLines.Clear();
        }

        foreach (var rawLine in lines)
        {
            var trimmed = rawLine.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                FlushBlock();
                continue;
            }

            // Image marker
            var imgMatch = ImageMarkerPattern().Match(trimmed);
            if (imgMatch.Success)
            {
                FlushBlock();
                var parts = imgMatch.Groups[1].Value.Split('|');
                var path = parts[0].Trim();
                var caption = "";
                foreach (var part in parts)
                {
                    var cm = CaptionPattern().Match(part.Trim());
                    if (cm.Success) caption = cm.Groups[1].Value;
                }
                paras.Add(new IcmlParagraph(IcmlStyles.ImagePlaceholder,
                    [new IcmlRun("", $"[IMAGE: {path}]")]));
                if (!string.IsNullOrEmpty(caption))
                    paras.Add(new IcmlParagraph(IcmlStyles.Caption,
                        [new IcmlRun("", caption)]));
                continue;
            }

            // Heading
            var headingMatch = HeadingPattern().Match(trimmed);
            if (headingMatch.Success)
            {
                FlushBlock();
                var level = headingMatch.Groups[1].Value.Length;
                var headingStyle = level == 1 ? IcmlStyles.Heading1
                                 : level == 2 ? IcmlStyles.Heading2
                                 : IcmlStyles.Heading3;
                paras.Add(new IcmlParagraph(headingStyle,
                    ParseInlineRuns(headingMatch.Groups[2].Value)));
                afterHeading = true;
                continue;
            }

            // Horizontal rule
            if (HorizontalRulePattern().IsMatch(trimmed))
            {
                FlushBlock();
                paras.Add(new IcmlParagraph(IcmlStyles.ItemSeparator,
                    [new IcmlRun("", "* * *")]));
                continue;
            }

            // Blockquote
            if (trimmed.StartsWith('>'))
            {
                FlushBlock();
                var quoteText = trimmed[1..].TrimStart();
                paras.Add(new IcmlParagraph(IcmlStyles.Blockquote,
                    ParseInlineRuns(quoteText)));
                continue;
            }

            blockLines.Add(trimmed);
        }

        FlushBlock();
        return paras;
    }

    // =========================================================================
    // Inline Markdown Parsing
    // =========================================================================

    /// <summary>
    /// Parse inline markdown formatting (***bold italic***, **bold**, *italic*, `code`)
    /// into character style runs.
    /// </summary>
    internal static List<IcmlRun> ParseInlineRuns(string text)
    {
        var runs = new List<IcmlRun>();
        var pattern = InlineFormattingPattern();
        var lastIndex = 0;

        foreach (Match match in pattern.Matches(text))
        {
            if (match.Index > lastIndex)
                runs.Add(new IcmlRun("", text[lastIndex..match.Index]));

            if (match.Groups[2].Success)
                runs.Add(new IcmlRun(IcmlStyles.BoldItalic, match.Groups[2].Value));
            else if (match.Groups[3].Success)
                runs.Add(new IcmlRun(IcmlStyles.Strong, match.Groups[3].Value));
            else if (match.Groups[4].Success)
                runs.Add(new IcmlRun(IcmlStyles.Emphasis, match.Groups[4].Value));
            else if (match.Groups[5].Success)
                runs.Add(new IcmlRun(IcmlStyles.Code, match.Groups[5].Value));

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
            runs.Add(new IcmlRun("", text[lastIndex..]));

        if (runs.Count == 0 && text.Length > 0)
            runs.Add(new IcmlRun("", text));

        return runs;
    }

    // =========================================================================
    // XML Rendering
    // =========================================================================

    internal static string EscapeXml(string text) =>
        text.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");

    private static string RenderRun(IcmlRun run)
    {
        var csAttr = string.IsNullOrEmpty(run.CharStyle)
            ? $"CharacterStyle/{IcmlStyles.NoCharStyle}"
            : $"CharacterStyle/{run.CharStyle}";
        return $"""      <CharacterStyleRange AppliedCharacterStyle="{csAttr}"><Content>{EscapeXml(run.Text)}</Content></CharacterStyleRange>""";
    }

    private static string RenderParagraph(IcmlParagraph para)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"""    <ParagraphStyleRange AppliedParagraphStyle="ParagraphStyle/{para.ParaStyle}">""");
        foreach (var run in para.Runs)
            sb.AppendLine(RenderRun(run));
        sb.Append("    </ParagraphStyleRange>");
        return sb.ToString();
    }

    internal static string RenderParagraphs(IReadOnlyList<IcmlParagraph> paras)
    {
        if (paras.Count == 0) return "";
        return string.Join("\n    <Br/>\n", paras.Select(RenderParagraph));
    }

    // =========================================================================
    // Regex Patterns
    // =========================================================================

    [GeneratedRegex(@"^<!--\s*IMAGE:\s*(.+?)\s*-->$")]
    private static partial Regex ImageMarkerPattern();

    [GeneratedRegex(@"^caption:\s*""?(.*?)""?\s*$")]
    private static partial Regex CaptionPattern();

    [GeneratedRegex(@"^(#{1,3})\s+(.+)$")]
    private static partial Regex HeadingPattern();

    [GeneratedRegex(@"^[-*_]{3,}\s*$")]
    private static partial Regex HorizontalRulePattern();

    [GeneratedRegex(@"(\*\*\*(.+?)\*\*\*|\*\*(.+?)\*\*|\*(.+?)\*|`(.+?)`)")]
    private static partial Regex InlineFormattingPattern();
}

// =========================================================================
// Value Types
// =========================================================================

/// <summary>A character-styled run of text within an ICML paragraph.</summary>
public sealed record IcmlRun(string CharStyle, string Text);

/// <summary>An ICML paragraph with a paragraph style and one or more runs.</summary>
public sealed record IcmlParagraph(string ParaStyle, IReadOnlyList<IcmlRun> Runs);
