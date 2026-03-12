using System.IO.Compression;
using System.Text;
using TheCanonry.Illuminator.PrePrint;
using TheCanonry.Illuminator.PrePrint.Export;

namespace TheCanonry.Illuminator.Tests.PrePrint;

public sealed class MarkdownExportTests
{
    // =========================================================================
    // MarkdownFormatters.FormatEntity
    // =========================================================================

    [Fact]
    public void FormatEntity_ContainsYamlFrontmatterAndDescription()
    {
        var result = MarkdownFormatters.FormatEntity(
            name: "Aldric the Elder",
            kind: "npc",
            subtype: "merchant",
            culture: "maritime",
            prominence: "renowned",
            description: "A weathered trader who has sailed every sea.",
            historianNotes: null);

        // YAML frontmatter present
        Assert.Contains("---", result);
        Assert.Contains("title: Aldric the Elder", result);
        Assert.Contains("type: entity", result);
        Assert.Contains("entity_kind: npc", result);
        Assert.Contains("entity_subtype: merchant", result);
        Assert.Contains("culture: maritime", result);
        Assert.Contains("prominence: renowned", result);

        // Body present
        Assert.Contains("# Aldric the Elder", result);
        Assert.Contains("A weathered trader who has sailed every sea.", result);
    }

    [Fact]
    public void FormatEntity_OptionalFieldsOmittedWhenNull()
    {
        var result = MarkdownFormatters.FormatEntity(
            name: "TestEntity",
            kind: "place",
            subtype: null,
            culture: null,
            prominence: "marginal",
            description: null,
            historianNotes: null);

        Assert.DoesNotContain("entity_subtype", result);
        Assert.DoesNotContain("culture:", result);
        Assert.DoesNotContain("Historian Notes", result);
    }

    [Fact]
    public void FormatEntity_HistorianNotesAppearsAsFootnotes()
    {
        var notes = new List<string> { "Note about origins.", "Contested account." };
        var result = MarkdownFormatters.FormatEntity(
            name: "Mira",
            kind: "npc",
            subtype: null,
            culture: null,
            prominence: "recognized",
            description: "A scholar.",
            historianNotes: notes);

        Assert.Contains("## Historian Notes", result);
        Assert.Contains("[^1]: Note about origins.", result);
        Assert.Contains("[^2]: Contested account.", result);
    }

    // =========================================================================
    // MarkdownFormatters.FormatChronicle
    // =========================================================================

    [Fact]
    public void FormatChronicle_IncludesTitleAndContent()
    {
        var result = MarkdownFormatters.FormatChronicle(
            title: "The Fall of Ironhold",
            format: "narrative",
            summary: "A tale of siege and treachery.",
            content: "Long ago, Ironhold stood unconquered...",
            historianNotes: null);

        Assert.Contains("---", result);
        Assert.Contains("title: The Fall of Ironhold", result);
        Assert.Contains("type: chronicle", result);
        Assert.Contains("format: narrative", result);
        Assert.Contains("# The Fall of Ironhold", result);
        Assert.Contains("*A tale of siege and treachery.*", result);
        Assert.Contains("Long ago, Ironhold stood unconquered...", result);
    }

    // =========================================================================
    // MarkdownFormatters.FormatStaticPage
    // =========================================================================

    [Fact]
    public void FormatStaticPage_MinimalFormattingWorks()
    {
        var result = MarkdownFormatters.FormatStaticPage(
            title: "World Overview",
            summary: null,
            content: "This world was forged from conflict.");

        Assert.Contains("---", result);
        Assert.Contains("title: World Overview", result);
        Assert.Contains("type: static_page", result);
        Assert.Contains("This world was forged from conflict.", result);
    }

    // =========================================================================
    // MarkdownExporter.BuildExportZip
    // =========================================================================

    [Fact]
    public void BuildExportZip_ProducesValidZipWithCorrectFileCount()
    {
        var nodes = new List<FlattenedNode>
        {
            new("n1", "Front Matter", ContentNodeType.Folder,    null,   "01-front-matter", 0),
            new("n2", "World Overview", ContentNodeType.StaticPage, "p1", "01-front-matter", 1),
            new("n3", "Body",         ContentNodeType.Folder,    null,   "02-body",         0),
            new("n4", "Aldric",       ContentNodeType.Entity,    "e1",   "02-body",         1),
            new("n5", "The Chronicle",ContentNodeType.Chronicle, "c1",   "02-body",         1),
        };

        string ContentResolver(FlattenedNode n) => n.Type == ContentNodeType.Folder
            ? ""
            : $"# {n.Name}\n\nContent for {n.Name}.";

        var zipBytes = MarkdownExporter.BuildExportZip(nodes, ContentResolver);

        Assert.NotNull(zipBytes);
        Assert.True(zipBytes.Length > 0);

        // Validate it is a real ZIP
        using var ms = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        // 3 content nodes (not folders) should produce 3 .md entries
        Assert.Equal(3, archive.Entries.Count);
        Assert.All(archive.Entries, e => Assert.EndsWith(".md", e.FullName));
    }

    [Fact]
    public void BuildExportZip_FilesContainExpectedMarkdown()
    {
        var nodes = new List<FlattenedNode>
        {
            new("n1", "Aldric", ContentNodeType.Entity, "e1", "02-body", 1),
        };

        var zipBytes = MarkdownExporter.BuildExportZip(
            nodes,
            n => $"# {n.Name}\n\nThis is the description.");

        using var ms = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        var entry = archive.Entries.Single();
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        var content = reader.ReadToEnd();

        Assert.Contains("# Aldric", content);
        Assert.Contains("This is the description.", content);
    }

    // =========================================================================
    // ExportManifestBuilder.Build
    // =========================================================================

    [Fact]
    public void ExportManifest_ContainsCorrectFilePaths()
    {
        var nodes = new List<FlattenedNode>
        {
            new("n1", "Front Matter", ContentNodeType.Folder,    null, "01-front-matter", 0),
            new("n2", "World Overview", ContentNodeType.StaticPage, "p1", "01-front-matter", 1),
            new("n3", "Aldric",       ContentNodeType.Entity,    "e1", "02-body",         1),
        };

        var manifest = ExportManifestBuilder.Build(nodes, totalImages: 5);

        Assert.Equal(2, manifest.TotalFiles);
        Assert.Equal(5, manifest.TotalImages);
        Assert.Contains("01-front-matter/world-overview.md", manifest.FilePaths);
        Assert.Contains("02-body/aldric.md", manifest.FilePaths);
    }
}
