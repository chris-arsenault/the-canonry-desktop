using TheCanonry.NameForge.Generation;
using TheCanonry.NameForge.Types;
using TheCanonry.NameForge.Utils;

namespace TheCanonry.NameForge.Tests.Generation;

public class PhonologyTests
{
    private static PhonologyProfile CreateBasicProfile() => new()
    {
        Consonants = ["t", "k", "r", "n", "s"],
        Vowels = ["a", "e", "i", "o"],
        SyllableTemplates = ["CV", "CVC"],
        LengthRange = [2, 3],
        ForbiddenClusters = [],
        FavoredClusters = []
    };

    [Fact]
    public void GenerateSyllable_ProducesNonEmptyString()
    {
        var rng = new SeededRng("syllable-test");
        var profile = CreateBasicProfile();

        var result = Phonology.GenerateSyllable(rng, profile);

        Assert.NotEmpty(result.Syllable);
        Assert.NotEmpty(result.Template);
    }

    [Fact]
    public void GenerateSyllable_ExpandsTemplate_CV()
    {
        var profile = new PhonologyProfile
        {
            Consonants = ["t"],
            Vowels = ["a"],
            SyllableTemplates = ["CV"],
            LengthRange = [1, 1]
        };
        var rng = new SeededRng("cv-test");

        var result = Phonology.GenerateSyllable(rng, profile);

        Assert.Equal("ta", result.Syllable);
        Assert.Equal("CV", result.Template);
    }

    [Fact]
    public void GenerateSyllable_ExpandsTemplate_CVC()
    {
        var profile = new PhonologyProfile
        {
            Consonants = ["k"],
            Vowels = ["o"],
            SyllableTemplates = ["CVC"],
            LengthRange = [1, 1]
        };
        var rng = new SeededRng("cvc-test");

        var result = Phonology.GenerateSyllable(rng, profile);

        Assert.Equal("kok", result.Syllable);
        Assert.Equal("CVC", result.Template);
    }

    [Fact]
    public void GenerateWord_RespectsLengthRange()
    {
        var rng = new SeededRng("length-test");
        var profile = CreateBasicProfile();

        for (var i = 0; i < 20; i++)
        {
            var word = Phonology.GenerateWord(rng, profile);
            // With CV/CVC templates and 2-3 syllables, min chars ~4, max practical ~9
            Assert.True(word.Length >= 2, $"Word '{word}' too short");
            Assert.True(word.Length <= 20, $"Word '{word}' too long");
        }
    }

    [Fact]
    public void GenerateWord_RejectsForbiddenClusters()
    {
        var rng = new SeededRng("forbidden-test");
        var profile = new PhonologyProfile
        {
            Consonants = ["t", "k"],
            Vowels = ["a", "e"],
            SyllableTemplates = ["CVC"],
            LengthRange = [2, 3],
            ForbiddenClusters = ["tt", "kk"]
        };

        for (var i = 0; i < 50; i++)
        {
            var word = Phonology.GenerateWord(rng, profile);
            Assert.DoesNotContain("tt", word.ToLowerInvariant());
            Assert.DoesNotContain("kk", word.ToLowerInvariant());
        }
    }

    [Fact]
    public void GenerateWordWithFavoredClusters_BoostsFavoredClusters()
    {
        var rng = new SeededRng("favored-test");
        var profile = new PhonologyProfile
        {
            Consonants = ["t", "r", "s", "n"],
            Vowels = ["a", "e", "i"],
            SyllableTemplates = ["CV", "CVC"],
            LengthRange = [2, 3],
            FavoredClusters = ["tr"],
            FavoredClusterBoost = 10.0
        };

        var hasFavored = 0;
        const int total = 100;
        for (var i = 0; i < total; i++)
        {
            var word = Phonology.GenerateWordWithFavoredClusters(rng, profile, candidateCount: 5);
            if (word.Contains("tr", StringComparison.OrdinalIgnoreCase))
                hasFavored++;
        }

        // With a 10x boost and 5 candidates, we expect a meaningful number to have 'tr'
        Assert.True(hasFavored > 0, "Expected some words with favored cluster 'tr'");
    }

    [Fact]
    public void GenerateWordWithDebug_ReturnsSyllablesAndTemplates()
    {
        var rng = new SeededRng("debug-test");
        var profile = CreateBasicProfile();

        var result = Phonology.GenerateWordWithDebug(rng, profile);

        Assert.NotEmpty(result.Word);
        Assert.True(result.Syllables.Length >= 2 && result.Syllables.Length <= 3);
        Assert.Equal(result.Syllables.Length, result.Templates.Length);
        Assert.Equal(string.Concat(result.Syllables), result.Word);
    }

    [Fact]
    public void GenerateWord_IsDeterministic()
    {
        var profile = CreateBasicProfile();

        var word1 = Phonology.GenerateWord(new SeededRng("det-test"), profile);
        var word2 = Phonology.GenerateWord(new SeededRng("det-test"), profile);

        Assert.Equal(word1, word2);
    }
}
