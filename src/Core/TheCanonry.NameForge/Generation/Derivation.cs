namespace TheCanonry.NameForge.Generation;

/// <summary>
/// Derivation types for morphological transformations.
/// </summary>
public enum DerivationType
{
    /// <summary>Agentive: one who does X (hunt -> hunter).</summary>
    Er,
    /// <summary>Superlative: most X (deep -> deepest).</summary>
    Est,
    /// <summary>Comparative: more X (dark -> darker).</summary>
    Comp,
    /// <summary>Gerund/present participle: X-ing (burn -> burning).</summary>
    Ing,
    /// <summary>Past/passive: X-ed (curse -> cursed).</summary>
    Ed,
    /// <summary>Possessive: X's (storm -> storm's).</summary>
    Poss
}

/// <summary>
/// Rule-based morphological derivations for English words.
/// Transforms base words into derived forms.
/// </summary>
public static class Derivation
{
    private static readonly HashSet<char> Vowels = ['a', 'e', 'i', 'o', 'u'];

    #region Irregular forms

    private static readonly Dictionary<string, string> IrregularPast = new(StringComparer.OrdinalIgnoreCase)
    {
        ["break"] = "broken", ["speak"] = "spoken", ["wake"] = "woken",
        ["take"] = "taken", ["shake"] = "shaken", ["forsake"] = "forsaken",
        ["sing"] = "sung", ["ring"] = "rung", ["drink"] = "drunk",
        ["sink"] = "sunk", ["shrink"] = "shrunk", ["stink"] = "stunk",
        ["swim"] = "swum", ["begin"] = "begun",
        ["bring"] = "brought", ["buy"] = "bought", ["catch"] = "caught",
        ["fight"] = "fought", ["seek"] = "sought", ["teach"] = "taught",
        ["think"] = "thought",
        ["bear"] = "borne", ["bite"] = "bitten", ["blow"] = "blown",
        ["choose"] = "chosen", ["draw"] = "drawn", ["drive"] = "driven",
        ["eat"] = "eaten", ["fall"] = "fallen", ["fly"] = "flown",
        ["freeze"] = "frozen", ["give"] = "given", ["grow"] = "grown",
        ["hide"] = "hidden", ["know"] = "known", ["ride"] = "ridden",
        ["rise"] = "risen", ["see"] = "seen", ["slay"] = "slain",
        ["smite"] = "smitten", ["steal"] = "stolen", ["strike"] = "stricken",
        ["swear"] = "sworn", ["tear"] = "torn", ["throw"] = "thrown",
        ["wear"] = "worn", ["weave"] = "woven", ["write"] = "written",
        ["cut"] = "cut", ["hit"] = "hit", ["hurt"] = "hurt",
        ["put"] = "put", ["set"] = "set", ["shed"] = "shed",
        ["shut"] = "shut", ["split"] = "split", ["spread"] = "spread",
        ["thrust"] = "thrust",
        ["bend"] = "bent", ["build"] = "built", ["burn"] = "burnt",
        ["deal"] = "dealt", ["dream"] = "dreamt", ["dwell"] = "dwelt",
        ["feel"] = "felt", ["keep"] = "kept", ["kneel"] = "knelt",
        ["lean"] = "leant", ["leap"] = "leapt", ["learn"] = "learnt",
        ["leave"] = "left", ["lend"] = "lent", ["lose"] = "lost",
        ["mean"] = "meant", ["meet"] = "met", ["send"] = "sent",
        ["sleep"] = "slept", ["smell"] = "smelt", ["spell"] = "spelt",
        ["spend"] = "spent", ["spill"] = "spilt", ["sweep"] = "swept",
        ["weep"] = "wept",
        ["be"] = "been", ["do"] = "done", ["go"] = "gone",
        ["have"] = "had", ["make"] = "made", ["say"] = "said",
        ["hold"] = "held", ["stand"] = "stood", ["find"] = "found",
        ["bind"] = "bound", ["grind"] = "ground", ["wind"] = "wound",
        ["hang"] = "hung", ["dig"] = "dug", ["stick"] = "stuck",
        ["spin"] = "spun", ["win"] = "won", ["cling"] = "clung",
        ["fling"] = "flung", ["sling"] = "slung", ["sting"] = "stung",
        ["swing"] = "swung", ["wring"] = "wrung"
    };

    private static readonly Dictionary<string, string> IrregularAgentive = new(StringComparer.OrdinalIgnoreCase)
    {
        ["lie"] = "liar",
        ["die"] = "death-dealer",
        ["sing"] = "singer",
        ["slay"] = "slayer"
    };

    private static readonly Dictionary<string, string> IrregularSuperlative = new(StringComparer.OrdinalIgnoreCase)
    {
        ["good"] = "best", ["bad"] = "worst", ["far"] = "farthest",
        ["little"] = "least", ["much"] = "most", ["many"] = "most"
    };

    private static readonly Dictionary<string, string> IrregularComparative = new(StringComparer.OrdinalIgnoreCase)
    {
        ["good"] = "better", ["bad"] = "worse", ["far"] = "farther",
        ["little"] = "less", ["much"] = "more", ["many"] = "more"
    };

    #endregion

