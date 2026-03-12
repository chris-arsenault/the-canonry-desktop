using TheCanonry.NameForge.Types;
using TheCanonry.NameForge.Utils;

namespace TheCanonry.NameForge.Generation;

/// <summary>
/// Token capitalization modifiers.
/// </summary>
internal enum TokenCapitalization
{
    Cap,
    Lower,
    Upper,
    Title
}

/// <summary>
/// Truncation modifier types.
/// </summary>
internal enum TruncationType
{
    ChopL,
    ChopR,
    Chop
}

/// <summary>
/// Parsed modifiers from a token pattern.
/// </summary>
internal sealed record ParsedModifiers(
    string Pattern,
    DerivationType? Derivation,
    TruncationType? Truncation,
    TokenCapitalization? Capitalization);

/// <summary>
/// Context for grammar expansion.
/// </summary>
internal sealed class GrammarExpansionContext
{
    public bool UsedMarkov { get; set; }
    public required Dictionary<string, string> UserContext { get; init; }
}

/// <summary>
/// Recursive grammar expansion engine.
/// Expands context-free grammars with slot:, domain:, markov:, and context: references.
/// </summary>
public static class GrammarExpander
{
    /// <summary>Unicode private use area markers for deferred capitalization.</summary>
    private const char CapMarkerStart = '\uE000';
    private const char CapMarkerSep = '\uE001';
    private const char CapMarkerEnd = '\uE002';

    private static readonly string[] PatternPrefixes = ["slot:", "domain:", "markov:", "context:"];

    private static readonly Dictionary<string, TokenCapitalization> CapitalizationModifiers = new()
    {
        ["cap"] = TokenCapitalization.Cap,
        ["lower"] = TokenCapitalization.Lower,
        ["upper"] = TokenCapitalization.Upper,
        ["title"] = TokenCapitalization.Title,
        ["c"] = TokenCapitalization.Cap,
        ["l"] = TokenCapitalization.Lower,
        ["u"] = TokenCapitalization.Upper,
        ["t"] = TokenCapitalization.Title
    };

    private static readonly HashSet<string> DerivationModifiers = ["er", "est", "ing", "ed", "poss", "comp"];
    private static readonly HashSet<string> TruncationModifiers = ["chopL", "chopR", "chop"];

    /// <summary>
    /// Expand a grammar to generate a name.
    /// </summary>
    internal static (string Name, bool UsedMarkov) ExpandGrammar(
        Grammar grammar,
        SeededRng rng,
        NamingDomain[] domains,
        Grammar[] grammars,
        LexemeList[] lexemeLists,
        Dictionary<string, MarkovModel> markovModels,
        Dictionary<string, string> userContext)
    {
        var startSymbol = string.IsNullOrEmpty(grammar.Start) ? "name" : grammar.Start;
        var expansionCtx = new GrammarExpansionContext
        {
            UsedMarkov = false,
            UserContext = userContext
        };

        var name = ExpandSymbol(startSymbol, grammar.Rules, rng, domains, lexemeLists, markovModels, expansionCtx, 0);

        // Apply grammar-level capitalization (unless mixed)
        if (grammar.Capitalization != Capitalization.Mixed)
            name = StringHelpers.ApplyCapitalization(name, grammar.Capitalization);

        // Apply deferred token-level capitalization markers
        name = ApplyTokenCapitalizationMarkers(name);

        return (name, expansionCtx.UsedMarkov);
    }

    private static string ExpandSymbol(
        string symbol,
        Dictionary<string, string[][]> rules,
        SeededRng rng,
        NamingDomain[] domains,
        LexemeList[] lexemeLists,
        Dictionary<string, MarkovModel> markovModels,
        GrammarExpansionContext ctx,
        int depth)
    {
        if (depth > 10) return symbol; // Prevent infinite recursion

        if (!rules.TryGetValue(symbol, out var productions) || productions.Length == 0)
            return ResolveToken(symbol, rules, rng, domains, lexemeLists, markovModels, ctx, depth);

        // Filter out productions that need context values we don't have
        var viableProductions = productions
            .Where(p => !ProductionNeedsMissingContext(p, ctx.UserContext))
            .ToArray();

        var candidateProductions = viableProductions.Length > 0 ? viableProductions : productions;
        var production = rng.PickRandom(candidateProductions);

        var parts = new List<string>();
        foreach (var token in production)
        {
            // Check for hyphen-separated compound references (e.g., "slot:core-domain:tech")
            // Only split if the token contains multiple pattern references separated by hyphens.
            // Do NOT split tokens like "slot:loc-words" where the hyphen is inside a single ID.
            if (token.Contains('-') && HasMultiplePatternRefs(token))
            {
                var subParts = token.Split('-');
                var resolvedParts = subParts.Select(part =>
                {
                    var trimmed = part.Trim();
                    if (rules.ContainsKey(trimmed))
                        return ExpandSymbol(trimmed, rules, rng, domains, lexemeLists, markovModels, ctx, depth + 1);
                    return ResolveToken(trimmed, rules, rng, domains, lexemeLists, markovModels, ctx, depth);
                });
                parts.Add(string.Join("-", resolvedParts));
                continue;
            }

            if (rules.ContainsKey(token))
            {
                parts.Add(ExpandSymbol(token, rules, rng, domains, lexemeLists, markovModels, ctx, depth + 1));
                continue;
            }

            parts.Add(ResolveToken(token, rules, rng, domains, lexemeLists, markovModels, ctx, depth));
        }

        return string.Join(" ", parts).Trim();
    }

