using System.IO.Compression;
using System.Text;

namespace TheCanonry.Illuminator.PrePrint.Export;

/// <summary>
/// Generates IDML (InDesign Markup Language) — a complete document package.
/// IDML is a ZIP archive containing multiple XML files that InDesign can open directly.
///
/// Structure:
/// - mimetype                   — MIME type declaration (uncompressed)
/// - META-INF/container.xml     — package manifest
/// - designmap.xml              — document manifest (story + spread references)
/// - Resources/Styles.xml       — paragraph and character style definitions
/// - Resources/Fonts.xml        — font declarations
/// - Resources/Graphic.xml      — graphics settings
/// - Resources/Preferences.xml  — document preferences
/// - MasterSpreads/             — master page spreads
/// - Spreads/                   — one spread per content entry
/// - Stories/                   — one Story XML per content item
/// - XML/BackingStory.xml        — required backing story
/// - XML/Tags.xml               — XML tags
/// </summary>
public static class IdmlExporter
{
    private const string DomVersion = "8.0";
    private const string IdLayer = "uc5";
    private const string IdSection = "uc9";

    // =========================================================================
    // Public API
    // =========================================================================

    /// <summary>
    /// Export content as IDML package (ZIP containing XML files).
    /// Each FlattenedNode produces one Story XML and one Spread XML.
    /// </summary>
    public static byte[] Export(
        IReadOnlyList<FlattenedNode> flatNodes,
        Func<FlattenedNode, string> contentResolver)
    {
        var stories = BuildStories(flatNodes, contentResolver);
        var spreads = BuildSpreads(stories);
        var masterSpreads = BuildMasterSpreads();
        var totalPages = Math.Max(2, spreads.Count * 2);

        var masterStoryIds = masterSpreads.Select(ms => ms.StoryId).ToList();
        var allStoryIds = stories.Select(s => s.StoryId)
            .Concat(masterStoryIds)
            .ToList();

        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            // mimetype must be the first entry and uncompressed
            AddEntry(archive, "mimetype",
                "application/vnd.adobe.indesign-idml-package",
                CompressionLevel.NoCompression);

            AddEntry(archive, "META-INF/container.xml", BuildContainerXml());

            AddEntry(archive, "designmap.xml",
                BuildDesignmap(masterSpreads, spreads, stories, masterStoryIds, totalPages, allStoryIds));

            AddEntry(archive, "Resources/Styles.xml", IcmlStyles.GenerateIdmlStylesXml());
            AddEntry(archive, "Resources/Fonts.xml", BuildFontsXml());
            AddEntry(archive, "Resources/Graphic.xml", BuildGraphicXml());
            AddEntry(archive, "Resources/Preferences.xml", BuildPreferencesXml());

            foreach (var ms2 in masterSpreads)
                AddEntry(archive, ms2.Filename, ms2.Xml);

            foreach (var spread in spreads)
                AddEntry(archive, spread.Filename, spread.Xml);

            foreach (var story in stories)
                AddEntry(archive, story.Filename, story.Xml);

            foreach (var ms3 in masterSpreads)
                AddEntry(archive, $"Stories/Story_{ms3.StoryId}.xml", BuildMasterStoryXml(ms3.StoryId));

            AddEntry(archive, "XML/BackingStory.xml", BuildBackingStoryXml());
            AddEntry(archive, "XML/Tags.xml", BuildTagsXml());
        }

