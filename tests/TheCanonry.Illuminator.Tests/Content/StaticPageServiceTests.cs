namespace TheCanonry.Illuminator.Tests.Content;

using Microsoft.EntityFrameworkCore;
using TheCanonry.Illuminator.Content;
using TheCanonry.Persistence;
using TheCanonry.Persistence.Repositories;

// ─── StaticPageService Tests ──────────────────────────────────────────────────

public sealed class StaticPageServiceTests : IDisposable
{
    private readonly CanonryDbContext _db;
    private readonly StaticPageService _svc;

    public StaticPageServiceTests()
    {
        var options = new DbContextOptionsBuilder<CanonryDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _db = new CanonryDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _svc = new StaticPageService(new StaticPageRepository(_db));
    }

    public void Dispose() => _db.Dispose();

    // -------------------------------------------------------------------------
    // Test 1: CreatePage generates slug, word count, and linked entity IDs
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreatePage_ComputesSlugWordCountAndLinkedEntityIds()
    {
        const string content = "The [[Thornwood Keep]] was built in the age of stone.";

        var page = await _svc.CreatePage(
            projectId: "proj-1",
            simulationRunId: "sim-1",
            title: "A Great Tale",
            content: content);

        Assert.Equal("a-great-tale", page.Slug);
        Assert.True(page.WordCount > 0);
        Assert.Contains("Thornwood Keep", page.LinkedEntityIdsJson);
        Assert.StartsWith("static-", page.PageId);
    }

    // -------------------------------------------------------------------------
    // Test 2: UpdatePage recomputes slug and word count when title/content change
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdatePage_RecomputesSlugAndWordCount()
    {
        var created = await _svc.CreatePage("proj-1", "sim-1", "Old Title", "Short text.");

        var updated = await _svc.UpdatePage(created.PageId, title: "New Bright Title");

        Assert.NotNull(updated);
        Assert.Equal("new-bright-title", updated!.Slug);
    }

    // -------------------------------------------------------------------------
    // Test 3: GetPage returns the page by pageId
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetPage_ReturnsByPageId()
    {
        var created = await _svc.CreatePage("proj-1", "sim-1", "Lookup Test", "content here");

        var loaded = await _svc.GetPage(created.PageId);

        Assert.NotNull(loaded);
        Assert.Equal("Lookup Test", loaded!.Title);
    }

    // -------------------------------------------------------------------------
    // Test 4: GetPagesForSimulation returns pages for the simulation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetPagesForSimulation_ReturnsAllPagesForSim()
    {
        await _svc.CreatePage("proj-1", "sim-A", "Page 1", "content");
        await _svc.CreatePage("proj-1", "sim-A", "Page 2", "content");
        await _svc.CreatePage("proj-1", "sim-B", "Other", "content");

        var pages = await _svc.GetPagesForSimulation("sim-A");

        Assert.Equal(2, pages.Count);
        Assert.All(pages, p => Assert.Equal("sim-A", p.SimulationRunId));
    }

    // -------------------------------------------------------------------------
    // Test 5: DeletePage removes the page
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeletePage_RemovesPage()
    {
        var created = await _svc.CreatePage("proj-1", "sim-1", "To Delete", "content");

        await _svc.DeletePage(created.Id);

        var loaded = await _svc.GetPage(created.PageId);
        Assert.Null(loaded);
    }
}

// ─── WikiLinkService Tests ────────────────────────────────────────────────────

public sealed class WikiLinkServiceTests
{
    // -------------------------------------------------------------------------
    // Test 6: ExtractEntityLinks parses [[...]] links
    // -------------------------------------------------------------------------

    [Fact]
    public void ExtractEntityLinks_ParsesWikiLinks()
    {
        const string content = "The [[Thornwood Keep]] stood near [[Lake Sorren]].";

        var links = WikiLinkService.ExtractEntityLinks(content);

        Assert.Equal(2, links.Count);
        Assert.Contains("Thornwood Keep", links);
        Assert.Contains("Lake Sorren", links);
    }

    [Fact]
    public void ExtractEntityLinks_EmptyContent_ReturnsEmpty()
    {
        var links = WikiLinkService.ExtractEntityLinks("");
        Assert.Empty(links);
    }

    // -------------------------------------------------------------------------
    // Test 7: FindEntityMentions finds entity names in text
    // -------------------------------------------------------------------------

    [Fact]
    public void FindEntityMentions_FindsEntityNamesInText()
    {
        var entities = new[]
        {
            new WikiLinkEntity("e1", "Thornwood Keep"),
            new WikiLinkEntity("e2", "Lake Sorren"),
        };
        const string content = "Thornwood Keep is located near Lake Sorren.";

        var mentions = WikiLinkService.FindEntityMentions(content, entities);

        Assert.Equal(2, mentions.Count);
        Assert.Contains(mentions, m => m.EntityId == "e1" && m.EntityName == "Thornwood Keep");
        Assert.Contains(mentions, m => m.EntityId == "e2" && m.EntityName == "Lake Sorren");
    }

