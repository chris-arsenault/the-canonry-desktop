namespace TheCanonry.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;

public class DynamicsRunRepository
{
    private readonly CanonryDbContext _db;

    public DynamicsRunRepository(CanonryDbContext db)
    {
        _db = db;
    }

    public async Task<List<DynamicsRunEntity>> GetBySimulation(string simulationRunId)
    {
        return await _db.DynamicsRuns
            .Where(r => r.SimulationRunId == simulationRunId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<DynamicsRunEntity> Create(DynamicsRunEntity run)
    {
        _db.DynamicsRuns.Add(run);
        await _db.SaveChangesAsync();
        return run;
    }

    public async Task<DynamicsRunEntity> Update(DynamicsRunEntity run)
    {
        _db.DynamicsRuns.Update(run);
        await _db.SaveChangesAsync();
        return run;
    }

    public async Task Delete(long id)
    {
        var run = await _db.DynamicsRuns.FindAsync(id);
        if (run is not null)
        {
            _db.DynamicsRuns.Remove(run);
            await _db.SaveChangesAsync();
        }
    }
}