        return ms.ToArray();
    }

    // =========================================================================
    // Story Building
    // =========================================================================

    private sealed record StoryFile(string Filename, string Xml, string StoryId);
    private sealed record SpreadFile(string Filename, string Xml, string SpreadId, string StoryId);

    private static List<StoryFile> BuildStories(
        IReadOnlyList<FlattenedNode> flatNodes,
        Func<FlattenedNode, string> contentResolver)
    {
        var stories = new List<StoryFile>();
        var index = 0;

        foreach (var node in flatNodes)
        {
            var storyId = $"s{index:D4}";
            string storyXml;

            if (node.Type == ContentNodeType.Folder)
            {
                storyXml = BuildFolderStoryXml(storyId, node.Name);
            }
            else
            {
                var markdown = contentResolver(node);
                storyXml = BuildContentStoryXml(storyId, node.Name, markdown ?? "");
            }

            stories.Add(new StoryFile($"Stories/Story_{storyId}.xml", storyXml, storyId));
            index++;
        }

        return stories;
    }

    private static string BuildFolderStoryXml(string storyId, string name)
    {
        var paras = new List<IcmlParagraph>
        {
            new(IcmlStyles.SectionHeading, [new IcmlRun("", name)]),
        };
        return WrapStory(storyId, IcmlExporter.RenderParagraphs(paras));
    }

    private static string BuildContentStoryXml(string storyId, string name, string markdown)
    {
        var paras = new List<IcmlParagraph>
        {
            new(IcmlStyles.ItemTitle, [new IcmlRun("", name)]),
        };

        if (!string.IsNullOrWhiteSpace(markdown))
            paras.AddRange(IcmlExporter.MarkdownToIcmlParagraphs(markdown));

        return WrapStory(storyId, IcmlExporter.RenderParagraphs(paras));
    }

    private static string WrapStory(string storyId, string content)
    {
        return $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <?aid style="50" type="story" readerVersion="6.0" featureSet="257" product="8.0(370)" ?>
            <idPkg:Story xmlns:idPkg="http://ns.adobe.com/AdobeInDesign/idml/1.0/packaging"
                DOMVersion="{DomVersion}">
              <Story Self="{storyId}" TrackChanges="false" StoryTitle="" AppliedTOCStyle="n" AppliedNamedGrid="n">
                <StoryPreference OpticalMarginAlignment="true" OpticalMarginSize="12" />
            {content}
              </Story>
            </idPkg:Story>
            """;
    }

    // =========================================================================
    // Spread Building
    // =========================================================================

    private static List<SpreadFile> BuildSpreads(IReadOnlyList<StoryFile> stories)
    {
        var spreads = new List<SpreadFile>();

        for (var i = 0; i < stories.Count; i++)
        {
            var story = stories[i];
            var spreadId = $"sp{i:D4}";
            var pageIndex = i * 2; // 2 pages per entry (recto + verso)
            spreads.Add(new SpreadFile(
                $"Spreads/Spread_{spreadId}.xml",
                BuildSpreadXml(spreadId, story.StoryId, pageIndex),
                spreadId,
                story.StoryId));
        }

        return spreads;
    }

    private static string BuildSpreadXml(string spreadId, string storyId, int pageIndex)
    {
        var leftPageId = $"{spreadId}_L";
        var rightPageId = $"{spreadId}_R";
        var leftFrameId = $"{spreadId}_fL";
        var rightFrameId = $"{spreadId}_fR";

        // Standard trade 6x9 dimensions in points
        const double PageW = 432.0;  // 6in
        const double PageH = 648.0;  // 9in
        const double MarginTop = 54.0;    // 0.75in
        const double MarginBottom = 54.0;
        const double MarginInside = 54.0;
        const double MarginOutside = 40.5; // 0.5625in
        const double TextW = PageW - MarginInside - MarginOutside;
        const double TextH = PageH - MarginTop - MarginBottom;

        var frameTy = -(PageH / 2) + MarginTop;
        var rectoFrameTx = MarginInside;
        var versoFrameTx = -(PageW - MarginOutside);

        return $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <?aid style="50" type="spread" readerVersion="6.0" featureSet="257" product="8.0(370)" ?>
            <idPkg:Spread xmlns:idPkg="http://ns.adobe.com/AdobeInDesign/idml/1.0/packaging"
                DOMVersion="{DomVersion}">
              <Spread Self="{spreadId}"
                  PageCount="2"
                  AllowPageShuffle="true"
                  FlattenerOverride="Default"
                  BindingLocation="0"
                  ActiveLayer="{IdLayer}">

                <Page Self="{leftPageId}"
                    AppliedTrapPreset="TrapPreset/$ID/Default"
                    Bounds="{-PageH / 2} {-PageW} {PageH / 2} 0"
                    MasterPageTransform="1 0 0 1 {-PageW} 0"
                    Name="{pageIndex}"
                    OverrideList=""
                    AppliedMaster="MasterSpread/mA">
                  <MarginPreference ColumnCount="1" ColumnGutter="12"
                      Top="{MarginTop}" Bottom="{MarginBottom}"
                      Left="{MarginInside}" Right="{MarginOutside}" />
                </Page>

                <Page Self="{rightPageId}"
                    AppliedTrapPreset="TrapPreset/$ID/Default"
                    Bounds="{-PageH / 2} 0 {PageH / 2} {PageW}"
                    MasterPageTransform="1 0 0 1 0 0"
                    Name="{pageIndex + 1}"
                    OverrideList=""
                    AppliedMaster="MasterSpread/mA">
                  <MarginPreference ColumnCount="1" ColumnGutter="12"
                      Top="{MarginTop}" Bottom="{MarginBottom}"
                      Left="{MarginInside}" Right="{MarginOutside}" />
                </Page>

                <TextFrame Self="{leftFrameId}" ParentStory="{storyId}"
                    ContentType="TextType"
                    AppliedObjectStyle="ObjectStyle/$ID/[Normal Text Frame]"
                    GeometricBounds="{frameTy} {versoFrameTx} {frameTy + TextH} {versoFrameTx + TextW}"
                    ItemLayer="{IdLayer}">
                  <TextFramePreference TextColumnCount="1" TextColumnGutter="12"
                      TextColumnFixedWidth="{TextW}" />
                </TextFrame>

                <TextFrame Self="{rightFrameId}" ParentStory="{storyId}"
                    PreviousTextFrame="{leftFrameId}"
                    ContentType="TextType"
                    AppliedObjectStyle="ObjectStyle/$ID/[Normal Text Frame]"
                    GeometricBounds="{frameTy} {rectoFrameTx} {frameTy + TextH} {rectoFrameTx + TextW}"
                    ItemLayer="{IdLayer}">
                  <TextFramePreference TextColumnCount="1" TextColumnGutter="12"
                      TextColumnFixedWidth="{TextW}" />
                </TextFrame>

              </Spread>
            </idPkg:Spread>
            """;
    }

    // =========================================================================
    // Master Spreads
    // =========================================================================

    private sealed record MasterSpreadInfo(string SpreadId, string StoryId, string Name, string Prefix, string Filename, string Xml);

    private static List<MasterSpreadInfo> BuildMasterSpreads()
    {
        var masters = new[]
        {
            ("mA", "mA_s", "A-Story",       "A"),
            ("mB", "mB_s", "B-Document",    "B"),
            ("mC", "mC_s", "C-Narrative",   "C"),
            ("mD", "mD_s", "D-Encyclopedia","D"),
        };

        return masters.Select(m => new MasterSpreadInfo(
            m.Item1, m.Item2, m.Item3, m.Item4,
            $"MasterSpreads/MasterSpread_{m.Item1}.xml",
            BuildMasterSpreadXml(m.Item1, m.Item2, m.Item3, m.Item4)
        )).ToList();
    }

    private static string BuildMasterSpreadXml(string spreadId, string storyId, string name, string prefix)
    {
        const double PageW = 432.0;
        const double PageH = 648.0;

        return $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <?aid style="50" type="masterSpread" readerVersion="6.0" featureSet="257" product="8.0(370)" ?>
            <idPkg:MasterSpread xmlns:idPkg="http://ns.adobe.com/AdobeInDesign/idml/1.0/packaging"
                DOMVersion="{DomVersion}">
              <MasterSpread Self="MasterSpread/{spreadId}"
                  NamePrefix="{prefix}"
                  BaseName="{name}"
                  PageCount="2"
                  AllowPageShuffle="true">

                <Page Self="{spreadId}_L"
                    Bounds="{-PageH / 2} {-PageW} {PageH / 2} 0"
                    MasterPageTransform="1 0 0 1 {-PageW} 0"
                    Name="" />

                <Page Self="{spreadId}_R"
                    Bounds="{-PageH / 2} 0 {PageH / 2} {PageW}"
                    MasterPageTransform="1 0 0 1 0 0"
                    Name="" />

              </MasterSpread>
            </idPkg:MasterSpread>
            """;
    }

    private static string BuildMasterStoryXml(string storyId) =>
        $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <?aid style="50" type="story" readerVersion="6.0" featureSet="257" product="8.0(370)" ?>
        <idPkg:Story xmlns:idPkg="http://ns.adobe.com/AdobeInDesign/idml/1.0/packaging"
            DOMVersion="{DomVersion}">
          <Story Self="{storyId}" TrackChanges="false" StoryTitle="" AppliedTOCStyle="n" AppliedNamedGrid="n">
            <StoryPreference OpticalMarginAlignment="true" OpticalMarginSize="12" />
          </Story>
        </idPkg:Story>
        """;

    // =========================================================================
    // designmap.xml
    // =========================================================================

    private static string BuildDesignmap(
        IReadOnlyList<MasterSpreadInfo> masterSpreads,
        IReadOnlyList<SpreadFile> spreads,
        IReadOnlyList<StoryFile> stories,
        IReadOnlyList<string> masterStoryIds,
        int totalPageCount,
        IReadOnlyList<string> allStoryIds)
    {
        var masterRefs = string.Join("\n",
            masterSpreads.Select(ms => $"  <idPkg:MasterSpread src=\"{ms.Filename}\" />"));
        var spreadRefs = string.Join("\n",
            spreads.Select(s => $"  <idPkg:Spread src=\"{s.Filename}\" />"));
        var storyRefs = string.Join("\n",
            stories.Select(s => $"  <idPkg:Story src=\"{s.Filename}\" />"));
        var masterStoryRefs = string.Join("\n",
            masterStoryIds.Select(id => $"  <idPkg:Story src=\"Stories/Story_{id}.xml\" />"));
        var storyListStr = string.Join(" ", allStoryIds);

        return $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <?aid style="50" type="document" readerVersion="6.0" featureSet="257" product="8.0(370)" ?>
            <Document xmlns:idPkg="http://ns.adobe.com/AdobeInDesign/idml/1.0/packaging"
                DOMVersion="{DomVersion}"
                Self="d"
                StoryList="{storyListStr}"
                Name=""
                ZeroPoint="0 0"
                ActiveLayer="{IdLayer}">

              <Language Self="Language/$ID/English%3a USA"
                  Name="$ID/English: USA"
                  Id="269"
                  HyphenationVendor="Hunspell"
                  SpellingVendor="Hunspell" />

              <idPkg:Graphic src="Resources/Graphic.xml" />
              <idPkg:Fonts src="Resources/Fonts.xml" />
              <idPkg:Styles src="Resources/Styles.xml" />
              <idPkg:Preferences src="Resources/Preferences.xml" />

              <Layer Self="{IdLayer}" Name="Layer 1" Visible="true" Locked="false"
                  IgnoreWrap="false" ShowGuides="true" LockGuides="false"
                  UI="true" Expendable="true" Printable="true" />

            {masterRefs}
            {spreadRefs}

              <Section Self="{IdSection}" Length="{totalPageCount}" Name="" AutomaticNumbering="true"
                  IncludeSectionPrefix="false" Marker=""
                  PageNumberStart="1" SectionPrefix=""
                  PageNumberStyle="Arabic"
                  ContinueNumbering="false" />

            {storyRefs}
            {masterStoryRefs}

              <idPkg:BackingStory src="XML/BackingStory.xml" />
              <idPkg:Tags src="XML/Tags.xml" />

            </Document>
            """;
    }

    // =========================================================================
    // Resource Files
    // =========================================================================

    private static string BuildContainerXml() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
          <rootfiles>
            <rootfile full-path="designmap.xml" media-type="text/xml" />
          </rootfiles>
        </container>
        """;

    private static string BuildFontsXml() =>
        $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <idPkg:Fonts xmlns:idPkg="http://ns.adobe.com/AdobeInDesign/idml/1.0/packaging"
            DOMVersion="{DomVersion}">
          <FontFamily Self="FontFamily/Junicode" Name="Junicode">
            <Font Self="Font/Junicode%09Regular%09" FontFamily="Junicode"
                Name="Junicode&#x9;Regular&#x9;" PostScriptName="Junicode-Regular"
                Status="Installed" FontStyleName="Regular" FontType="OpenType" />
          </FontFamily>
          <FontFamily Self="FontFamily/Courier New" Name="Courier New">
            <Font Self="Font/Courier New%09Regular%09" FontFamily="Courier New"
                Name="Courier New&#x9;Regular&#x9;" PostScriptName="CourierNewPSMT"
                Status="Installed" FontStyleName="Regular" FontType="TrueType" />
          </FontFamily>
        </idPkg:Fonts>
        """;

    private static string BuildGraphicXml() =>
        $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <idPkg:Graphic xmlns:idPkg="http://ns.adobe.com/AdobeInDesign/idml/1.0/packaging"
            DOMVersion="{DomVersion}">
          <SVGExportPreference Self="SVGExportPreference/" />
        </idPkg:Graphic>
        """;

    private static string BuildPreferencesXml()
    {
        const double PageW = 432.0;
        const double PageH = 648.0;
        const double MarginTop = 54.0;
        const double MarginBottom = 54.0;
        const double MarginInside = 54.0;
        const double MarginOutside = 40.5;
        const double TextW = PageW - MarginInside - MarginOutside;

        return $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <idPkg:Preferences xmlns:idPkg="http://ns.adobe.com/AdobeInDesign/idml/1.0/packaging"
                DOMVersion="{DomVersion}">

              <DocumentPreference Self="DocumentPreference/"
                  PageHeight="{PageH}"
                  PageWidth="{PageW}"
                  PagesPerDocument="1"
                  FacingPages="true"
                  DocumentBleedTopOffset="0"
                  DocumentBleedBottomOffset="0"
                  DocumentBleedInsideOrLeftOffset="0"
                  DocumentBleedOutsideOrRightOffset="0"
                  PageBinding="LeftToRight"
                  AllowPageShuffle="true"
                  Intent="PrintIntent" />

              <MarginPreference Self="MarginPreference/"
                  ColumnCount="1"
                  ColumnGutter="12"
                  Top="{MarginTop}"
                  Bottom="{MarginBottom}"
                  Left="{MarginInside}"
                  Right="{MarginOutside}"
                  ColumnPositions="0 {TextW}" />

            </idPkg:Preferences>
            """;
    }

    private static string BuildBackingStoryXml() =>
        $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <?aid style="50" type="backingStory" readerVersion="6.0" featureSet="257" product="8.0(370)" ?>
        <idPkg:BackingStory xmlns:idPkg="http://ns.adobe.com/AdobeInDesign/idml/1.0/packaging"
            DOMVersion="{DomVersion}">
          <XmlStory Self="xbs" TrackChanges="false" StoryTitle="" AppliedTOCStyle="n" AppliedNamedGrid="n">
            <StoryPreference OpticalMarginAlignment="true" OpticalMarginSize="12" />
          </XmlStory>
        </idPkg:BackingStory>
        """;

    private static string BuildTagsXml() =>
        $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <idPkg:Tags xmlns:idPkg="http://ns.adobe.com/AdobeInDesign/idml/1.0/packaging"
            DOMVersion="{DomVersion}">
          <XMLTag Self="XMLTag/Root" Name="Root" />
        </idPkg:Tags>
        """;

    // =========================================================================
    // ZIP Helpers
    // =========================================================================

    private static void AddEntry(
        ZipArchive archive,
        string entryPath,
        string content,
        CompressionLevel level = CompressionLevel.Optimal)
    {
        var entry = archive.CreateEntry(entryPath, level);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }
}
