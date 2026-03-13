using TheCanonry.Engine.Engine;
using TheCanonry.NameForge.Types;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

namespace TheCanonry.NameForge;

/// <summary>
/// Implements INameGenerationService using the NameGenerator engine.
/// Translates between engine schema types and name-forge internal types.
/// </summary>
public sealed class NameForgeService : INameGenerationService
{
    private readonly NameGenerator _generator;

    /// <summary>
    /// Create a NameForgeService from a set of name-forge cultures.
    /// </summary>
    public NameForgeService(IEnumerable<Culture> cultures)
    {
        _generator = new NameGenerator(cultures);
    }

    /// <inheritdoc />
    public Task<string> GenerateAsync(
        EntityKind kind,
        string subtype,
        Prominence prominence,
        EntityTags tags,
        CultureId culture,
        string? context = null)
    {
        var nfCulture = _generator.GetCulture(culture.Value);
        if (nfCulture == null)
            return Task.FromResult("unnamed");

        try
        {
            // Convert EntityTags keys to string array for condition matching
            var tagKeys = tags.All.Keys.ToArray();

            // Convert Prominence to a label string for condition matching
            var prominenceLabel = prominence.Label.ToLowerInvariant();

            // Parse context string into a dictionary if provided
            var contextDict = ParseContext(context);

            var name = NameGenerator.GenerateOne(
                nfCulture,
                kind.Value,
                subtype,
                prominenceLabel,
                tagKeys,
                contextDict);

            return Task.FromResult(name);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return Task.FromResult("unnamed");
        }
    }

    private static Dictionary<string, string> ParseContext(string? context)
    {
        if (string.IsNullOrEmpty(context))
            return [];

        // Simple key=value;key=value format
        var dict = new Dictionary<string, string>();
        foreach (var pair in context.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var eqIdx = pair.IndexOf('=');
            if (eqIdx > 0)
                dict[pair[..eqIdx].Trim()] = pair[(eqIdx + 1)..].Trim();
        }
        return dict;
    }
}
