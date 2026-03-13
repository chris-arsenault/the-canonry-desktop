using TheCanonry.Illuminator.BulkOps;

namespace TheCanonry.Illuminator.Tests.BulkOps;

// ─── BulkOperationRunner Tests ────────────────────────────────────────────────

public sealed class BulkOperationRunnerTests
{
    private sealed record Item(string Id, string Name);

    // -------------------------------------------------------------------------
    // Test 1: Processes all items and reports progress after each
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RunAsync_ProcessesAllItems_AndReportsProgress()
    {
        var items = new[]
        {
            new Item("id-1", "Alpha"),
            new Item("id-2", "Beta"),
            new Item("id-3", "Gamma"),
        };

        var progressReports = new List<BulkOperationProgress>();
        var progress = new Progress<BulkOperationProgress>(p => progressReports.Add(p));

        var processed = new List<string>();
        var result = await BulkOperationRunner.RunAsync(
            items,
            item => item.Id,
            item => item.Name,
            (item, _) =>
            {
                processed.Add(item.Id);
                return Task.CompletedTask;
            },
            progress);

        Assert.Equal(3, processed.Count);
        Assert.Equal(3, result.Processed);
        Assert.Equal(0, result.Failed);
        Assert.Equal(BulkOperationStatus.Complete, result.Status);
        Assert.Empty(result.Failures);

        // Allow async progress callbacks to flush
        await Task.Delay(10);
        Assert.NotEmpty(progressReports);
    }

    // -------------------------------------------------------------------------
    // Test 2: Handles item failures gracefully (continues processing)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RunAsync_HandlesItemFailures_ContinuesProcessing()
    {
        var items = new[]
        {
            new Item("id-1", "Alpha"),
            new Item("id-2", "Failing"),
            new Item("id-3", "Gamma"),
        };

        var processed = new List<string>();
        var result = await BulkOperationRunner.RunAsync(
            items,
            item => item.Id,
            item => item.Name,
            (item, _) =>
            {
                if (item.Name == "Failing")
                    throw new InvalidOperationException("Test failure");
                processed.Add(item.Id);
                return Task.CompletedTask;
            });

        // Alpha and Gamma should succeed; Failing should be recorded
        Assert.Contains("id-1", processed);
        Assert.Contains("id-3", processed);
        Assert.Equal(1, result.Failed);
        Assert.Single(result.Failures);
        Assert.Equal("id-2", result.Failures[0].ItemId);
        Assert.Equal("Failing", result.Failures[0].ItemName);
        Assert.Contains("Test failure", result.Failures[0].Error);
    }

    // -------------------------------------------------------------------------
    // Test 3: Respects cancellation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RunAsync_RespectsCancellation()
    {
        var items = Enumerable.Range(1, 10)
            .Select(i => new Item($"id-{i}", $"Item{i}"))
            .ToList();

        using var cts = new CancellationTokenSource();

        var processed = new List<string>();
        var result = await BulkOperationRunner.RunAsync(
            items,
            item => item.Id,
            item => item.Name,
            async (item, _) =>
            {
                processed.Add(item.Id);
                if (processed.Count == 3)
                    await cts.CancelAsync();
            },
            ct: cts.Token);

        Assert.Equal(BulkOperationStatus.Cancelled, result.Status);
        // Should have processed 3 before cancellation stopped the loop
        Assert.True(result.Processed <= items.Count);
    }

    // -------------------------------------------------------------------------
    // Test 4: Reports Complete when all items succeed
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RunAsync_AllSucceed_ReportsCompleteStatus()
    {
        var items = new[]
        {
            new Item("id-1", "Alpha"),
            new Item("id-2", "Beta"),
        };

        var result = await BulkOperationRunner.RunAsync(
            items,
            item => item.Id,
            item => item.Name,
            (_, _) => Task.CompletedTask);

        Assert.Equal(BulkOperationStatus.Complete, result.Status);
        Assert.Equal(2, result.Total);
        Assert.Equal(2, result.Processed);
        Assert.Equal(0, result.Failed);
    }