    /// <summary>
    /// Apply agentive: one who does X (hunt -> hunter, forge -> forger).
    /// </summary>
    public static string Agentive(string word)
    {
        var lower = word.ToLowerInvariant();
        if (IrregularAgentive.TryGetValue(lower, out var irregular))
            return MatchCase(word, irregular);
        if (lower.EndsWith('e'))
            return word + "r";
        if (lower.EndsWith('y') && word.Length > 1 && IsConsonant(lower[^2]))
            return word[..^1] + "ier";
        if (ShouldDoubleConsonant(word))
            return word + word[^1] + "er";
        return word + "er";
    }

    /// <summary>
    /// Apply superlative: most X (deep -> deepest, grim -> grimmest).
    /// </summary>
    public static string Superlative(string word)
    {
        var lower = word.ToLowerInvariant();
        if (IrregularSuperlative.TryGetValue(lower, out var irregular))
            return MatchCase(word, irregular);
        if (lower.EndsWith('e'))
            return word + "st";
        if (lower.EndsWith('y') && word.Length > 1 && IsConsonant(lower[^2]))
            return word[..^1] + "iest";
        if (ShouldDoubleConsonant(word))
            return word + word[^1] + "est";
        return word + "est";
    }

    /// <summary>
    /// Apply comparative: more X (dark -> darker).
    /// </summary>
    public static string Comparative(string word)
    {
        var lower = word.ToLowerInvariant();
        if (IrregularComparative.TryGetValue(lower, out var irregular))
            return MatchCase(word, irregular);
        if (lower.EndsWith('e'))
            return word + "r";
        if (lower.EndsWith('y') && word.Length > 1 && IsConsonant(lower[^2]))
            return word[..^1] + "ier";
        if (ShouldDoubleConsonant(word))
            return word + word[^1] + "er";
        return word + "er";
    }

    /// <summary>
    /// Apply gerund/present participle: X-ing (burn -> burning, forge -> forging).
    /// </summary>
    public static string Gerund(string word)
    {
        var lower = word.ToLowerInvariant();
        if (lower.EndsWith("ie"))
            return word[..^2] + "ying";
        if (lower.EndsWith('e') && !lower.EndsWith("ee"))
            return word[..^1] + "ing";
        if (ShouldDoubleConsonant(word))
            return word + word[^1] + "ing";
        return word + "ing";
    }

    /// <summary>
    /// Apply past/passive: X-ed (curse -> cursed, hunt -> hunted).
    /// </summary>
    public static string Past(string word)
    {
        var lower = word.ToLowerInvariant();
        if (IrregularPast.TryGetValue(lower, out var irregular))
            return MatchCase(word, irregular);
        if (lower.EndsWith('e'))
            return word + "d";
        if (lower.EndsWith('y') && word.Length > 1 && IsConsonant(lower[^2]))
            return word[..^1] + "ied";
        if (ShouldDoubleConsonant(word))
            return word + word[^1] + "ed";
        return word + "ed";
    }

    /// <summary>
    /// Apply possessive: X's (storm -> storm's, darkness -> darkness').
    /// </summary>
    public static string Possessive(string word)
    {
        var lower = word.ToLowerInvariant();
        if (lower.EndsWith('s') || lower.EndsWith('x') || lower.EndsWith('z'))
            return word + "'";
        return word + "'s";
    }

    /// <summary>
    /// Apply a derivation by type.
    /// </summary>
    public static string ApplyDerivation(string word, DerivationType type) => type switch
    {
        DerivationType.Er => Agentive(word),
        DerivationType.Est => Superlative(word),
        DerivationType.Comp => Comparative(word),
        DerivationType.Ing => Gerund(word),
        DerivationType.Ed => Past(word),
        DerivationType.Poss => Possessive(word),
        _ => word
    };

    /// <summary>
    /// Try to parse a derivation type from its string modifier name.
    /// </summary>
    public static bool TryParseModifier(string modifier, out DerivationType type)
    {
        type = modifier switch
        {
            "er" => DerivationType.Er,
            "est" => DerivationType.Est,
            "comp" => DerivationType.Comp,
            "ing" => DerivationType.Ing,
            "ed" => DerivationType.Ed,
            "poss" => DerivationType.Poss,
            _ => (DerivationType)(-1)
        };
        return (int)type >= 0;
    }

    private static bool IsVowel(char c) => Vowels.Contains(char.ToLowerInvariant(c));
    private static bool IsConsonant(char c) => char.IsLetter(c) && !IsVowel(c);

    private static bool ShouldDoubleConsonant(string word)
    {
        if (word.Length < 3) return false;

        var last = char.ToLowerInvariant(word[^1]);
        var secondLast = char.ToLowerInvariant(word[^2]);
        var thirdLast = char.ToLowerInvariant(word[^3]);

        if (last is 'w' or 'x' or 'y') return false;
        if (!IsConsonant(last)) return false;
        if (!IsVowel(secondLast)) return false;
        if (!IsConsonant(thirdLast)) return false;

        if (word.Length <= 4) return true;

        string[] stressedEndings = ["mit", "gin", "fer", "cur", "pel", "quet"];
        var lower = word.ToLowerInvariant();
        foreach (var ending in stressedEndings)
        {
            if (lower.EndsWith(ending, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static string MatchCase(string source, string result)
    {
        if (source == source.ToUpperInvariant())
            return result.ToUpperInvariant();
        if (char.IsUpper(source[0]))
            return char.ToUpperInvariant(result[0]) + result[1..];
        return result;
    }
}