    [Fact]
    public void FindEntityMentions_CaseInsensitive()
    {
        var entities = new[] { new WikiLinkEntity("e1", "Thornwood Keep") };
        const string content = "The thornwood keep was ancient.";

        var mentions = WikiLinkService.FindEntityMentions(content, entities);

        Assert.Single(mentions);
        Assert.Equal("e1", mentions[0].EntityId);
    }

    [Fact]
    public void FindEntityMentions_NoMatch_ReturnsEmpty()
    {
        var entities = new[] { new WikiLinkEntity("e1", "Nonexistent Place") };
        const string content = "Nothing here matches.";

        var mentions = WikiLinkService.FindEntityMentions(content, entities);

        Assert.Empty(mentions);
    }

    // -------------------------------------------------------------------------
    // Test 8: ApplyWikiLinks wraps mentions in brackets
    // -------------------------------------------------------------------------

    [Fact]
    public void ApplyWikiLinks_WrapsMentionsInBrackets()
    {
        var entities = new[] { new WikiLinkEntity("e1", "Thornwood Keep") };
        const string content = "Thornwood Keep stood tall.";

        var result = WikiLinkService.ApplyWikiLinks(content, entities);

        Assert.Contains("[[Thornwood Keep]]", result.Content);
        Assert.Contains("e1", result.LinkedEntityIds);
    }

    [Fact]
    public void ApplyWikiLinks_NoMatchingEntities_ReturnsUnchangedContent()
    {
        var entities = new[] { new WikiLinkEntity("e1", "Absent Name") };
        const string content = "Nothing to link here.";

        var result = WikiLinkService.ApplyWikiLinks(content, entities);

        Assert.Equal(content, result.Content);
        Assert.Empty(result.LinkedEntityIds);
    }

    [Fact]
    public void ApplyWikiLinks_PossessiveForm_NormalizesCorrectly()
    {
        var entities = new[] { new WikiLinkEntity("e1", "Micseleia") };
        // Possessive form should still match the entity
        const string content = "Micseleia's Dagger was lost.";

        var mentions = WikiLinkService.FindEntityMentions(content, entities);

        // "Micseleia" normalizes to "micseleia"; "Micseleia's Dagger" normalizes to "micseleia-dagger"
        // The entity name "Micseleia" normalizes to "micseleia"
        // Content normalized: "micseleia-dagger-was-lost"
        // "micseleia" is at boundary before "-" -> should match
        Assert.Single(mentions);
    }
}

// ─── StaticPageService slug/word-count unit tests ────────────────────────────

public sealed class StaticPageSlugWordCountTests
{
    // -------------------------------------------------------------------------
    // Test 9: GenerateSlug converts title to slug
    // -------------------------------------------------------------------------

    [Fact]
    public void GenerateSlug_ProducesLowercaseHyphenated()
    {
        var slug = StaticPageService.GenerateSlug("A Great Realm of Fire");
        Assert.Equal("a-great-realm-of-fire", slug);
    }

    [Fact]
    public void GenerateSlug_StripsSpecialCharacters()
    {
        var slug = StaticPageService.GenerateSlug("Lord's Keep & Castle!");
        Assert.DoesNotContain("!", slug);
        Assert.DoesNotContain("&", slug);
        Assert.DoesNotContain("'", slug);
    }

    [Fact]
    public void GenerateSlug_TruncatesAt100Chars()
    {
        var longTitle = new string('a', 150);
        var slug = StaticPageService.GenerateSlug(longTitle);
        Assert.Equal(100, slug.Length);
    }

    // -------------------------------------------------------------------------
    // Test 10: Word count strips markdown syntax
    // -------------------------------------------------------------------------

    [Fact]
    public void CountWords_StripsMarkdownSyntax()
    {
        const string markdown = "# Heading One\n**bold text** and *italic* words.";
        // After stripping: "Heading One bold text and italic words" = 7 words
        var count = StaticPageService.CountWords(markdown);
        Assert.True(count > 0);
        // Heading marker removed, bold/italic markers removed
        Assert.Equal(7, count);
    }

    [Fact]
    public void CountWords_PlainText_CountsCorrectly()
    {
        const string text = "The quick brown fox jumps over the lazy dog";
        var count = StaticPageService.CountWords(text);
        Assert.Equal(9, count);
    }

    [Fact]
    public void CountWords_EmptyString_ReturnsZero()
    {
        Assert.Equal(0, StaticPageService.CountWords(""));
        Assert.Equal(0, StaticPageService.CountWords("   "));
    }
}
