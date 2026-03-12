namespace TheCanonry.Engine.Engine;

using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

/// <summary>
/// Service for generating culturally appropriate names for entities.
/// Implemented by the name generation subsystem.
/// </summary>
public interface INameGenerationService
{
    /// <summary>
    /// Generate a name for an entity based on its kind, subtype, prominence, tags, and culture.
    /// </summary>
    /// <param name="kind">The entity kind (e.g., "faction", "npc").</param>
    /// <param name="subtype">The entity subtype (e.g., "merchant", "colony").</param>
    /// <param name="prominence">The entity's prominence level.</param>
    /// <param name="tags">The entity's tags.</param>
    /// <param name="culture">The entity's cultural affiliation.</param>
    /// <param name="context">Additional context for name generation.</param>
    /// <returns>A generated name appropriate for the entity.</returns>
    Task<string> GenerateAsync(
        EntityKind kind,
        string subtype,
        Prominence prominence,
        EntityTags tags,
        CultureId culture,
        string? context = null);
}
