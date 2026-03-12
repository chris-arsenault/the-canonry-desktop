namespace TheCanonry.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;

public class EnrichmentJobRepository
{
    private readonly CanonryDbContext _db;

    public EnrichmentJobRepository(CanonryDbContext db)
    {
        _db = db;
    }

    public async Task<EnrichmentJobEntity> Create(EnrichmentJobEntity job)
    {
        _db.EnrichmentJobs.Add(job);
        await _db.SaveChangesAsync();
        return job;
    }

    public async Task<EnrichmentJobEntity?> GetById(long id)
    {
        return await _db.EnrichmentJobs.FindAsync(id);
    }

    public async Task<List<EnrichmentJobEntity>> GetByStatus(string status)
    {
        return await _db.EnrichmentJobs
            .Where(j => j.Status == status)
            .OrderBy(j => j.QueuedAt)
            .ToListAsync();
    }

    public async Task<List<EnrichmentJobEntity>> GetBySlot(string simulationRunId)
    {
        return await _db.EnrichmentJobs
            .Where(j => j.SlotSimulationRunId == simulationRunId)
            .OrderByDescending(j => j.QueuedAt)
            .ToListAsync();
    }

    public async Task<List<EnrichmentJobEntity>> GetOrphaned()
    {
        return await _db.EnrichmentJobs
            .Where(j => j.Status == "Running")
            .OrderBy(j => j.StartedAt)
            .ToListAsync();
    }

    public async Task UpdateStatus(long id, string status, string? errorMessage = null)
    {
        var job = await _db.EnrichmentJobs.FindAsync(id);
        if (job is null)
            throw new InvalidOperationException($"EnrichmentJob {id} not found");

        job.Status = status;

        if (status == "Running")
            job.StartedAt = DateTime.UtcNow;

        if (status == "Completed" || status == "Failed")
            job.CompletedAt = DateTime.UtcNow;

        if (errorMessage is not null)
            job.ErrorMessage = errorMessage;

        await _db.SaveChangesAsync();
    }

    public async Task Delete(long id)
    {
        var job = await _db.EnrichmentJobs.FindAsync(id);
        if (job is not null)
        {
            _db.EnrichmentJobs.Remove(job);
            await _db.SaveChangesAsync();
        }
    }
}
