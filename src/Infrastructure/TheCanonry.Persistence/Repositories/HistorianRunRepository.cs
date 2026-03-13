using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;

namespace TheCanonry.Persistence.Repositories;

public class HistorianRunRepository
{
    private readonly CanonryDbContext _db;

    public HistorianRunRepository(CanonryDbContext db)
    {
        _db = db;
    }

    public async Task<List<HistorianRun>> GetBySimulation(string simulationRunId)
    {
        return await _db.HistorianRuns
            .Where(h => h.SimulationRunId == simulationRunId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<HistorianRun>> GetByEntity(string simulationRunId, string entityId)
    {
        return await _db.HistorianRuns
            .Where(h => h.SimulationRunId == simulationRunId && h.EntityId == entityId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();
    }

    public async Task<HistorianRun> Create(HistorianRun run)
    {
        _db.HistorianRuns.Add(run);
        await _db.SaveChangesAsync();
        return run;
    }

    public async Task<HistorianRun> Update(HistorianRun run)
    {
        _db.HistorianRuns.Update(run);
        await _db.SaveChangesAsync();
        return run;
    }

    public async Task Delete(long id)
    {
        var run = await _db.HistorianRuns.FindAsync(id);
        if (run is not null)
        {
            _db.HistorianRuns.Remove(run);
            await _db.SaveChangesAsync();
        }
    }
}
