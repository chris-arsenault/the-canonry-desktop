using TheCanonry.NameForge.Generation;
using TheCanonry.NameForge.Types;
using TheCanonry.NameForge.Utils;

namespace TheCanonry.NameForge.Tests.Generation;

public class GrammarExpanderTests
{
    private static NamingDomain CreateMinimalDomain() => new()
    {
        Id = "test-domain",
        AppliesTo = new AppliesTo { Kind = ["npc"] },
        Phonology = new PhonologyProfile
        {
            Consonants = ["t", "k", "r"],
            Vowels = ["a", "e"],
            SyllableTemplates = ["CV"],
            LengthRange = [2, 2]
        },
        Morphology = new MorphologyProfile
        {
            Structure = ["root"]
        },
        Style = new StyleRules()
    };

    [Fact]
    public void ExpandGrammar_LiteralProduction_ReturnsLiteral()
    {
        var grammar = new Grammar
        {
            Id = "test",
            Start = "name",
            Rules = new Dictionary<string, string[][]>
            {
                ["name"] = [["The", "Wanderer"]]
            },
            Capitalization = Capitalization.TitleWords
        };

        var rng = new SeededRng("literal-test");
        var (name, _) = GrammarExpander.ExpandGrammar(
            grammar, rng, [], [], [], [], []);

        Assert.Equal("The Wanderer", name);
    }

    [Fact]
    public void ExpandGrammar_SlotReference_PicksFromLexemeList()
    {
        var grammar = new Grammar
        {
            Id = "test",
            Start = "name",
            Rules = new Dictionary<string, string[][]>
            {
                ["name"] = [["slot:titles"]]
            }
        };
        var lexemes = new LexemeList[]
        {
            new() { Id = "titles", Entries = ["King", "Queen", "Lord"] }
        };

        var rng = new SeededRng("slot-test");
        var (name, _) = GrammarExpander.ExpandGrammar(
            grammar, rng, [], [], lexemes, [], []);

        var validTitles = new[] { "king", "queen", "lord" };
        Assert.Contains(name.ToLowerInvariant(), validTitles);
    }

    [Fact]
    public void ExpandGrammar_DomainReference_GeneratesPhonotacticName()
    {
        var grammar = new Grammar
        {
            Id = "test",
            Start = "name",
            Rules = new Dictionary<string, string[][]>
            {
                ["name"] = [["domain:test-domain"]]
            }
        };
        var domain = CreateMinimalDomain();

        var rng = new SeededRng("domain-test");
        var (name, usedMarkov) = GrammarExpander.ExpandGrammar(
            grammar, rng, [domain], [], [], [], []);

        Assert.NotEmpty(name);
        Assert.False(usedMarkov);
    }

    [Fact]
    public void ExpandGrammar_NestedRules_ExpandsRecursively()
    {
        var grammar = new Grammar
        {
            Id = "test",
            Start = "name",
            Rules = new Dictionary<string, string[][]>
            {
                ["name"] = [["title", "given"]],
                ["title"] = [["Lord"]],
                ["given"] = [["slot:names"]]
            },
            Capitalization = Capitalization.TitleWords
        };
        var lexemes = new LexemeList[]
        {
            new() { Id = "names", Entries = ["Thorn"] }
        };

        var rng = new SeededRng("nested-test");
        var (name, _) = GrammarExpander.ExpandGrammar(
            grammar, rng, [], [], lexemes, [], []);

        Assert.Equal("Lord Thorn", name);
    }

    [Fact]
    public void ExpandGrammar_CaretJoin_ConcatenatesWithoutSpace()
    {
        var grammar = new Grammar
        {
            Id = "test",
            Start = "name",
            Rules = new Dictionary<string, string[][]>
            {
                ["name"] = [["slot:names^'s"]]
            }
        };
        var lexemes = new LexemeList[]
        {
            new() { Id = "names", Entries = ["Storm"] }
        };

        var rng = new SeededRng("caret-test");
        var (name, _) = GrammarExpander.ExpandGrammar(
            grammar, rng, [], [], lexemes, [], []);

        Assert.Equal("Storm's", name);
    }

    [Fact]
    public void ExpandGrammar_DerivationModifier_AppliesTransform()
    {
        var grammar = new Grammar
        {
            Id = "test",
            Start = "name",
            Rules = new Dictionary<string, string[][]>
            {
                ["name"] = [["The", "slot:verbs~er"]]
            },
            Capitalization = Capitalization.TitleWords
        };
        var lexemes = new LexemeList[]
        {
            new() { Id = "verbs", Entries = ["hunt"] }
        };

        var rng = new SeededRng("deriv-test");
        var (name, _) = GrammarExpander.ExpandGrammar(
            grammar, rng, [], [], lexemes, [], []);

        Assert.Equal("The Hunter", name);
    }

    [Fact]
    public void ExpandGrammar_ContextReference_UsesContextValue()
    {
        var grammar = new Grammar
        {
            Id = "test",
            Start = "name",
            Rules = new Dictionary<string, string[][]>
            {
                ["name"] = [["context:parent^'s", "Heir"]]
            },
            Capitalization = Capitalization.TitleWords
        };
        var context = new Dictionary<string, string> { ["parent"] = "Gorban" };

        var rng = new SeededRng("context-test");
        var (name, _) = GrammarExpander.ExpandGrammar(
            grammar, rng, [], [], [], [], context);

        Assert.Equal("Gorban's Heir", name);
    }

    [Fact]
    public void ExpandGrammar_MissingContext_SkipsProduction()
    {
        var grammar = new Grammar
        {
            Id = "test",
            Start = "name",
            Rules = new Dictionary<string, string[][]>
            {
                ["name"] = [
                    ["context:parent^'s", "Heir"],  // Needs context
                    ["The", "Unknown"]                // Fallback
                ]
            },
            Capitalization = Capitalization.TitleWords
        };

        var rng = new SeededRng("missing-ctx-test");
        var (name, _) = GrammarExpander.ExpandGrammar(
            grammar, rng, [], [], [], [], []);

        // Should pick the fallback production since context:parent is missing
        Assert.Equal("The Unknown", name);
    }

    [Fact]
    public void ExpandGrammar_MarkovReference_SetsUsedMarkovFlag()
    {
        var grammar = new Grammar
        {
            Id = "test",
            Start = "name",
            Rules = new Dictionary<string, string[][]>
            {
                ["name"] = [["markov:norse"]]
            }
        };
        var markovModels = new Dictionary<string, MarkovModel>
        {
            ["norse"] = new MarkovModel
            {
                Order = 2,
                StartStates = new Dictionary<string, double> { ["th"] = 1.0 },
                Transitions = new Dictionary<string, Dictionary<string, double>>
                {
                    ["th"] = new() { ["o"] = 0.5, ["$"] = 0.5 },
                    ["ho"] = new() { ["r"] = 1.0 },
                    ["or"] = new() { ["$"] = 1.0 }
                }
            }
        };

        var rng = new SeededRng("markov-test");
        var (name, usedMarkov) = GrammarExpander.ExpandGrammar(
            grammar, rng, [], [], [], markovModels, []);

        Assert.True(usedMarkov);
        Assert.NotEmpty(name);
    }
}
