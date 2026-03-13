using TheCanonry.NameForge.Generation;
using TheCanonry.NameForge.Types;
using TheCanonry.NameForge.Utils;

namespace TheCanonry.NameForge;

/// <summary>
/// Public API for name generation.
/// Holds cultures and generates names using strategy group selection.
/// </summary>
public sealed class NameGenerator
{
    private readonly Dictionary<string, Culture> _cultures;

    public NameGenerator(IEnumerable<Culture> cultures)
    {
        _cultures = cultures.ToDictionary(c => c.Id);
    }

    /// <summary>
    /// Generate names using a culture's configuration.
    /// </summary>
    public static GenerateResult Generate(Culture culture, GenerateRequest request)
    {
        var profile = SelectProfile(culture.Profiles, request.ProfileId, request.Kind);
        if (profile == null)
        {
            var available = string.Join(", ", culture.Profiles.Select(p =>
            {
                var kinds = p.EntityKinds.Length > 0 ? $" ({string.Join(", ", p.EntityKinds)})" : "";
                var def = p.IsDefault ? " [default]" : "";
                return $"{p.Id}{kinds}{def}";
            }));
            throw new InvalidOperationException(
                $"No matching profile for entityKind \"{(string.IsNullOrEmpty(request.Kind) ? "(none)" : request.Kind)}\" " +
                $"in culture {culture.Id}. Available profiles: {(available.Length > 0 ? available : "none")}.");
        }

        var seed = !string.IsNullOrEmpty(request.Seed) ? request.Seed : $"gen-{DateTime.UtcNow.Ticks}";
        var rng = new SeededRng(seed);

        var lexemeLists = culture.LexemeLists.Values.ToArray();

        // Find matching strategy group
        var (matchingGroup, groupDebugInfo) = FindMatchingGroup(
            profile.StrategyGroups, request.Kind, request.Subtype, request.Prominence, request.Tags);

        var names = new List<string>();
        var debugInfos = new List<NameDebugInfo>();
        var strategyUsage = new Dictionary<string, int>
        {
            ["grammar"] = 0,
            ["phonotactic"] = 0,
            ["markov"] = 0,
            ["fallback"] = 0
        };

        for (var i = 0; i < request.Count; i++)
        {
            var result = GenerateSingleName(
                matchingGroup, rng, culture.Domains, culture.Grammars,
                lexemeLists, [], request.Context, i);

            names.Add(result.Name);
            strategyUsage[result.StrategyType] = strategyUsage.GetValueOrDefault(result.StrategyType) + 1;

            debugInfos.Add(new NameDebugInfo
            {
                GroupUsed = matchingGroup?.Name ?? "(fallback)",
                StrategyUsed = result.StrategyDesc,
                StrategyType = result.StrategyType,
                GrammarId = result.GrammarId,
                DomainId = result.DomainId,
                GroupMatching = groupDebugInfo
            });
        }

        return new GenerateResult
        {
            Names = [.. names],
            StrategyUsage = strategyUsage,
            DebugInfo = [.. debugInfos]
        };
    }

    /// <summary>
    /// Generate a single name. Convenience method for engine integration.
    /// </summary>
    public static string GenerateOne(
        Culture culture,
        string kind,
        string subtype,
        string prominence,
        string[] tags,
        Dictionary<string, string>? context = null)
    {
        var request = new GenerateRequest
        {
            CultureId = culture.Id,
            Kind = kind,
            Subtype = subtype,
            Prominence = prominence,
            Tags = tags,
            Context = context ?? [],
            Count = 1,
            Seed = $"{DateTime.UtcNow.Ticks}-{Guid.NewGuid()}"
        };

        var result = Generate(culture, request);
        return result.Names[0];
    }

    /// <summary>
    /// Get a culture by ID.
    /// </summary>
    public Culture? GetCulture(string cultureId)
    {
        return _cultures.GetValueOrDefault(cultureId);
    }

    // ========================================================================
    // Profile Selection
    // ========================================================================

