using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;

namespace TheCanonry.Persistence.Repositories;

public class EraNarrativeRepository
{
    private readonly CanonryDbContext _db;

    public EraNarrativeRepository(CanonryDbContext db)
    {
        _db = db;
    }

    public async Task<List<EraNarrative>> GetBySimulation(string simulationRunId)
    {
        return await _db.EraNarratives
            .Where(n => n.SimulationRunId == simulationRunId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<EraNarrative?> GetByEra(string simulationRunId, string eraId)
    {
        return await _db.EraNarratives
            .FirstOrDefaultAsync(n => n.SimulationRunId == simulationRunId && n.EraId == eraId);
    }

    public async Task<EraNarrative> Create(EraNarrative narrative)
    {
        _db.EraNarratives.Add(narrative);
        await _db.SaveChangesAsync();
        return narrative;
    }

    public async Task<EraNarrative> Update(EraNarrative narrative)
    {
        _db.EraNarratives.Update(narrative);
        await _db.SaveChangesAsync();
        return narrative;
    }

    public async Task Delete(long id)
    {
        var narrative = await _db.EraNarratives.FindAsync(id);
        if (narrative is not null)
        {
            _db.EraNarratives.Remove(narrative);
            await _db.SaveChangesAsync();
        }
    }
}
