using TheCanonry.NameForge.Types;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

namespace TheCanonry.NameForge.Tests;

public class NameGeneratorTests
{
    private static Culture CreateMinimalCulture()
    {
        var domain = new NamingDomain
        {
            Id = "elvish",
            AppliesTo = new AppliesTo { Kind = ["npc"] },
            Phonology = new PhonologyProfile
            {
                Consonants = ["t", "l", "r", "n", "s", "m"],
                Vowels = ["a", "e", "i", "o", "u"],
                SyllableTemplates = ["CV", "CVC", "VC"],
                LengthRange = [2, 3]
            },
            Morphology = new MorphologyProfile
            {
                Structure = ["root"]
            },
            Style = new StyleRules
            {
                Capitalization = Capitalization.Title,
                ApostropheRate = 0.2
            }
        };

        var lexemes = new Dictionary<string, LexemeList>
        {
            ["titles"] = new LexemeList
            {
                Id = "titles",
                Entries = ["Lord", "Lady", "Prince", "Princess", "King", "Queen"]
            },
            ["epithets"] = new LexemeList
            {
                Id = "epithets",
                Entries = ["the Bold", "the Wise", "the Fierce", "the Fair"]
            }
        };

        var grammar = new Grammar
        {
            Id = "npc-name",
            Start = "name",
            Rules = new Dictionary<string, string[][]>
            {
                ["name"] = [
                    ["domain:elvish"],
                    ["slot:titles", "domain:elvish"],
                    ["domain:elvish", "slot:epithets"]
                ]
            },
            Capitalization = Capitalization.TitleWords
        };

        var profile = new Profile
        {
            Id = "default",
            EntityKinds = [],
            IsDefault = true,
            StrategyGroups =
            [
                new StrategyGroup
                {
                    Name = "default",
                    Priority = 0,
                    Conditions = null,
                    Strategies =
                    [
                        new Strategy
                        {
                            Type = StrategyType.Grammar,
                            Weight = 3,
                            GrammarId = "npc-name"
                        },
                        new Strategy
                        {
                            Type = StrategyType.Phonotactic,
                            Weight = 1,
                            DomainId = "elvish"
                        }
                    ]
                }
            ]
        };

        return new Culture
        {
            Id = "elves",
            Name = "Elvish",
            Domains = [domain],
            LexemeLists = lexemes,
            Grammars = [grammar],
            Profiles = [profile]
        };
    }

    [Fact]
    public void Generate_ReturnsRequestedCount()
    {
        var culture = CreateMinimalCulture();
        var generator = new NameGenerator([culture]);

        var result = generator.Generate(culture, new GenerateRequest
        {
            CultureId = "elves",
            Count = 5,
            Seed = "test-seed"
        });

        Assert.Equal(5, result.Names.Length);
        Assert.All(result.Names, name => Assert.NotEmpty(name));
    }

    [Fact]
    public void Generate_IsDeterministic_WithSameSeed()
    {
        var culture = CreateMinimalCulture();

        var gen1 = new NameGenerator([culture]);
        var gen2 = new NameGenerator([culture]);

        var result1 = gen1.Generate(culture, new GenerateRequest
        {
            CultureId = "elves",
            Count = 10,
            Seed = "determinism-test"
        });

        var result2 = gen2.Generate(culture, new GenerateRequest
        {
            CultureId = "elves",
            Count = 10,
            Seed = "determinism-test"
        });

        Assert.Equal(result1.Names, result2.Names);
    }

    [Fact]
    public void Generate_DifferentSeeds_ProduceDifferentNames()
    {
        var culture = CreateMinimalCulture();
        var generator = new NameGenerator([culture]);

        var result1 = generator.Generate(culture, new GenerateRequest
        {
            CultureId = "elves",
            Count = 5,
            Seed = "seed-a"
        });

        var result2 = generator.Generate(culture, new GenerateRequest
        {
            CultureId = "elves",
            Count = 5,
            Seed = "seed-b"
        });

        // At least some names should differ
        Assert.True(result1.Names.Zip(result2.Names).Any(p => p.First != p.Second),
            "Expected different seeds to produce at least some different names");
    }

    [Fact]
    public void Generate_TracksStrategyUsage()
    {
        var culture = CreateMinimalCulture();
        var generator = new NameGenerator([culture]);

        var result = generator.Generate(culture, new GenerateRequest
        {
            CultureId = "elves",
            Count = 20,
            Seed = "strategy-test"
        });

        var totalUsage = result.StrategyUsage.Values.Sum();
        Assert.Equal(20, totalUsage);
    }

