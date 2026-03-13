using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;

namespace TheCanonry.Persistence.Tests;

public sealed class DbContextTests : IDisposable
{
    private readonly CanonryDbContext _db;

    public DbContextTests()
    {
        var options = new DbContextOptionsBuilder<CanonryDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _db = new CanonryDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Can_create_and_retrieve_simulation_slot()
    {
        var slot = new SimulationSlotEntity
        {
            ProjectId = "proj-1",
            SlotIndex = 0,
            SimulationRunId = "run-abc",
            UpdatedAt = DateTime.UtcNow,
        };

        _db.SimulationSlots.Add(slot);
        await _db.SaveChangesAsync();

        var loaded = await _db.SimulationSlots.FirstAsync();
        Assert.Equal("proj-1", loaded.ProjectId);
        Assert.Equal("run-abc", loaded.SimulationRunId);
    }

    [Fact]
    public async Task Can_create_and_retrieve_enrichment_job()
    {
        var job = new EnrichmentJobEntity
        {
            TaskType = "description",
            TargetEntityId = "e-1",
            SlotSimulationRunId = "run-abc",
            Status = "Queued",
            QueuedAt = DateTime.UtcNow,
            AttemptCount = 0,
        };

        _db.EnrichmentJobs.Add(job);
        await _db.SaveChangesAsync();

        var loaded = await _db.EnrichmentJobs.FirstAsync();
        Assert.Equal("description", loaded.TaskType);
        Assert.Equal("Queued", loaded.Status);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _db.Dispose();
    }
}
