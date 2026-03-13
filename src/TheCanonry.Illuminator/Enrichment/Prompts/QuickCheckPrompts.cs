using TheCanonry.Illuminator.Chronicle.PerspectiveSynthesis;
using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Prompts;

/// <summary>
/// Prompts for the quick check step — fast unanchored-reference scan.
/// </summary>
public static class QuickCheckPrompts
{
    public static string SystemPrompt =>
        """
        You are a continuity checker for fictional narratives. Your job is to find proper-noun-like phrases in the text that do NOT correspond to any known entity in the provided cast list or name bank. These are "unanchored references" — names, titles, place names, or entity references that the author may have invented during generation without grounding them in the world's established entities.

        You should NOT flag:
        - Common nouns, adjectives, or generic descriptions ("the old captain", "the western shore")
        - Titles used as common nouns ("the king", "the council", "the elders")
        - Known entities referenced by their full name, alias, partial name, or ID slug
        - Name bank names used for minor/invented characters (this is expected and correct)
        - Obvious metonyms or descriptive epithets for known entities ("the great beast" for a known creature)
        - Pronouns or demonstratives ("he", "she", "this one")

        You SHOULD flag:
        - Proper nouns that don't match any known name, alias, or ID slug
        - Place names not in the cast
        - Organization or faction names not in the cast
        - Named characters who are not in the cast or name bank
        - Abbreviated or partial names that don't clearly correspond to a known entity (e.g. "Aldric" when the known entity is "Aldric the Bold" — this is borderline but worth flagging as low confidence)

        Return ONLY valid JSON. No markdown wrapping.
        """;

    /// <summary>
    /// Build the quick check user prompt from chronicle data.
    /// </summary>
    public static string BuildUserPrompt(
        string chronicleContent,
        IReadOnlyList<ChronicleRoleAssignment> roleAssignments,
        IReadOnlyList<TertiaryCastEntry>? tertiaryCast = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? nameBank = null,
        IReadOnlyList<EntityDirective>? entityDirectives = null)
    {
        var sections = new List<string>();

        // Cast list with ID slugs
        if (roleAssignments.Count > 0)
        {
            var castLines = roleAssignments.Select(ra =>
            {
                var slugName = System.Text.RegularExpressions.Regex.Replace(ra.EntityId, @"-[a-f0-9]{4,}$", "")
                    .Replace('-', ' ');
                return $"- Name: \"{ra.EntityName}\" | ID slug: \"{ra.EntityId}\" (original: \"{slugName}\") | Kind: {ra.EntityKind} | Role: {ra.Role}";
            });
            sections.Add($"== KNOWN ENTITIES (cast) ==\n{string.Join("\n", castLines)}");
        }

        // Tertiary cast
        var acceptedTertiary = tertiaryCast?.Where(e => e.Accepted).ToList();
        if (acceptedTertiary is { Count: > 0 })
        {
            var tertiaryLines = acceptedTertiary.Select(e => $"- {e.Name} ({e.Kind})");
            sections.Add($"== TERTIARY CAST (detected mentions, not in declared cast \u2014 treat as known) ==\n{string.Join("\n", tertiaryLines)}");
        }

        // Name bank
        if (nameBank is { Count: > 0 })
        {
            var nbLines = nameBank.Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value)}");
            sections.Add($"== NAME BANK (expected invented names) ==\n{string.Join("\n", nbLines)}");
        }

        // Entity directives (extra name references)
        if (entityDirectives is { Count: > 0 })
        {
            var dLines = entityDirectives.Select(d => $"- {d.EntityName} ({d.EntityId}): {d.Directive}");
            sections.Add($"== ENTITY DIRECTIVES ==\n{string.Join("\n", dLines)}");
        }

        sections.Add($"== CHRONICLE TEXT ==\n{chronicleContent}");

        sections.Add("""
            == TASK ==
            Scan the chronicle text for proper-noun-like phrases that do NOT match any known entity name, alias, ID slug, or name bank entry. For each suspicious reference, provide the exact phrase, a brief snippet of surrounding context, your reasoning, and a confidence level.

            Return JSON:
            {
              "suspects": [
                {
                  "phrase": "the exact phrase as it appears",
                  "context": "...brief surrounding sentence or clause...",
                  "reasoning": "why this appears to be an unanchored reference",
                  "confidence": "high" | "medium" | "low"
                }
              ],
              "assessment": "clean" | "minor" | "flagged",
              "summary": "One sentence summary of findings"
            }
            """);

        return string.Join("\n\n", sections);
    }
}
