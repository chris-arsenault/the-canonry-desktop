using System.IO.Compression;
using System.Xml;
using TheCanonry.Illuminator.PrePrint;
using TheCanonry.Illuminator.PrePrint.Export;

namespace TheCanonry.Illuminator.Tests.PrePrint;

public sealed class IdmlExportTests
{
    // =========================================================================
    // IcmlStyles.GenerateStyleDefinitions
    // =========================================================================

    [Fact]
    public void IcmlStyles_GeneratesValidXmlFragment()
    {
        var styleXml = IcmlStyles.GenerateStyleDefinitions();

        Assert.NotEmpty(styleXml);
        Assert.Contains("RootCharacterStyleGroup", styleXml);
        Assert.Contains("RootParagraphStyleGroup", styleXml);
        Assert.Contains("SectionHeading", styleXml);
        Assert.Contains("HistorianNote", styleXml);
        Assert.Contains("Blockquote", styleXml);
    }

    [Fact]
    public void IcmlStyles_GenerateIdmlStylesXml_IsWellFormedXml()
    {
        var xml = IcmlStyles.GenerateIdmlStylesXml();

        // Must parse without exceptions
        var doc = new XmlDocument();
        doc.LoadXml(xml); // throws if malformed

        // Should contain key style groups
        Assert.NotNull(doc.SelectSingleNode("//*[local-name()='RootCharacterStyleGroup']"));
        Assert.NotNull(doc.SelectSingleNode("//*[local-name()='RootParagraphStyleGroup']"));
    }

    // =========================================================================
    // IcmlExporter.Export
    // =========================================================================

    [Fact]
    public void IcmlExporter_ProducesWellFormedIcml()
    {
        var nodes = new List<FlattenedNode>
        {
            new("n1", "Introduction", ContentNodeType.Folder,    null, "01-front", 0),
            new("n2", "Aldric",       ContentNodeType.Entity,    "e1", "02-body",  1),
        };

        var icml = IcmlExporter.Export(
            nodes,
            n => n.Type == ContentNodeType.Folder
                ? ""
                : $"# {n.Name}\n\nA renowned merchant of the northern seas.");

        Assert.NotEmpty(icml);
        Assert.Contains("<?xml version=\"1.0\"", icml);
        Assert.Contains("InCopyInterchange", icml);
        Assert.Contains("ParagraphStyleRange", icml);
        Assert.Contains("SectionHeading", icml);
        Assert.Contains("ItemTitle", icml);

        // Must be valid XML
        var doc = new XmlDocument();
        doc.LoadXml(icml);
    }

    [Fact]
    public void IcmlExporter_InlineMarkdownConvertsToCharacterStyles()
    {
        var nodes = new List<FlattenedNode>
        {
            new("n1", "Aldric", ContentNodeType.Entity, "e1", "body", 0),
        };

        var icml = IcmlExporter.Export(
            nodes,
            _ => "He was **bold** and *clever* in equal measure.");

        // Bold and italic character styles should appear
        Assert.Contains("Bold", icml);
        Assert.Contains("Italic", icml);
        Assert.Contains("CharacterStyleRange", icml);
    }

    // =========================================================================
    // IdmlExporter.Export
    // =========================================================================

    [Fact]
    public void IdmlExporter_ProducesValidZip()
    {
        var nodes = new List<FlattenedNode>
        {
            new("n1", "Front Matter", ContentNodeType.Folder,  null, "01-front", 0),
            new("n2", "Aldric",       ContentNodeType.Entity,  "e1", "02-body",  1),
        };

        var idmlBytes = IdmlExporter.Export(
            nodes,
            n => $"# {n.Name}\n\nContent.");

        Assert.NotNull(idmlBytes);
        Assert.True(idmlBytes.Length > 0);

        // Must be a valid ZIP
        using var ms = new MemoryStream(idmlBytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        Assert.NotEmpty(archive.Entries);
    }

    [Fact]
    public void IdmlExporter_ZipContainsRequiredEntries()
    {
        var nodes = new List<FlattenedNode>
        {
            new("n1", "Aldric", ContentNodeType.Entity, "e1", "body", 0),
            new("n2", "Chronicles", ContentNodeType.Folder, null, "body", 0),
        };

        var idmlBytes = IdmlExporter.Export(
            nodes,
            n => "Some content.");

        using var ms = new MemoryStream(idmlBytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        var entryNames = archive.Entries.Select(e => e.FullName).ToHashSet();

        // Required structural entries
        Assert.Contains("mimetype", entryNames);
        Assert.Contains("META-INF/container.xml", entryNames);
        Assert.Contains("designmap.xml", entryNames);
        Assert.Contains("Resources/Styles.xml", entryNames);
        Assert.Contains("Resources/Fonts.xml", entryNames);
        Assert.Contains("Resources/Preferences.xml", entryNames);
        Assert.Contains("XML/BackingStory.xml", entryNames);
        Assert.Contains("XML/Tags.xml", entryNames);

        // Should have at least one Story
        Assert.Contains(entryNames, e => e.StartsWith("Stories/") && e.EndsWith(".xml"));

        // Should have at least one Spread
        Assert.Contains(entryNames, e => e.StartsWith("Spreads/") && e.EndsWith(".xml"));

        // Should have MasterSpreads
        Assert.Contains(entryNames, e => e.StartsWith("MasterSpreads/") && e.EndsWith(".xml"));
    }

    [Fact]
    public void IdmlExporter_DesignmapXmlIsWellFormed()
    {
        var nodes = new List<FlattenedNode>
        {
            new("n1", "Aldric", ContentNodeType.Entity, "e1", "body", 0),
        };

        var idmlBytes = IdmlExporter.Export(nodes, _ => "Content.");

        using var ms = new MemoryStream(idmlBytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        var designmap = archive.GetEntry("designmap.xml");
        Assert.NotNull(designmap);

        using var reader = new System.IO.StreamReader(designmap!.Open());
        var xml = reader.ReadToEnd();

        var doc = new XmlDocument();
        doc.LoadXml(xml); // throws if malformed

        // designmap must reference Stories and the layer
        Assert.Contains("Stories/", xml);
        Assert.Contains("Layer 1", xml);
    }

    [Fact]
    public void IdmlExporter_StoriesXmlAreWellFormed()
    {
        var nodes = new List<FlattenedNode>
        {
            new("n1", "Aldric", ContentNodeType.Entity, "e1", "body", 0),
        };

        var idmlBytes = IdmlExporter.Export(nodes, _ => "# Aldric\n\nA **renowned** merchant.");

        using var ms = new MemoryStream(idmlBytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        var storyEntries = archive.Entries
            .Where(e => e.FullName.StartsWith("Stories/Story_s") && e.FullName.EndsWith(".xml"))
            .ToList();

        Assert.NotEmpty(storyEntries);

        foreach (var entry in storyEntries)
        {
            using var reader = new System.IO.StreamReader(entry.Open());
            var xml = reader.ReadToEnd();

            var doc = new XmlDocument();
            doc.LoadXml(xml); // throws if malformed
        }
    }
}
