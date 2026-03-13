using System.Text;

namespace TheCanonry.Illuminator.PrePrint.Export;

/// <summary>
/// InDesign paragraph and character style definitions.
/// Constants and XML generation for ICML/IDML exports.
/// </summary>
public static class IcmlStyles
{
    // =========================================================================
    // Paragraph Style Names
    // =========================================================================

    public const string SectionHeading = "SectionHeading";
    public const string EraHeading = "EraHeading";
    public const string ItemTitle = "ItemTitle";
    public const string ItemSubtitle = "ItemSubtitle";
    public const string BodyText = "Body";
    public const string BodyFirst = "BodyFirst";
    public const string Heading1 = "Heading1";
    public const string Heading2 = "Heading2";
    public const string Heading3 = "Heading3";
    public const string Blockquote = "Blockquote";
    public const string HistorianNote = "HistorianNote";
    public const string Caption = "Caption";
    public const string ImagePlaceholder = "ImagePlaceholder";
    public const string Metadata = "Metadata";
    public const string CastEntry = "CastEntry";
    public const string ItemSeparator = "ItemSeparator";
    public const string FootnoteText = "FootnoteText";
    public const string CalloutBody = "CalloutBody";
    public const string Frontmatter = "Metadata"; // alias

    // =========================================================================
    // Character Style Names
    // =========================================================================

    public const string Emphasis = "Italic";
    public const string Strong = "Bold";
    public const string BoldItalic = "BoldItalic";
    public const string Code = "Code";
    public const string EntityName = "Bold"; // alias
    public const string FootnoteRef = "FootnoteRef";
    public const string NoCharStyle = "$ID/[No character style]";

    // =========================================================================
    // Style Definition Records
    // =========================================================================

    private sealed record ParagraphStyleDef(
        string Name,
        double PointSize,
        double Leading,
        string FontStyle,
        string Justification,
        double FirstLineIndent,
        double LeftIndent,
        double RightIndent,
        double SpaceBefore,
        double SpaceAfter,
        string AppliedFont);

    private sealed record CharacterStyleDef(
        string Name,
        string FontStyle,
        string? AppliedFont = null,
        string? Position = null);

    // =========================================================================
    // Style Definitions
    // =========================================================================

    private static readonly CharacterStyleDef[] CharacterStyles =
    [
        new("Bold",         "Bold"),
        new("Italic",       "Italic"),
        new("BoldItalic",   "Bold Italic"),
        new("Code",         "Regular", "Courier New"),
        new("Symbol",       "Regular", "Segoe UI Symbol"),
        new("FootnoteRef",  "Regular", Position: "Superscript"),
    ];

    private static readonly ParagraphStyleDef[] ParagraphStyles =
    [
        new(SectionHeading,  28, 34, "Regular",    "CenterAlign",     0,  0,  0, 36, 18, "Junicode"),
        new(EraHeading,      20, 26, "Regular",    "LeftAlign",       0,  0,  0, 24, 12, "Junicode"),
        new(ItemTitle,       16, 20, "Bold",        "LeftAlign",       0,  0,  0, 18,  4, "Junicode"),
        new(ItemSubtitle,    10, 14, "Italic",      "LeftAlign",       0,  0,  0,  0,  8, "Junicode"),
        new(BodyText,        11, 14, "Regular",     "LeftJustified",  18,  0,  0,  0,  4, "Junicode"),
        new(BodyFirst,       11, 14, "Regular",     "LeftJustified",   0,  0,  0,  0,  4, "Junicode"),
        new(Heading1,        14, 18, "Bold",        "LeftAlign",       0,  0,  0, 14,  6, "Junicode"),
        new(Heading2,        12, 16, "Bold",        "LeftAlign",       0,  0,  0, 10,  4, "Junicode"),
        new(Heading3,        11, 14, "Bold Italic",  "LeftAlign",       0,  0,  0,  8,  4, "Junicode"),
        new(Blockquote,      10, 13, "Italic",      "LeftJustified",   0, 24, 24,  6,  6, "Junicode"),
        new(HistorianNote,    9, 12, "Regular",     "LeftAlign",       0, 18,  0,  4,  4, "Junicode"),
        new(Caption,          9, 12, "Italic",      "CenterAlign",     0,  0,  0,  4,  8, "Junicode"),
        new(ImagePlaceholder, 9, 12, "Regular",     "LeftAlign",       0,  0,  0,  8,  8, "Courier New"),
        new(Metadata,         9, 12, "Regular",     "LeftAlign",       0,  0,  0,  2,  2, "Junicode"),
        new(CastEntry,       10, 13, "Regular",     "LeftAlign",       0, 18,  0,  2,  2, "Junicode"),
        new(ItemSeparator,   11, 20, "Regular",     "CenterAlign",     0,  0,  0, 12, 12, "Junicode"),
        new(FootnoteText,     8, 10, "Regular",     "LeftAlign",       0,  0,  0,  0,  2, "Junicode"),
        new(CalloutBody,     10, 13, "Italic",      "LeftAlign",       0, 12,  0,  4,  4, "Junicode"),
    ];