    // -------------------------------------------------------------------------
    // Test 5: Reports correct total count
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RunAsync_TotalMatchesInputCount()
    {
        var items = Enumerable.Range(1, 7)
            .Select(i => new Item($"id-{i}", $"Item{i}"))
            .ToList();

        var result = await BulkOperationRunner.RunAsync(
            items,
            i => i.Id,
            i => i.Name,
            (_, _) => Task.CompletedTask);

        Assert.Equal(7, result.Total);
    }
}

// ─── BulkHistorian Tests ──────────────────────────────────────────────────────

public sealed class BulkHistorianTests
{
    // -------------------------------------------------------------------------
    // Test 6: AssignReviewTones cycles through tones in order
    // -------------------------------------------------------------------------

    [Fact]
    public void AssignReviewTones_CyclesThroughTones()
    {
        var entities = Enumerable.Range(0, 7)
            .Select(i => ($"e{i}", "npc", "default", (double)i))
            .ToList();

        var assignments = BulkHistorian.AssignReviewTones(entities);

        Assert.Equal(7, assignments.Count);
        // First 5 should match the cycle; 6th wraps back to index 0
        for (var i = 0; i < 7; i++)
        {
            var expected = BulkHistorian.ReviewToneCycle[i % BulkHistorian.ReviewToneCycle.Length];
            Assert.Equal(expected, assignments[i].Tone);
        }
    }

    // -------------------------------------------------------------------------
    // Test 7: AssignReviewTones sorts by kind then culture then prominence
    // -------------------------------------------------------------------------

    [Fact]
    public void AssignReviewTones_SortsByKindCultureProminence()
    {
        var entities = new[]
        {
            ("e3", "npc",      "zethar", 2.0),
            ("e1", "artifact", "zethar", 1.0),
            ("e2", "npc",      "amara",  1.0),
            ("e4", "npc",      "amara",  3.0),
        };

        var assignments = BulkHistorian.AssignReviewTones(entities);

        // Sort order: artifact/zethar/1 → npc/amara/1 → npc/amara/3 → npc/zethar/2
        Assert.Equal("e1", assignments[0].EntityId);
        Assert.Equal("e2", assignments[1].EntityId);
        Assert.Equal("e4", assignments[2].EntityId);
        Assert.Equal("e3", assignments[3].EntityId);
    }

    // -------------------------------------------------------------------------
    // Test 8: AssignReviewTones returns empty for empty input
    // -------------------------------------------------------------------------

    [Fact]
    public void AssignReviewTones_EmptyInput_ReturnsEmpty()
    {
        var result = BulkHistorian.AssignReviewTones(
            Array.Empty<(string, string, string, double)>());
        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // Test 9: ReviewToneCycle has 5 elements
    // -------------------------------------------------------------------------

    [Fact]
    public void ReviewToneCycle_HasFiveElements()
    {
        Assert.Equal(5, BulkHistorian.ReviewToneCycle.Length);
        Assert.Contains("witty", BulkHistorian.ReviewToneCycle);
        Assert.Contains("cantankerous", BulkHistorian.ReviewToneCycle);
    }
}

// ─── BulkBackport Tests ───────────────────────────────────────────────────────

public sealed class BulkBackportTests
{
    // -------------------------------------------------------------------------
    // Test 10: MaxBatchSize constant is 8
    // -------------------------------------------------------------------------

    [Fact]
    public void MaxBatchSize_IsEight()
    {
        Assert.Equal(8, BulkBackport.MaxBatchSize);
    }

    // -------------------------------------------------------------------------
    // Test 11: PrepareBatches splits into batches of 8, sorted by chronicle count desc
    // -------------------------------------------------------------------------

    [Fact]
    public void PrepareBatches_SortsByChronicleCountDesc_AndBatches()
    {
        var entities = Enumerable.Range(1, 20)
            .Select(i => new BulkBackportEntitySummary($"e{i}", $"Entity {i}", "npc", "default", i))
            .ToList();

        var batches = BulkBackport.PrepareBatches(entities);

        // 20 items / 8 = 3 batches (8, 8, 4)
        Assert.Equal(3, batches.Count);
        Assert.Equal(8, batches[0].Count);
        Assert.Equal(8, batches[1].Count);
        Assert.Equal(4, batches[2].Count);

        // First batch should have the highest chronicle counts (20, 19, ..., 13)
        Assert.Equal(20, batches[0][0].ChronicleCount);
    }

