using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Content;

/// <summary>
/// Input entity with optional aliases for tertiary cast detection.
/// </summary>
public sealed record DetectableEntity(string Id, string Name, string Kind, IReadOnlyList<string>? Aliases = null);

/// <summary>
/// Detects entities mentioned in chronicle text that aren't in the declared cast.
/// Uses WikiLinkService's Aho-Corasick matching to find entity name mentions,
/// then filters out entities already in the declared cast.
/// </summary>
public static class TertiaryCastDetector
{
    /// <summary>
    /// Detect tertiary cast — entities mentioned in text but not in the declared cast.
    /// </summary>
    /// <param name="content">Chronicle text to scan.</param>
    /// <param name="allEntities">All entities in the simulation run (with optional aliases).</param>
    /// <param name="declaredEntityIds">Entity IDs already in the declared cast.</param>
    /// <param name="previousDecisions">Previous accept/reject decisions (entityId → accepted) to preserve user choices.</param>
    /// <returns>List of tertiary cast entries for entities mentioned but not declared.</returns>
    public static IReadOnlyList<TertiaryCastEntry> Detect(
        string content,
        IReadOnlyList<DetectableEntity> allEntities,
        IReadOnlySet<string> declaredEntityIds,
        IReadOnlyDictionary<string, bool>? previousDecisions = null)
    {
        if (string.IsNullOrEmpty(content) || allEntities.Count == 0)
            return Array.Empty<TertiaryCastEntry>();

        // Build wiki entities list including aliases
        var wikiEntities = BuildWikiEntities(allEntities);

        // Find all entity mentions in the text
        var mentions = WikiLinkService.FindEntityMentions(content, wikiEntities);

        // Build lookup for entity metadata
        var entityMap = allEntities.ToDictionary(e => e.Id, StringComparer.Ordinal);

        // Filter: only entities not in declared cast, deduplicate by ID
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var results = new List<TertiaryCastEntry>();

        foreach (var mention in mentions)
        {
            if (declaredEntityIds.Contains(mention.EntityId)) continue;
            if (!seen.Add(mention.EntityId)) continue;
            if (!entityMap.TryGetValue(mention.EntityId, out var entity)) continue;

            var accepted = previousDecisions is not null && previousDecisions.TryGetValue(mention.EntityId, out var prev)
                ? prev
                : true; // Default to accepted

            results.Add(new TertiaryCastEntry
            {
                EntityId = entity.Id,
                Name = entity.Name,
                Kind = entity.Kind,
                MatchedAs = content[mention.Position..(mention.Position + mention.Length)],
                MatchStart = mention.Position,
                MatchEnd = mention.Position + mention.Length,
                Accepted = accepted,
            });
        }

        return results;
    }

    private static IReadOnlyList<WikiLinkEntity> BuildWikiEntities(IReadOnlyList<DetectableEntity> entities)
    {
        var wikiEntities = new List<WikiLinkEntity>();
        foreach (var entity in entities)
        {
            // Skip eras — they aren't cast members
            if (string.Equals(entity.Kind, "era", StringComparison.OrdinalIgnoreCase))
                continue;

            wikiEntities.Add(new WikiLinkEntity(entity.Id, entity.Name));

            if (entity.Aliases is null) continue;
            foreach (var alias in entity.Aliases)
            {
                if (alias.Length >= 3)
                    wikiEntities.Add(new WikiLinkEntity(entity.Id, alias));
            }
        }
        return wikiEntities;
    }
}