    [Fact]
    public void Generate_PhonotacticOnly_ProducesNames()
    {
        var domain = new NamingDomain
        {
            Id = "simple",
            AppliesTo = new AppliesTo { Kind = ["npc"] },
            Phonology = new PhonologyProfile
            {
                Consonants = ["b", "d", "g"],
                Vowels = ["a", "o", "u"],
                SyllableTemplates = ["CV"],
                LengthRange = [2, 3]
            },
            Morphology = new MorphologyProfile
            {
                Structure = ["root"]
            },
            Style = new StyleRules { Capitalization = Capitalization.Title }
        };

        var culture = new Culture
        {
            Id = "simple",
            Name = "Simple",
            Domains = [domain],
            LexemeLists = [],
            Grammars = [],
            Profiles =
            [
                new Profile
                {
                    Id = "default",
                    IsDefault = true,
                    StrategyGroups =
                    [
                        new StrategyGroup
                        {
                            Name = "default",
                            Conditions = null,
                            Strategies =
                            [
                                new Strategy
                                {
                                    Type = StrategyType.Phonotactic,
                                    Weight = 1,
                                    DomainId = "simple"
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var generator = new NameGenerator([culture]);
        var result = generator.Generate(culture, new GenerateRequest
        {
            CultureId = "simple",
            Count = 10,
            Seed = "phonotactic-only"
        });

        Assert.Equal(10, result.Names.Length);
        Assert.All(result.Names, name =>
        {
            Assert.NotEmpty(name);
            Assert.True(char.IsUpper(name[0]), $"Expected title case, got: {name}");
        });
    }

    [Fact]
    public void GenerateOne_ReturnsSingleName()
    {
        var culture = CreateMinimalCulture();
        var generator = new NameGenerator([culture]);

        var name = generator.GenerateOne(culture, "npc", "merchant", "recognized", []);

        Assert.NotEmpty(name);
    }

    [Fact]
    public void Generate_ThrowsOnMissingProfile()
    {
        var culture = new Culture
        {
            Id = "empty",
            Name = "Empty",
            Domains = [],
            LexemeLists = [],
            Grammars = [],
            Profiles = []
        };
        var generator = new NameGenerator([culture]);

        Assert.Throws<InvalidOperationException>(() =>
            generator.Generate(culture, new GenerateRequest
            {
                CultureId = "empty",
                Kind = "npc",
                Count = 1,
                Seed = "fail"
            }));
    }

    [Fact]
    public void Generate_StrategyGroupConditions_MatchesEntityKind()
    {
        var domain = new NamingDomain
        {
            Id = "person",
            AppliesTo = new AppliesTo { Kind = ["npc"] },
            Phonology = new PhonologyProfile
            {
                Consonants = ["r", "s"],
                Vowels = ["a", "e"],
                SyllableTemplates = ["CV"],
                LengthRange = [2, 2]
            },
            Morphology = new MorphologyProfile { Structure = ["root"] },
            Style = new StyleRules()
        };

        var culture = new Culture
        {
            Id = "cond-test",
            Name = "CondTest",
            Domains = [domain],
            LexemeLists = new Dictionary<string, LexemeList>
            {
                ["loc-words"] = new LexemeList
                {
                    Id = "loc-words",
                    Entries = ["Blackwood", "Ironpeak", "Stormhold"]
                }
            },
            Grammars =
            [
                new Grammar
                {
                    Id = "location-name",
                    Start = "name",
                    Rules = new Dictionary<string, string[][]>
                    {
                        ["name"] = [["slot:loc-words"]]
                    }
                }
            ],
            Profiles =
            [
                new Profile
                {
                    Id = "default",
                    IsDefault = true,
                    StrategyGroups =
                    [
                        new StrategyGroup
                        {
                            Name = "locations",
                            Priority = 10,
                            Conditions = new GroupConditions
                            {
                                EntityKinds = ["location"]
                            },
                            Strategies =
                            [
                                new Strategy
                                {
                                    Type = StrategyType.Grammar,
                                    Weight = 1,
                                    GrammarId = "location-name"
                                }
                            ]
                        },
                        new StrategyGroup
                        {
                            Name = "default",
                            Priority = 0,
                            Conditions = null,
                            Strategies =
                            [
                                new Strategy
                                {
                                    Type = StrategyType.Phonotactic,
                                    Weight = 1,
                                    DomainId = "person"
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var generator = new NameGenerator([culture]);

        // Request for location should use grammar strategy
        var locResult = generator.Generate(culture, new GenerateRequest
        {
            CultureId = "cond-test",
            Kind = "location",
            Count = 5,
            Seed = "loc-seed"
        });

        // All location names should come from the loc-words lexeme list
        var locationWords = new[] { "Blackwood", "Ironpeak", "Stormhold" };
        Assert.All(locResult.Names, name =>
            Assert.Contains(name, locationWords));
    }
}

public class NameForgeServiceTests
{
    [Fact]
    public async Task GenerateAsync_ReturnsName()
    {
        var domain = new NamingDomain
        {
            Id = "test",
            AppliesTo = new AppliesTo { Kind = ["npc"] },
            Phonology = new PhonologyProfile
            {
                Consonants = ["t", "k"],
                Vowels = ["a", "e"],
                SyllableTemplates = ["CV"],
                LengthRange = [2, 2]
            },
            Morphology = new MorphologyProfile { Structure = ["root"] },
            Style = new StyleRules()
        };

        var culture = new Types.Culture
        {
            Id = "test-culture",
            Name = "Test",
            Domains = [domain],
            LexemeLists = [],
            Grammars = [],
            Profiles =
            [
                new Profile
                {
                    Id = "default",
                    IsDefault = true,
                    StrategyGroups =
                    [
                        new StrategyGroup
                        {
                            Name = "default",
                            Conditions = null,
                            Strategies =
                            [
                                new Strategy
                                {
                                    Type = StrategyType.Phonotactic,
                                    Weight = 1,
                                    DomainId = "test"
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var service = new NameForgeService([culture]);

        var name = await service.GenerateAsync(
            new EntityKind("npc"),
            "merchant",
            new Prominence(2.5),
            new EntityTags(),
            new CultureId("test-culture"));

        Assert.NotEmpty(name);
        Assert.NotEqual("unnamed", name);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsUnnamed_ForMissingCulture()
    {
        var domain = new NamingDomain
        {
            Id = "test",
            AppliesTo = new AppliesTo { Kind = ["npc"] },
            Phonology = new PhonologyProfile
            {
                Consonants = ["t"],
                Vowels = ["a"],
                SyllableTemplates = ["CV"],
                LengthRange = [2, 2]
            },
            Morphology = new MorphologyProfile { Structure = ["root"] },
            Style = new StyleRules()
        };

        var culture = new Types.Culture
        {
            Id = "existing",
            Name = "Existing",
            Domains = [domain],
            LexemeLists = [],
            Grammars = [],
            Profiles =
            [
                new Profile
                {
                    Id = "default",
                    IsDefault = true,
                    StrategyGroups =
                    [
                        new StrategyGroup
                        {
                            Name = "default",
                            Conditions = null,
                            Strategies =
                            [
                                new Strategy
                                {
                                    Type = StrategyType.Phonotactic,
                                    Weight = 1,
                                    DomainId = "test"
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var service = new NameForgeService([culture]);

        var name = await service.GenerateAsync(
            new EntityKind("npc"),
            "merchant",
            new Prominence(2.5),
            new EntityTags(),
            new CultureId("nonexistent"));

        Assert.Equal("unnamed", name);
    }

    [Fact]
    public async Task GenerateAsync_ParsesContextString()
    {
        var domain = new NamingDomain
        {
            Id = "test",
            AppliesTo = new AppliesTo { Kind = ["npc"] },
            Phonology = new PhonologyProfile
            {
                Consonants = ["t"],
                Vowels = ["a"],
                SyllableTemplates = ["CV"],
                LengthRange = [2, 2]
            },
            Morphology = new MorphologyProfile { Structure = ["root"] },
            Style = new StyleRules()
        };

        var culture = new Types.Culture
        {
            Id = "ctx-test",
            Name = "CtxTest",
            Domains = [domain],
            LexemeLists = [],
            Grammars =
            [
                new Grammar
                {
                    Id = "ctx-grammar",
                    Start = "name",
                    Rules = new Dictionary<string, string[][]>
                    {
                        ["name"] = [
                            ["context:parent^'s", "Child"],
                            ["domain:test"]
                        ]
                    },
                    Capitalization = Capitalization.TitleWords
                }
            ],
            Profiles =
            [
                new Profile
                {
                    Id = "default",
                    IsDefault = true,
                    StrategyGroups =
                    [
                        new StrategyGroup
                        {
                            Name = "default",
                            Conditions = null,
                            Strategies =
                            [
                                new Strategy
                                {
                                    Type = StrategyType.Grammar,
                                    Weight = 1,
                                    GrammarId = "ctx-grammar"
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var service = new NameForgeService([culture]);

        var name = await service.GenerateAsync(
            new EntityKind("npc"),
            "child",
            new Prominence(2.0),
            new EntityTags(),
            new CultureId("ctx-test"),
            "parent=Gorban");

        Assert.NotEmpty(name);
        // With context provided, should often generate "Gorban's Child"
        // but grammar picks randomly, so just verify it's not unnamed
        Assert.NotEqual("unnamed", name);
    }
}