    private static Profile? SelectProfile(Profile[] profiles, string profileId, string kind)
    {
        if (profiles.Length == 0) return null;

        // 1. If profileId specified, use that
        if (!string.IsNullOrEmpty(profileId))
            return Array.Find(profiles, p => p.Id == profileId);

        // 2. Find first profile with matching entityKind
        if (!string.IsNullOrEmpty(kind))
        {
            var kindMatch = Array.Find(profiles, p => p.EntityKinds.Contains(kind));
            if (kindMatch != null) return kindMatch;
        }

        // 3. Fall back to profile marked isDefault
        var defaultProfile = Array.Find(profiles, p => p.IsDefault);
        if (defaultProfile != null) return defaultProfile;

        // 4. Legacy: if only one profile with no entityKinds, use it
        if (profiles.Length == 1 && profiles[0].EntityKinds.Length == 0)
            return profiles[0];

        return null;
    }

    // ========================================================================
    // Strategy Group Selection
    // ========================================================================

    private static (StrategyGroup? Group, GroupMatchDebug[] DebugInfo) FindMatchingGroup(
        StrategyGroup[] groups,
        string kind,
        string subtype,
        string prominence,
        string[] tags)
    {
        var debugInfo = new List<GroupMatchDebug>();

        if (groups.Length == 0)
            return (null, []);

        var matchingGroups = new List<StrategyGroup>();
        foreach (var group in groups)
        {
            var (matched, reason) = CheckGroupMatch(group, kind, subtype, prominence, tags);
            debugInfo.Add(new GroupMatchDebug(
                string.IsNullOrEmpty(group.Name) ? "(unnamed)" : group.Name,
                matched, reason, group.Priority));
            if (matched)
                matchingGroups.Add(group);
        }

        if (matchingGroups.Count == 0)
        {
            // Fall back to first group
            if (debugInfo.Count > 0)
            {
                debugInfo[0] = debugInfo[0] with
                {
                    Reason = debugInfo[0].Reason + " [FALLBACK - used because no groups matched]"
                };
            }
            return (groups[0], [.. debugInfo]);
        }

        // Sort by priority (higher = selected first), break ties by preferring groups with conditions
        matchingGroups.Sort((a, b) =>
        {
            var priorityDiff = b.Priority - a.Priority;
            if (priorityDiff != 0) return priorityDiff;

            var aHasConds = HasMeaningfulConditions(a.Conditions);
            var bHasConds = HasMeaningfulConditions(b.Conditions);
            if (aHasConds && !bHasConds) return -1;
            if (bHasConds && !aHasConds) return 1;
            return 0;
        });

        var selected = matchingGroups[0];
        return (selected, [.. debugInfo]);
    }

    private static bool HasMeaningfulConditions(GroupConditions? conditions)
    {
        if (conditions == null) return false;
        return conditions.EntityKinds.Length > 0 ||
               conditions.Subtypes.Length > 0 ||
               conditions.Prominence.Length > 0 ||
               conditions.Tags.Length > 0;
    }

    private static (bool Matched, string Reason) CheckGroupMatch(
        StrategyGroup group,
        string kind,
        string subtype,
        string prominence,
        string[] tags)
    {
        var conditions = group.Conditions;
        if (conditions == null)
            return (true, "No conditions (matches all)");

        if (conditions.EntityKinds.Length > 0)
        {
            if (string.IsNullOrEmpty(kind))
                return (false, $"Requires entityKind in [{string.Join(", ", conditions.EntityKinds)}] but none provided");
            if (!conditions.EntityKinds.Contains(kind))
                return (false, $"entityKind \"{kind}\" not in [{string.Join(", ", conditions.EntityKinds)}]");
        }

        if (conditions.Subtypes.Length > 0)
        {
            if (string.IsNullOrEmpty(subtype))
                return (false, $"Requires subtype in [{string.Join(", ", conditions.Subtypes)}] but none provided");
            if (!conditions.Subtypes.Contains(subtype))
                return (false, $"subtype \"{subtype}\" not in [{string.Join(", ", conditions.Subtypes)}]");
        }

        if (conditions.Prominence.Length > 0)
        {
            if (string.IsNullOrEmpty(prominence))
                return (false, $"Requires prominence in [{string.Join(", ", conditions.Prominence)}] but none provided");
            if (!conditions.Prominence.Contains(prominence))
                return (false, $"prominence \"{prominence}\" not in [{string.Join(", ", conditions.Prominence)}]");
        }

        if (conditions.Tags.Length > 0)
        {
            if (conditions.TagMatchAll)
            {
                var missing = conditions.Tags.Where(t => !tags.Contains(t)).ToArray();
                if (missing.Length > 0)
                    return (false, $"Missing required tags: [{string.Join(", ", missing)}]");
            }
            else
            {
                if (!conditions.Tags.Any(t => tags.Contains(t)))
                    return (false, $"No matching tags from [{string.Join(", ", conditions.Tags)}]");
            }
        }

        return (true, "All conditions matched");
    }

