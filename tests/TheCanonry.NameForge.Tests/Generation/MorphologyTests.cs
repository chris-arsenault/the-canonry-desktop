using TheCanonry.NameForge.Generation;
using TheCanonry.NameForge.Types;
using TheCanonry.NameForge.Utils;

namespace TheCanonry.NameForge.Tests.Generation;

public class MorphologyTests
{
    private static MorphologyProfile CreateBasicProfile() => new()
    {
        Prefixes = ["al", "un"],
        Suffixes = ["ith", "or"],
        Infixes = [],
        WordRoots = [],
        Honorifics = ["Lord", "Lady"],
        Structure = ["root", "prefix-root", "root-suffix", "prefix-root-suffix"],
        StructureWeights = [1, 1, 1, 1]
    };

    [Fact]
    public void ApplyMorphology_RootOnly_ReturnsRoot()
    {
        var rng = new SeededRng("root-only");
        var profile = new MorphologyProfile
        {
            Structure = ["root"],
            StructureWeights = [1]
        };

        var result = Morphology.ApplyMorphology(rng, "taka", profile, ["ta", "ka"]);

        Assert.Equal("taka", result.Result);
        Assert.Equal("root", result.Structure);
        Assert.Contains("root:taka", result.Parts);
    }

    [Fact]
    public void ApplyMorphology_PrefixRoot_PrependPrefix()
    {
        var rng = new SeededRng("prefix-test");
        var profile = new MorphologyProfile
        {
            Prefixes = ["al"],
            Structure = ["prefix-root"],
            StructureWeights = [1]
        };

        var result = Morphology.ApplyMorphology(rng, "taka", profile, ["ta", "ka"]);

        Assert.Equal("altaka", result.Result);
        Assert.Contains("prefix:al", result.Parts);
    }

    [Fact]
    public void ApplyMorphology_RootSuffix_AppendsSuffix()
    {
        var rng = new SeededRng("suffix-test");
        var profile = new MorphologyProfile
        {
            Suffixes = ["ith"],
            Structure = ["root-suffix"],
            StructureWeights = [1]
        };

        var result = Morphology.ApplyMorphology(rng, "taka", profile, ["ta", "ka"]);

        Assert.Equal("takaith", result.Result);
        Assert.Contains("suffix:ith", result.Parts);
    }

    [Fact]
    public void ApplyMorphology_TracksSyllables()
    {
        var rng = new SeededRng("syllable-track");
        var profile = new MorphologyProfile
        {
            Suffixes = ["or"],
            Structure = ["root-suffix"],
            StructureWeights = [1]
        };

        var result = Morphology.ApplyMorphology(rng, "taka", profile, ["ta", "ka"]);

        // Syllables should include root syllables plus suffix
        Assert.Equal(3, result.Syllables.Length);
        Assert.Equal("ta", result.Syllables[0]);
        Assert.Equal("ka", result.Syllables[1]);
        Assert.Equal("or", result.Syllables[2]);
    }

    [Fact]
    public void ApplyMorphologyBest_PrefersModerateLengthNames()
    {
        var rng = new SeededRng("best-test");
        var profile = CreateBasicProfile();

        var result = Morphology.ApplyMorphologyBest(
            rng, "taka", profile, candidateCount: 5, maxLength: 20,
            rootSyllables: ["ta", "ka"]);

        Assert.NotEmpty(result.Result);
        Assert.True(result.Result.Length <= 25,
            $"Expected moderate length, got {result.Result.Length}: '{result.Result}'");
    }

    [Fact]
    public void CanApplyMorphology_ReturnsFalseForRootOnly()
    {
        var profile = new MorphologyProfile
        {
            Structure = ["root"],
        };

        Assert.False(Morphology.CanApplyMorphology(profile));
    }

    [Fact]
    public void CanApplyMorphology_ReturnsTrueWithAffixes()
    {
        var profile = new MorphologyProfile
        {
            Suffixes = ["ith"],
            Structure = ["root", "root-suffix"],
        };

        Assert.True(Morphology.CanApplyMorphology(profile));
    }

