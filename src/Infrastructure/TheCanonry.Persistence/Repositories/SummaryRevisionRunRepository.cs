namespace TheCanonry.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;

public class SummaryRevisionRunRepository
{
    private readonly CanonryDbContext _db;

    public SummaryRevisionRunRepository(CanonryDbContext db)
    {
        _db = db;
    }

    public async Task<List<SummaryRevisionRunEntity>> GetBySimulation(string simulationRunId)
    {
        return await _db.SummaryRevisionRuns
            .Where(r => r.SimulationRunId == simulationRunId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<SummaryRevisionRunEntity> Create(SummaryRevisionRunEntity run)
    {
        _db.SummaryRevisionRuns.Add(run);
        await _db.SaveChangesAsync();
        return run;
    }

    public async Task<SummaryRevisionRunEntity> Update(SummaryRevisionRunEntity run)
    {
        _db.SummaryRevisionRuns.Update(run);
        await _db.SaveChangesAsync();
        return run;
    }

    public async Task Delete(long id)
    {
        var run = await _db.SummaryRevisionRuns.FindAsync(id);
        if (run is not null)
        {
            _db.SummaryRevisionRuns.Remove(run);
            await _db.SaveChangesAsync();
        }
    }
}