    // ========================================================================
    // Single Name Generation
    // ========================================================================

    private sealed record SingleNameResult(
        string Name,
        string StrategyType,
        string StrategyDesc,
        string GrammarId,
        string DomainId);

    private static SingleNameResult GenerateSingleName(
        StrategyGroup? group,
        SeededRng rng,
        NamingDomain[] domains,
        Grammar[] grammars,
        LexemeList[] lexemeLists,
        Dictionary<string, MarkovModel> markovModels,
        Dictionary<string, string> userContext,
        int index)
    {
        if (group == null || group.Strategies.Length == 0)
        {
            return new SingleNameResult(
                GenerateFallbackName(lexemeLists, rng, index),
                "fallback", "No strategies in group", "", "");
        }

        var strategy = PickWeightedStrategy(group.Strategies, rng);

        if (strategy.Type == StrategyType.Grammar && !string.IsNullOrEmpty(strategy.GrammarId))
        {
            var grammar = Array.Find(grammars, g => g.Id == strategy.GrammarId);
            if (grammar != null)
            {
                var (name, usedMarkov) = GrammarExpander.ExpandGrammar(
                    grammar, rng, domains, grammars, lexemeLists, markovModels, userContext);
                return new SingleNameResult(
                    name,
                    usedMarkov ? "markov" : "grammar",
                    $"grammar:{strategy.GrammarId}",
                    strategy.GrammarId, "");
            }
            return new SingleNameResult(
                GenerateFallbackName(lexemeLists, rng, index),
                "fallback", $"grammar:{strategy.GrammarId} NOT FOUND",
                strategy.GrammarId, "");
        }

        if (strategy.Type == StrategyType.Phonotactic && !string.IsNullOrEmpty(strategy.DomainId))
        {
            var domain = Array.Find(domains, d => d.Id == strategy.DomainId);
            if (domain != null)
            {
                return new SingleNameResult(
                    PhonotacticPipeline.GeneratePhonotacticName(rng, domain),
                    "phonotactic", $"phonotactic:{strategy.DomainId}",
                    "", strategy.DomainId);
            }
            if (domains.Length > 0)
            {
                return new SingleNameResult(
                    PhonotacticPipeline.GeneratePhonotacticName(rng, domains[0]),
                    "phonotactic",
                    $"phonotactic:{strategy.DomainId} NOT FOUND, used {domains[0].Id}",
                    "", domains[0].Id);
            }
            return new SingleNameResult(
                GenerateFallbackName(lexemeLists, rng, index),
                "fallback",
                $"phonotactic:{strategy.DomainId} NOT FOUND, no domains available",
                "", strategy.DomainId);
        }

        return new SingleNameResult(
            GenerateFallbackName(lexemeLists, rng, index),
            "fallback", $"Unknown strategy type: {strategy.Type}",
            "", "");
    }

    private static Strategy PickWeightedStrategy(Strategy[] strategies, SeededRng rng)
    {
        if (strategies.Length == 1) return strategies[0];

        var totalWeight = strategies.Sum(s => s.Weight);
        if (totalWeight == 0) return strategies[0];

        var roll = rng.NextDouble() * totalWeight;
        foreach (var strategy in strategies)
        {
            roll -= strategy.Weight;
            if (roll <= 0) return strategy;
        }
        return strategies[^1];
    }

    private static string GenerateFallbackName(LexemeList[] lexemeLists, SeededRng rng, int index)
    {
        var nonEmpty = lexemeLists.Where(l => l.Entries.Length > 0).ToArray();
        if (nonEmpty.Length == 0)
            return $"Name-{index + 1}";

        var parts = new List<string>();
        var numParts = 1 + (int)(rng.NextDouble() * 2); // 1-2 parts

        for (var i = 0; i < numParts; i++)
        {
            var list = rng.PickRandom(nonEmpty);
            if (list.Entries.Length > 0)
                parts.Add(rng.PickRandom(list.Entries));
        }

        if (parts.Count == 0)
            return $"Name-{index + 1}";

        var name = string.Join("-", parts);
        return char.ToUpperInvariant(name[0]) + name[1..];
    }
}
