using TheCanonry.Illuminator.PrePrint;

namespace TheCanonry.Illuminator.Tests.PrePrint;

public sealed class PrePrintStatisticsTests
{
    // =========================================================================
    // CountWords
    // =========================================================================

    [Fact]
    public void CountWords_ReturnsZeroForNullOrEmpty()
    {
        Assert.Equal(0, PrePrintStatistics.CountWords(null));
        Assert.Equal(0, PrePrintStatistics.CountWords(""));
        Assert.Equal(0, PrePrintStatistics.CountWords("   "));
    }

    [Fact]
    public void CountWords_CountsCorrectlyForMultiWordStrings()
    {
        Assert.Equal(1, PrePrintStatistics.CountWords("hello"));
        Assert.Equal(3, PrePrintStatistics.CountWords("hello world foo"));
        Assert.Equal(3, PrePrintStatistics.CountWords("  extra   whitespace  here  "));
    }

    // =========================================================================
    // ComputeWordBreakdown
    // =========================================================================

    [Fact]
    public void ComputeWordBreakdown_SumsAcrossAllContentTypes()
    {
        var chronicles = new List<(string Content, string? HistorianNotes)>
        {
            ("one two three", "hist one"),
            ("four five",     null),
        };
        var entities = new List<(string? Description, string? HistorianNotes)>
        {
            ("a brave knight", "editor note here"),
            (null, null),
        };
        var eraNarratives = new List<string?> { "era text here" };
        var staticPages = new List<string?> { "page content words" };

        var breakdown = PrePrintStatistics.ComputeWordBreakdown(chronicles, entities, eraNarratives, staticPages);

        Assert.Equal(3 + 2, breakdown.ChronicleWords);                   // "one two three" + "four five"
        Assert.Equal(3,     breakdown.EntityWords);                        // "a brave knight"
        Assert.Equal(3,     breakdown.EraNarrativeWords);                  // "era text here"
        Assert.Equal(3,     breakdown.StaticPageWords);                    // "page content words"
        Assert.Equal(2 + 3, breakdown.HistorianWords);                    // "hist one" + "editor note here"
        Assert.Equal(5 + 3 + 3 + 3 + 2 + 3, breakdown.TotalWords);
    }

    [Fact]
    public void ComputeWordBreakdown_HandlesEmptyInputs()
    {
        var breakdown = PrePrintStatistics.ComputeWordBreakdown([], [], [], []);

        Assert.Equal(0, breakdown.TotalWords);
        Assert.Equal(0, breakdown.ChronicleWords);
        Assert.Equal(0, breakdown.EntityWords);
        Assert.Equal(0, breakdown.EraNarrativeWords);
        Assert.Equal(0, breakdown.StaticPageWords);
        Assert.Equal(0, breakdown.HistorianWords);
    }

    // =========================================================================
    // ComputeImageStats
    // =========================================================================

    [Fact]
    public void ComputeImageStats_GroupsByAspectAndType()
    {
        var images = new List<(string Aspect, string Type)>
        {
            ("portrait",  "entity"),
            ("portrait",  "entity"),
            ("landscape", "chronicle"),
            ("square",    "cover"),
        };

        var stats = PrePrintStatistics.ComputeImageStats(images);

        Assert.Equal(4, stats.TotalImages);
        Assert.Equal(2, stats.ByAspect["portrait"]);
        Assert.Equal(1, stats.ByAspect["landscape"]);
        Assert.Equal(1, stats.ByAspect["square"]);
        Assert.Equal(2, stats.ByType["entity"]);
        Assert.Equal(1, stats.ByType["chronicle"]);
        Assert.Equal(1, stats.ByType["cover"]);
    }

    [Fact]
    public void ComputeImageStats_ReturnsZeroTotalForEmptyList()
    {
        var stats = PrePrintStatistics.ComputeImageStats([]);
        Assert.Equal(0, stats.TotalImages);
        Assert.Empty(stats.ByAspect);
        Assert.Empty(stats.ByType);
    }

    // =========================================================================
    // ComputeCompletenessStats
    // =========================================================================

    [Fact]
    public void ComputeCompletenessStats_PassesThroughValuesCorrectly()
    {
        var stats = PrePrintStatistics.ComputeCompletenessStats(
            totalEntities: 100,
            entitiesWithDescription: 80,
            totalChronicles: 30,
            acceptedChronicles: 25,
            totalStaticPages: 10,
            publishedStaticPages: 8,
            totalEraNarratives: 5,
            completedEraNarratives: 4);

        Assert.Equal(100, stats.TotalEntities);
        Assert.Equal(80,  stats.EntitiesWithDescription);
        Assert.Equal(30,  stats.TotalChronicles);
        Assert.Equal(25,  stats.AcceptedChronicles);
        Assert.Equal(10,  stats.TotalStaticPages);
        Assert.Equal(8,   stats.PublishedStaticPages);
        Assert.Equal(5,   stats.TotalEraNarratives);
        Assert.Equal(4,   stats.CompletedEraNarratives);
    }

    // =========================================================================
    // ComputeAll
    // =========================================================================

    [Fact]
    public void ComputeAll_AggregatesAllStats()
    {
        var chronicles = new List<(string Content, string? HistorianNotes)>
        {
            ("word1 word2", null),
        };
        var images = new List<(string Aspect, string Type)>
        {
            ("portrait", "entity"),
        };

        var result = PrePrintStatistics.ComputeAll(
            chronicles: chronicles,
            entities: [],
            eraNarratives: [],
            staticPages: [],
            images: images,
            totalEntities: 10,
            entitiesWithDescription: 7,
            totalChronicles: 1,
            acceptedChronicles: 1,
            totalStaticPages: 0,
            publishedStaticPages: 0,
            totalEraNarratives: 0,
            completedEraNarratives: 0);

        Assert.Equal(2, result.WordCounts.ChronicleWords);
        Assert.Equal(1, result.Images.TotalImages);
        Assert.Equal(10, result.Completeness.TotalEntities);
    }
}