    // -------------------------------------------------------------------------
    // Test 12: PrepareBatches with empty input returns empty
    // -------------------------------------------------------------------------

    [Fact]
    public void PrepareBatches_EmptyInput_ReturnsEmpty()
    {
        var batches = BulkBackport.PrepareBatches(Array.Empty<BulkBackportEntitySummary>());
        Assert.Empty(batches);
    }
}

// ─── BulkEraNarrative Tests ───────────────────────────────────────────────────

public sealed class BulkEraNarrativeTests
{
    // -------------------------------------------------------------------------
    // Test 13: AvailableTones has 8 entries
    // -------------------------------------------------------------------------

    [Fact]
    public void AvailableTones_HasEightEntries()
    {
        Assert.Equal(8, BulkEraNarrative.AvailableTones.Length);
    }

    // -------------------------------------------------------------------------
    // Test 14: CreateConfig with valid tone succeeds
    // -------------------------------------------------------------------------

    [Fact]
    public void CreateConfig_ValidTone_Succeeds()
    {
        var config = BulkEraNarrative.CreateConfig("era-1", "Age of Ice", "witty");
        Assert.Equal("era-1", config.EraId);
        Assert.Equal("witty", config.Tone);
        Assert.Equal(5000, config.TargetWordCountMin);
        Assert.Equal(7000, config.TargetWordCountMax);
    }

    // -------------------------------------------------------------------------
    // Test 15: CreateConfig with invalid tone throws
    // -------------------------------------------------------------------------

    [Fact]
    public void CreateConfig_InvalidTone_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BulkEraNarrative.CreateConfig("era-1", "Age of Ice", "grumpy"));
    }

    // -------------------------------------------------------------------------
    // Test 16: PipelineSteps has 3 steps
    // -------------------------------------------------------------------------

    [Fact]
    public void PipelineSteps_HasThreeSteps()
    {
        Assert.Equal(3, BulkEraNarrative.PipelineSteps.Length);
        Assert.Equal("threads", BulkEraNarrative.PipelineSteps[0]);
        Assert.Equal("generate", BulkEraNarrative.PipelineSteps[1]);
        Assert.Equal("edit", BulkEraNarrative.PipelineSteps[2]);
    }
}

// ─── BulkFactCoverage Tests ───────────────────────────────────────────────────

public sealed class BulkFactCoverageTests
{
    [Fact]
    public void PrepareChronicles_FiltersOutChroniclesWithoutTitleOrSummary()
    {
        var chronicles = new[]
        {
            new FactCoverageChronicle("c1", "Title One", "Summary one", []),
            new FactCoverageChronicle("c2", "", "Summary two", []),
            new FactCoverageChronicle("c3", "Title Three", null, []),
            new FactCoverageChronicle("c4", "Title Four", "Summary four", []),
        };

        var prepared = BulkFactCoverage.PrepareChronicles(chronicles);

        Assert.Equal(2, prepared.Count);
        Assert.Contains(prepared, c => c.ChronicleId == "c1");
        Assert.Contains(prepared, c => c.ChronicleId == "c4");
    }
}

// ─── BulkToneRanking Tests ────────────────────────────────────────────────────

public sealed class BulkToneRankingTests
{
    [Fact]
    public void PrepareChronicles_SortsByTitleAndFiltersUntitled()
    {
        var chronicles = new[]
        {
            new ToneRankingChronicle("c1", "Zephyr Wind", null, null, []),
            new ToneRankingChronicle("c2", "", null, null, []),
            new ToneRankingChronicle("c3", "Amber Fog", "summary", null, []),
        };

        var prepared = BulkToneRanking.PrepareChronicles(chronicles);

        // Untitled filtered; alphabetical order: Amber < Zephyr
        Assert.Equal(2, prepared.Count);
        Assert.Equal("Amber Fog", prepared[0].Title);
        Assert.Equal("Zephyr Wind", prepared[1].Title);
    }
}
