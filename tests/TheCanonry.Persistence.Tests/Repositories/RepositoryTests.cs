using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;
using TheCanonry.Persistence.Repositories;

namespace TheCanonry.Persistence.Tests.Repositories;

public sealed class RepositoryTests : IDisposable
{
    private readonly CanonryDbContext _db;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CanonryDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _db = new CanonryDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
    }

    // --- Chronicle CRUD ---

    [Fact]
    public async Task Chronicle_Create_and_GetById()
    {
        var repo = new ChronicleRepository(_db);

        var chronicle = new Chronicle
        {
            SimulationRunId = "run-1",
            EntityIds = "[\"e-1\",\"e-2\"]",
            Format = "narrative",
            Content = "The kingdom rose from ashes.",
            Title = "Rise of Kingdoms",
        };

        var created = await repo.Create(chronicle);
        Assert.True(created.Id > 0);

        var loaded = await repo.GetById(created.Id);
        Assert.NotNull(loaded);
        Assert.Equal("run-1", loaded.SimulationRunId);
        Assert.Equal("Rise of Kingdoms", loaded.Title);
        Assert.Equal("The kingdom rose from ashes.", loaded.Content);
    }

    [Fact]
    public async Task Chronicle_Update_and_Delete()
    {
        var repo = new ChronicleRepository(_db);

        var chronicle = new Chronicle
        {
            SimulationRunId = "run-1",
            EntityIds = "[\"e-1\"]",
            Content = "Draft content",
        };
        await repo.Create(chronicle);

        chronicle.Content = "Revised content";
        chronicle.AcceptedAt = DateTime.UtcNow;
        await repo.Update(chronicle);

        var loaded = await repo.GetById(chronicle.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Revised content", loaded.Content);
        Assert.NotNull(loaded.AcceptedAt);

        await repo.Delete(chronicle.Id);
        var deleted = await repo.GetById(chronicle.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task Chronicle_GetBySimulation_and_GetByEntityId()
    {
        var repo = new ChronicleRepository(_db);

        await repo.Create(new Chronicle
        {
            SimulationRunId = "run-1",
            EntityIds = "[\"e-1\",\"e-2\"]",
            Content = "Chronicle A",
        });
        await repo.Create(new Chronicle
        {
            SimulationRunId = "run-1",
            EntityIds = "[\"e-3\"]",
            Content = "Chronicle B",
        });
        await repo.Create(new Chronicle
        {
            SimulationRunId = "run-2",
            EntityIds = "[\"e-1\"]",
            Content = "Chronicle C",
        });

        var byRun = await repo.GetBySimulation("run-1");
        Assert.Equal(2, byRun.Count);

        var byEntity = await repo.GetByEntityId("e-1");
        Assert.Equal(2, byEntity.Count);
    }

    // --- Image CRUD ---

    [Fact]
    public async Task Image_Create_GetById_and_Delete()
    {
        var repo = new ImageRepository(_db);

        var image = new ImageRecord
        {
            SimulationRunId = "run-1",
            EntityId = "e-1",
            Prompt = "A medieval castle at sunset",
            Model = "dall-e-3",
            Width = 1024,
            Height = 1024,
            Aspect = "square",
            Type = "entity",
            FilePath = "/images/castle.png",
        };

        var created = await repo.Create(image);
        Assert.True(created.Id > 0);

        var loaded = await repo.GetById(created.Id);
        Assert.NotNull(loaded);
        Assert.Equal("dall-e-3", loaded.Model);
        Assert.Equal(1024, loaded.Width);

        await repo.Delete(created.Id);
        Assert.Null(await repo.GetById(created.Id));
    }

    [Fact]
    public async Task Image_Search_filters_by_type_and_model()
    {
        var repo = new ImageRepository(_db);

        await repo.Create(new ImageRecord
        {
            SimulationRunId = "run-1",
            EntityId = "e-1",
            Prompt = "portrait",
            Model = "dall-e-3",
            Width = 512, Height = 512,
            Type = "entity",
            FilePath = "/a.png",
        });
        await repo.Create(new ImageRecord
        {
            SimulationRunId = "run-1",
            Prompt = "landscape",
            Model = "flux",
            Width = 1024, Height = 768,
            Type = "scene",
            FilePath = "/b.png",
        });

        var entityOnly = await repo.Search("run-1", type: "entity");
        Assert.Single(entityOnly);
        Assert.Equal("entity", entityOnly[0].Type);

        var fluxOnly = await repo.Search("run-1", model: "flux");
        Assert.Single(fluxOnly);
        Assert.Equal("flux", fluxOnly[0].Model);

        var all = await repo.Search("run-1");
        Assert.Equal(2, all.Count);
    }

    // --- EnrichmentJob lifecycle ---

    [Fact]
    public async Task EnrichmentJob_lifecycle_queued_to_running_to_completed()
    {
        var repo = new EnrichmentJobRepository(_db);

        var job = new EnrichmentJobEntity
        {
            TaskType = "description",
            TargetEntityId = "e-1",
            SlotSimulationRunId = "run-1",
            Status = "Queued",
            QueuedAt = DateTime.UtcNow,
            AttemptCount = 0,
        };
        var created = await repo.Create(job);

        var queued = await repo.GetByStatus("Queued");
        Assert.Single(queued);

        await repo.UpdateStatus(created.Id, "Running");
        var running = await repo.GetById(created.Id);
        Assert.NotNull(running);
        Assert.Equal("Running", running.Status);
        Assert.NotNull(running.StartedAt);

        await repo.UpdateStatus(created.Id, "Completed");
        var completed = await repo.GetById(created.Id);
        Assert.NotNull(completed);
        Assert.Equal("Completed", completed.Status);
        Assert.NotNull(completed.CompletedAt);
    }

    [Fact]
    public async Task EnrichmentJob_GetOrphaned_returns_running_jobs()
    {
        var repo = new EnrichmentJobRepository(_db);

        await repo.Create(new EnrichmentJobEntity
        {
            TaskType = "description",
            TargetEntityId = "e-1",
            SlotSimulationRunId = "run-1",
            Status = "Running",
            QueuedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow.AddMinutes(-30),
            AttemptCount = 1,
        });
        await repo.Create(new EnrichmentJobEntity
        {
            TaskType = "summary",
            TargetEntityId = "e-2",
            SlotSimulationRunId = "run-1",
            Status = "Queued",
            QueuedAt = DateTime.UtcNow,
            AttemptCount = 0,
        });

        var orphans = await repo.GetOrphaned();
        Assert.Single(orphans);
        Assert.Equal("Running", orphans[0].Status);
    }

    [Fact]
    public async Task EnrichmentJob_UpdateStatus_failed_with_error_message()
    {
        var repo = new EnrichmentJobRepository(_db);

        var job = await repo.Create(new EnrichmentJobEntity
        {
            TaskType = "image",
            TargetEntityId = "e-5",
            SlotSimulationRunId = "run-1",
            Status = "Running",
            QueuedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow,
            AttemptCount = 1,
        });

        await repo.UpdateStatus(job.Id, "Failed", "API rate limit exceeded");

        var failed = await repo.GetById(job.Id);
        Assert.NotNull(failed);
        Assert.Equal("Failed", failed.Status);
        Assert.Equal("API rate limit exceeded", failed.ErrorMessage);
        Assert.NotNull(failed.CompletedAt);
    }

    // --- Cost recording and summarization ---

    [Fact]
    public async Task Cost_Create_GetBySimulation_and_GetTotal()
    {
        var repo = new CostRepository(_db);

        await repo.Create(new ApiCostEntry
        {
            SimulationRunId = "run-1",
            TaskType = "description",
            Model = "claude-3",
            InputTokens = 1000,
            OutputTokens = 500,
            Cost = 0.015m,
        });
        await repo.Create(new ApiCostEntry
        {
            SimulationRunId = "run-1",
            TaskType = "summary",
            Model = "claude-3",
            InputTokens = 800,
            OutputTokens = 300,
            Cost = 0.010m,
        });
        await repo.Create(new ApiCostEntry
        {
            SimulationRunId = "run-2",
            TaskType = "description",
            Model = "gpt-4",
            InputTokens = 500,
            OutputTokens = 200,
            Cost = 0.008m,
        });

        var byRun = await repo.GetBySimulation("run-1");
        Assert.Equal(2, byRun.Count);

        var total = await repo.GetTotal("run-1");
        Assert.Equal(0.025m, total);
    }

    [Fact]
    public async Task Cost_Summarize_groups_by_model_and_taskType()
    {
        var repo = new CostRepository(_db);

        await repo.Create(new ApiCostEntry
        {
            SimulationRunId = "run-1",
            TaskType = "description",
            Model = "claude-3",
            InputTokens = 1000,
            OutputTokens = 500,
            Cost = 0.015m,
        });
        await repo.Create(new ApiCostEntry
        {
            SimulationRunId = "run-1",
            TaskType = "description",
            Model = "claude-3",
            InputTokens = 1200,
            OutputTokens = 600,
            Cost = 0.018m,
        });
        await repo.Create(new ApiCostEntry
        {
            SimulationRunId = "run-1",
            TaskType = "image",
            Model = "dall-e-3",
            InputTokens = 0,
            OutputTokens = 0,
            Cost = 0.040m,
        });

        var summary = await repo.Summarize("run-1");
        Assert.Equal(2, summary.Count);

        var claudeDesc = summary.First(s => s.Model == "claude-3" && s.TaskType == "description");
        Assert.Equal(2200, claudeDesc.TotalInputTokens);
        Assert.Equal(1100, claudeDesc.TotalOutputTokens);
        Assert.Equal(0.033m, claudeDesc.TotalCost);
        Assert.Equal(2, claudeDesc.Count);

        var dalleImage = summary.First(s => s.Model == "dall-e-3");
        Assert.Equal(0.040m, dalleImage.TotalCost);
        Assert.Equal(1, dalleImage.Count);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
