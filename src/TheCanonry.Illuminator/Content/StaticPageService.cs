/**
 * StaticPageService — CRUD wrapper for static wiki pages
 *
 * Computes slug, word count, and linked entity IDs on create/update.
 * Delegates persistence to StaticPageRepository.
 */

using System.Text.RegularExpressions;
using TheCanonry.Persistence.Entities;
using TheCanonry.Persistence.Repositories;

namespace TheCanonry.Illuminator.Content;

public class StaticPageService
{
    private readonly StaticPageRepository _repo;

    public StaticPageService(StaticPageRepository repo)
    {
        _repo = repo;
    }

    // -------------------------------------------------------------------------
    // CRUD operations
    // -------------------------------------------------------------------------

    public async Task<StaticPageEntity> CreatePage(
        string projectId,
        string simulationRunId,
        string title,
        string content)
    {
        var entity = new StaticPageEntity
        {
            PageId = "static-" + Guid.NewGuid().ToString("N"),
            ProjectId = projectId,
            SimulationRunId = simulationRunId,
            Title = title,
            Slug = GenerateSlug(title),
            Content = content,
            LinkedEntityIdsJson = BuildLinkedEntityIdsJson(content),
            WordCount = CountWords(content),
            Status = "draft",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        return await _repo.Create(entity);
    }

    public async Task<StaticPageEntity?> UpdatePage(
        string pageId,
        string? title = null,
        string? content = null)
    {
        var existing = await _repo.GetByPageId(pageId);
        if (existing is null) return null;

        if (title is not null)
        {
            existing.Title = title;
            existing.Slug = GenerateSlug(title);
        }

        if (content is not null)
        {
            existing.Content = content;
            existing.LinkedEntityIdsJson = BuildLinkedEntityIdsJson(content);
            existing.WordCount = CountWords(content);
        }

        existing.UpdatedAt = DateTime.UtcNow;
        return await _repo.Update(existing);
    }

    public Task<StaticPageEntity?> GetPage(string pageId) =>
        _repo.GetByPageId(pageId);

    public Task<List<StaticPageEntity>> GetPagesForSimulation(string simulationRunId) =>
        _repo.GetBySimulation(simulationRunId);

    public Task DeletePage(long id) =>
        _repo.Delete(id);

    // -------------------------------------------------------------------------
    // Slug generation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Generate a URL slug from a title: lowercase, strip non-alphanumeric,
    /// collapse whitespace to hyphens, truncate at 100 characters.
    /// </summary>
    public static string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "";

        var lower = title.ToLowerInvariant();
        // Replace non-alphanumeric, non-space with empty, then collapse spaces to hyphens
        var cleaned = Regex.Replace(lower, @"[^a-z0-9\s-]", "");
        var hyphenated = Regex.Replace(cleaned.Trim(), @"\s+", "-");
        // Collapse multiple hyphens
        hyphenated = Regex.Replace(hyphenated, @"-{2,}", "-");
        return hyphenated.Length > 100 ? hyphenated[..100] : hyphenated;
    }

    // -------------------------------------------------------------------------
    // Word counting
    // -------------------------------------------------------------------------

    private static readonly Regex MarkdownHeadings = new(@"^#+\s+", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex MarkdownBoldItalic = new(@"\*{1,3}([^*]+)\*{1,3}", RegexOptions.Compiled);
    private static readonly Regex MarkdownLinks = new(@"\[([^\]]*)\]\([^)]*\)", RegexOptions.Compiled);
    private static readonly Regex MarkdownWikiLinks = new(@"\[\[([^\]]+)\]\]", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRun = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Count words in markdown content by stripping markdown syntax and splitting on whitespace.
    /// </summary>
    public static int CountWords(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return 0;

        var stripped = content;
        stripped = MarkdownHeadings.Replace(stripped, " ");
        stripped = MarkdownBoldItalic.Replace(stripped, "$1");
        stripped = MarkdownLinks.Replace(stripped, "$1");
        stripped = MarkdownWikiLinks.Replace(stripped, "$1");

        var tokens = WhitespaceRun.Split(stripped.Trim());
        return tokens.Count(t => t.Length > 0);
    }

    // -------------------------------------------------------------------------
    // Linked entity IDs
    // -------------------------------------------------------------------------

    private static string BuildLinkedEntityIdsJson(string content)
    {
        var links = WikiLinkService.ExtractEntityLinks(content);
        if (links.Count == 0) return "[]";

        var json = System.Text.Json.JsonSerializer.Serialize(links);
        return json;
    }
}