    // =========================================================================
    // XML Generation
    // =========================================================================

    /// <summary>
    /// Generate ICML style definition XML (RootCharacterStyleGroup + RootParagraphStyleGroup).
    /// Used in standalone ICML documents.
    /// </summary>
    public static string GenerateStyleDefinitions()
    {
        var sb = new StringBuilder();

        sb.AppendLine("  <RootCharacterStyleGroup Self=\"rc_styles\">");
        sb.AppendLine("    <CharacterStyle Self=\"CharacterStyle/$ID/[No character style]\" Name=\"[No character style]\" />");
        foreach (var cs in CharacterStyles)
            sb.AppendLine(BuildIcmlCharacterStyle(cs));
        sb.AppendLine("  </RootCharacterStyleGroup>");

        sb.AppendLine("  <RootParagraphStyleGroup Self=\"rp_styles\">");
        sb.AppendLine("    <ParagraphStyle Self=\"ParagraphStyle/$ID/NormalParagraphStyle\" Name=\"[No paragraph style]\" />");
        foreach (var ps in ParagraphStyles)
            sb.AppendLine(BuildIcmlParagraphStyle(ps));
        sb.AppendLine("  </RootParagraphStyleGroup>");

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Generate IDML Styles.xml content (full idPkg:Styles document).
    /// </summary>
    public static string GenerateIdmlStylesXml(string fontFamily = "Junicode")
    {
        var charStyles = CharacterStyles.Select(cs => BuildIdmlCharacterStyle(cs));
        var paraStyles = ParagraphStyles.Select(ps => BuildIdmlParagraphStyleScaled(ps, fontFamily));

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.AppendLine("<idPkg:Styles xmlns:idPkg=\"http://ns.adobe.com/AdobeInDesign/idml/1.0/packaging\"");
        sb.AppendLine("    DOMVersion=\"8.0\">");
        sb.AppendLine();
        sb.AppendLine("  <RootCharacterStyleGroup Self=\"u79\">");
        sb.AppendLine("    <CharacterStyle Self=\"CharacterStyle/$ID/[No character style]\"");
        sb.AppendLine("        Name=\"[No character style]\" />");
        foreach (var cs in charStyles)
            sb.AppendLine(cs);
        sb.AppendLine("  </RootCharacterStyleGroup>");
        sb.AppendLine();
        sb.AppendLine("  <RootParagraphStyleGroup Self=\"u78\">");
        sb.AppendLine("    <ParagraphStyle Self=\"ParagraphStyle/$ID/NormalParagraphStyle\"");
        sb.AppendLine("        Name=\"[No paragraph style]\"");
        sb.AppendLine("        PointSize=\"12\" FontStyle=\"Regular\"");
        sb.AppendLine("        Justification=\"LeftAlign\" Hyphenation=\"true\"");
        sb.AppendLine("        FirstLineIndent=\"0\" LeftIndent=\"0\" RightIndent=\"0\"");
        sb.AppendLine("        SpaceBefore=\"0\" SpaceAfter=\"0\">");
        sb.AppendLine("      <Properties>");
        sb.AppendLine($"        <AppliedFont type=\"string\">{fontFamily}</AppliedFont>");
        sb.AppendLine("        <Leading type=\"unit\">14.4</Leading>");
        sb.AppendLine("      </Properties>");
        sb.AppendLine("    </ParagraphStyle>");
        foreach (var ps in paraStyles)
            sb.AppendLine(ps);
        sb.AppendLine("  </RootParagraphStyleGroup>");
        sb.AppendLine();
        sb.AppendLine("  <RootObjectStyleGroup Self=\"u93\">");
        sb.AppendLine("    <ObjectStyle Self=\"ObjectStyle/$ID/[None]\" Name=\"[None]\" />");
        sb.AppendLine("    <ObjectStyle Self=\"ObjectStyle/$ID/[Normal Graphics Frame]\" Name=\"[Normal Graphics Frame]\" />");
        sb.AppendLine("    <ObjectStyle Self=\"ObjectStyle/$ID/[Normal Text Frame]\" Name=\"[Normal Text Frame]\" />");
        sb.AppendLine("    <ObjectStyle Self=\"ObjectStyle/$ID/[Normal Grid]\" Name=\"[Normal Grid]\" />");
        sb.AppendLine("  </RootObjectStyleGroup>");
        sb.AppendLine();
        sb.AppendLine("  <RootCellStyleGroup Self=\"u88\">");
        sb.AppendLine("    <CellStyle Self=\"CellStyle/$ID/[None]\" Name=\"[None]\" />");
        sb.AppendLine("  </RootCellStyleGroup>");
        sb.AppendLine();
        sb.AppendLine("  <RootTableStyleGroup Self=\"u8a\">");
        sb.AppendLine("    <TableStyle Self=\"TableStyle/$ID/[No table style]\" Name=\"[No table style]\" />");
        sb.AppendLine("    <TableStyle Self=\"TableStyle/$ID/[Basic Table]\" Name=\"[Basic Table]\" />");
        sb.AppendLine("  </RootTableStyleGroup>");
        sb.AppendLine();
        sb.Append("</idPkg:Styles>");
        return sb.ToString();
    }

    // =========================================================================
    // Private XML Builders
    // =========================================================================

    private static string BuildIcmlCharacterStyle(CharacterStyleDef def)
    {
        var selfId = $"CharacterStyle/{def.Name}";
        var attrs = $"Self=\"{selfId}\" Name=\"{def.Name}\"";
        if (!string.IsNullOrEmpty(def.FontStyle)) attrs += $" FontStyle=\"{def.FontStyle}\"";
        if (!string.IsNullOrEmpty(def.Position)) attrs += $" Position=\"{def.Position}\"";

        if (!string.IsNullOrEmpty(def.AppliedFont))
            return $"    <CharacterStyle {attrs}>\n      <Properties><AppliedFont type=\"string\">{def.AppliedFont}</AppliedFont></Properties>\n    </CharacterStyle>";
        return $"    <CharacterStyle {attrs} />";
    }

    private static string BuildIdmlCharacterStyle(CharacterStyleDef def)
    {
        var selfId = $"CharacterStyle/{def.Name}";
        var attrs = $"Self=\"{selfId}\" Name=\"{def.Name}\"";
        if (!string.IsNullOrEmpty(def.FontStyle)) attrs += $" FontStyle=\"{def.FontStyle}\"";
        if (!string.IsNullOrEmpty(def.Position)) attrs += $" Position=\"{def.Position}\"";

        if (!string.IsNullOrEmpty(def.AppliedFont))
            return $"    <CharacterStyle {attrs}>\n      <Properties><AppliedFont type=\"string\">{def.AppliedFont}</AppliedFont></Properties>\n    </CharacterStyle>";
        return $"    <CharacterStyle {attrs} />";
    }

    private static string BuildIcmlParagraphStyle(ParagraphStyleDef def)
    {
        return $"""
            <ParagraphStyle Self="ParagraphStyle/{def.Name}" Name="{def.Name}"
                    PointSize="{def.PointSize}" Leading="{def.Leading}"
                    Justification="{def.Justification}" FontStyle="{def.FontStyle}"
                    FirstLineIndent="{def.FirstLineIndent}"
                    LeftIndent="{def.LeftIndent}" RightIndent="{def.RightIndent}"
                    SpaceBefore="{def.SpaceBefore}" SpaceAfter="{def.SpaceAfter}">
                  <Properties>
                    <BasedOn type="string">$ID/NormalParagraphStyle</BasedOn>
                    <AppliedFont type="string">{def.AppliedFont}</AppliedFont>
                  </Properties>
                </ParagraphStyle>
            """;
    }

    private static string BuildIdmlParagraphStyleScaled(ParagraphStyleDef def, string fontFamily)
    {
        var font = def.AppliedFont == "Courier New" ? "Courier New" : fontFamily;
        var hyphenation = def.Justification.Contains("Justified") ? "true" : "false";
        return $"""
                <ParagraphStyle Self="ParagraphStyle/{def.Name}" Name="{def.Name}"
                        PointSize="{def.PointSize}" FontStyle="{def.FontStyle}"
                        Justification="{def.Justification}"
                        FirstLineIndent="{def.FirstLineIndent}"
                        LeftIndent="{def.LeftIndent}" RightIndent="{def.RightIndent}"
                        SpaceBefore="{def.SpaceBefore}" SpaceAfter="{def.SpaceAfter}"
                        Hyphenation="{hyphenation}">
                      <Properties>
                        <BasedOn type="string">$ID/NormalParagraphStyle</BasedOn>
                        <AppliedFont type="string">{font}</AppliedFont>
                        <Leading type="unit">{def.Leading}</Leading>
                      </Properties>
                    </ParagraphStyle>
                """;
    }
}
