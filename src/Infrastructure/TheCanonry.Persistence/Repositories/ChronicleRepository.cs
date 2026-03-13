using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;

namespace TheCanonry.Persistence.Repositories;

public class ChronicleRepository
{
    private readonly CanonryDbContext _db;

    public ChronicleRepository(CanonryDbContext db)
    {
        _db = db;
    }

    public async Task<Chronicle> Create(Chronicle chronicle)
    {
        _db.Chronicles.Add(chronicle);
        await _db.SaveChangesAsync();
        return chronicle;
    }

    public async Task<Chronicle?> GetById(long id)
    {
        return await _db.Chronicles.FindAsync(id);
    }

    public async Task<List<Chronicle>> GetBySimulation(string simulationRunId)
    {
        return await _db.Chronicles
            .Where(c => c.SimulationRunId == simulationRunId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Chronicle>> GetByEntityId(string entityId)
    {
        // EntityIds is a JSON array stored as string; use Contains for simple matching
        return await _db.Chronicles
            .Where(c => c.EntityIds.Contains(entityId))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Chronicle> Update(Chronicle chronicle)
    {
        _db.Chronicles.Update(chronicle);
        await _db.SaveChangesAsync();
        return chronicle;
    }

    public async Task Delete(long id)
    {
        var chronicle = await _db.Chronicles.FindAsync(id);
        if (chronicle is not null)
        {
            _db.Chronicles.Remove(chronicle);
            await _db.SaveChangesAsync();
        }
    }
}