    [Fact]
    public void GenerateCompound_ConcatenatesRoots()
    {
        Assert.Equal("darkwood", Morphology.GenerateCompound("dark", "wood"));
        Assert.Equal("dark-wood", Morphology.GenerateCompound("dark", "wood", "-"));
    }

    [Fact]
    public void ApplyHonorific_PrependsTitleWithSpace()
    {
        var rng = new SeededRng("honor-test");
        var profile = new MorphologyProfile
        {
            Honorifics = ["Lord"],
            Structure = ["root"],
        };

        var result = Morphology.ApplyHonorific(rng, "Taka", profile);

        Assert.Equal("Lord Taka", result);
    }

    [Fact]
    public void ApplyHonorific_ReturnsNameWhenNoHonorifics()
    {
        var rng = new SeededRng("no-honor");
        var profile = new MorphologyProfile
        {
            Structure = ["root"],
        };

        var result = Morphology.ApplyHonorific(rng, "Taka", profile);

        Assert.Equal("Taka", result);
    }
}

public class DerivationTests
{
    [Theory]
    [InlineData("hunt", "hunter")]
    [InlineData("forge", "forger")]
    [InlineData("slay", "slayer")]
    public void Agentive_CorrectForms(string input, string expected)
    {
        Assert.Equal(expected, Derivation.Agentive(input));
    }

    [Theory]
    [InlineData("deep", "deepest")]
    [InlineData("pale", "palest")]
    [InlineData("good", "best")]
    public void Superlative_CorrectForms(string input, string expected)
    {
        Assert.Equal(expected, Derivation.Superlative(input));
    }

    [Theory]
    [InlineData("burn", "burning")]
    [InlineData("forge", "forging")]
    [InlineData("die", "dying")]
    public void Gerund_CorrectForms(string input, string expected)
    {
        Assert.Equal(expected, Derivation.Gerund(input));
    }

    [Theory]
    [InlineData("curse", "cursed")]
    [InlineData("hunt", "hunted")]
    [InlineData("break", "broken")]
    [InlineData("cut", "cut")]
    public void Past_CorrectForms(string input, string expected)
    {
        Assert.Equal(expected, Derivation.Past(input));
    }

    [Theory]
    [InlineData("storm", "storm's")]
    [InlineData("darkness", "darkness'")]
    [InlineData("fox", "fox'")]
    public void Possessive_CorrectForms(string input, string expected)
    {
        Assert.Equal(expected, Derivation.Possessive(input));
    }

    [Theory]
    [InlineData("dark", "darker")]
    [InlineData("good", "better")]
    public void Comparative_CorrectForms(string input, string expected)
    {
        Assert.Equal(expected, Derivation.Comparative(input));
    }

    [Fact]
    public void ApplyDerivation_DispatchesCorrectly()
    {
        Assert.Equal("hunter", Derivation.ApplyDerivation("hunt", DerivationType.Er));
        Assert.Equal("deepest", Derivation.ApplyDerivation("deep", DerivationType.Est));
        Assert.Equal("burning", Derivation.ApplyDerivation("burn", DerivationType.Ing));
        Assert.Equal("cursed", Derivation.ApplyDerivation("curse", DerivationType.Ed));
        Assert.Equal("storm's", Derivation.ApplyDerivation("storm", DerivationType.Poss));
    }

    [Fact]
    public void TryParseModifier_ParsesValidTypes()
    {
        Assert.True(Derivation.TryParseModifier("er", out var type));
        Assert.Equal(DerivationType.Er, type);

        Assert.True(Derivation.TryParseModifier("poss", out var type2));
        Assert.Equal(DerivationType.Poss, type2);
    }

    [Fact]
    public void TryParseModifier_ReturnsFalseForInvalid()
    {
        Assert.False(Derivation.TryParseModifier("invalid", out _));
    }
}