    /// <summary>
    /// Check if a token contains multiple pattern references (e.g., "slot:core-domain:tech").
    /// A hyphen inside a single ID (e.g., "slot:loc-words") should NOT trigger splitting.
    /// </summary>
    private static bool HasMultiplePatternRefs(string token)
    {
        var count = 0;
        foreach (var prefix in PatternPrefixes)
        {
            var idx = 0;
            while ((idx = token.IndexOf(prefix, idx, StringComparison.Ordinal)) != -1)
            {
                count++;
                if (count >= 2) return true;
                idx += prefix.Length;
            }
        }
        return false;
    }

    private static bool ProductionNeedsMissingContext(string[] production, Dictionary<string, string> userContext)
    {
        foreach (var token in production)
        {
            if (!token.Contains("context:")) continue;

            var idx = token.IndexOf("context:", StringComparison.Ordinal);
            if (idx < 0) continue;

            // Extract the context key (handle modifiers like ~poss)
            var afterPrefix = token[(idx + 8)..];
            var key = "";
            foreach (var c in afterPrefix)
            {
                if (char.IsLetterOrDigit(c) || c == '_') key += c;
                else break;
            }

            if (key.Length > 0 && (!userContext.TryGetValue(key, out var val) || string.IsNullOrEmpty(val)))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Resolve a terminal token to its value.
    /// Supports ^ for joining patterns without spaces and ~ for modifiers.
    /// </summary>
    private static string ResolveToken(
        string token,
        Dictionary<string, string[][]> rules,
        SeededRng rng,
        NamingDomain[] domains,
        LexemeList[] lexemeLists,
        Dictionary<string, MarkovModel> markovModels,
        GrammarExpansionContext ctx,
        int depth)
    {
        _ = rules;
        _ = depth;

        if (!token.Contains('^'))
            return ResolveSimpleToken(token, rng, domains, lexemeLists, markovModels, ctx);

        // Split by ^ and resolve each segment
        var segments = token.Split('^');
        var results = new List<string>();

        foreach (var segment in segments)
        {
            if (string.IsNullOrEmpty(segment)) continue;
            results.Add(ResolveSegmentWithPrefix(segment, rng, domains, lexemeLists, markovModels, ctx));
        }

        return string.Concat(results);
    }

    private static string ResolveSegmentWithPrefix(
        string segment,
        SeededRng rng,
        NamingDomain[] domains,
        LexemeList[] lexemeLists,
        Dictionary<string, MarkovModel> markovModels,
        GrammarExpansionContext ctx)
    {
        var earliestIndex = -1;
        foreach (var prefix in PatternPrefixes)
        {
            var idx = segment.IndexOf(prefix, StringComparison.Ordinal);
            if (idx != -1 && (earliestIndex == -1 || idx < earliestIndex))
                earliestIndex = idx;
        }

        if (earliestIndex == -1) return segment; // Pure literal

        var literalPrefix = segment[..earliestIndex];
        var patternWithModifier = segment[earliestIndex..];

        return literalPrefix + ResolveSimpleToken(patternWithModifier, rng, domains, lexemeLists, markovModels, ctx);
    }

    private static string ResolveSimpleToken(
        string token,
        SeededRng rng,
        NamingDomain[] domains,
        LexemeList[] lexemeLists,
        Dictionary<string, MarkovModel> markovModels,
        GrammarExpansionContext ctx)
    {
        var modifiers = ParseModifiersFromToken(token);
        var resolved = ResolvePatternRef(modifiers.Pattern, token, rng, domains, lexemeLists, markovModels, ctx);
        if (resolved == null) return ""; // Context key missing

        var result = resolved;

        if (modifiers.Derivation is { } derivation)
            result = Derivation.ApplyDerivation(result, derivation);

        if (modifiers.Truncation is { } truncation)
            result = ApplyTruncation(result, truncation, rng);

        if (modifiers.Capitalization is { } cap)
            result = WrapCapitalizationMarker(result, cap);

        return result;
    }

    private static string? ResolvePatternRef(
        string pattern,
        string originalToken,
        SeededRng rng,
        NamingDomain[] domains,
        LexemeList[] lexemeLists,
        Dictionary<string, MarkovModel> markovModels,
        GrammarExpansionContext ctx)
    {
        if (pattern.StartsWith("slot:", StringComparison.Ordinal))
        {
            var listId = pattern[5..];
            var list = Array.Find(lexemeLists, l => l.Id == listId);
            return list is { Entries.Length: > 0 } ? rng.PickRandom(list.Entries) : listId;
        }

        if (pattern.StartsWith("domain:", StringComparison.Ordinal))
        {
            var domainId = pattern[7..];
            var domain = Array.Find(domains, d => d.Id == domainId);
            return domain != null
                ? PhonotacticPipeline.GeneratePhonotacticName(rng, domain)
                : domainId;
        }

        if (pattern.StartsWith("markov:", StringComparison.Ordinal))
        {
            var modelId = pattern[7..];
            if (markovModels.TryGetValue(modelId, out var model))
            {
                ctx.UsedMarkov = true;
                return MarkovGenerator.Generate(model, rng);
            }
            // Fallback to phonotactic if available
            if (domains.Length > 0)
                return PhonotacticPipeline.GeneratePhonotacticName(rng, domains[0]);
            return modelId;
        }

        if (pattern.StartsWith("context:", StringComparison.Ordinal))
        {
            var key = pattern[8..];
            return ctx.UserContext.TryGetValue(key, out var val) && !string.IsNullOrEmpty(val)
                ? val
                : null;
        }

        return originalToken; // Literal
    }

    private static ParsedModifiers ParseModifiersFromToken(string pattern)
    {
        DerivationType? derivation = null;
        TruncationType? truncation = null;
        TokenCapitalization? capitalization = null;
        var remaining = pattern;

        while (true)
        {
            var tildeIndex = remaining.LastIndexOf('~');
            if (tildeIndex == -1) break;

            var modifierStr = remaining[(tildeIndex + 1)..];

            if (capitalization == null && CapitalizationModifiers.TryGetValue(modifierStr, out var capMod))
            {
                capitalization = capMod;
                remaining = remaining[..tildeIndex];
            }
            else if (truncation == null && TruncationModifiers.Contains(modifierStr))
            {
                truncation = modifierStr switch
                {
                    "chopL" => TruncationType.ChopL,
                    "chopR" => TruncationType.ChopR,
                    "chop" => TruncationType.Chop,
                    _ => null
                };
                remaining = remaining[..tildeIndex];
            }
            else if (derivation == null && DerivationModifiers.Contains(modifierStr) &&
                     Derivation.TryParseModifier(modifierStr, out var derivType))
            {
                derivation = derivType;
                remaining = remaining[..tildeIndex];
            }
            else
            {
                break; // Unknown modifier - stop parsing
            }
        }

        return new ParsedModifiers(remaining, derivation, truncation, capitalization);
    }

    private static string ApplyTruncation(string str, TruncationType type, SeededRng rng)
    {
        if (str.Length <= 3) return str;

        var maxChop = Math.Min(3, str.Length - 2);
        var chopAmount = 1 + (int)(rng.NextDouble() * maxChop);
        chopAmount = Math.Min(chopAmount, maxChop); // Clamp

        return type switch
        {
            TruncationType.ChopL => str[chopAmount..],
            TruncationType.ChopR => str[..^chopAmount],
            TruncationType.Chop => rng.Chance(0.5) ? str[chopAmount..] : str[..^chopAmount],
            _ => str
        };
    }

    private static string WrapCapitalizationMarker(string text, TokenCapitalization modifier)
    {
        var modStr = modifier switch
        {
            TokenCapitalization.Cap => "cap",
            TokenCapitalization.Lower => "lower",
            TokenCapitalization.Upper => "upper",
            TokenCapitalization.Title => "title",
            _ => "cap"
        };
        return $"{CapMarkerStart}{modStr}{CapMarkerSep}{text}{CapMarkerEnd}";
    }

    internal static string ApplyTokenCapitalizationMarkers(string str)
    {
        var result = str;
        while (true)
        {
            var startIdx = result.IndexOf(CapMarkerStart);
            if (startIdx == -1) break;

            var sepIdx = result.IndexOf(CapMarkerSep, startIdx);
            if (sepIdx == -1) break;

            var endIdx = result.IndexOf(CapMarkerEnd, sepIdx);
            if (endIdx == -1) break;

            var modifier = result[(startIdx + 1)..sepIdx];
            var text = result[(sepIdx + 1)..endIdx];

            var transformed = modifier switch
            {
                "cap" => text.Length > 0
                    ? char.ToUpperInvariant(text[0]) + text[1..].ToLowerInvariant()
                    : text,
                "lower" => text.ToLowerInvariant(),
                "upper" => text.ToUpperInvariant(),
                "title" => StringHelpers.CapitalizeWords(text),
                _ => text
            };

            result = result[..startIdx] + transformed + result[(endIdx + 1)..];
        }
        return result;
    }
}
