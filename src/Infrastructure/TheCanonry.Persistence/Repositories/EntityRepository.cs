using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;

namespace TheCanonry.Persistence.Repositories;

public class EntityRepository
{
    private readonly CanonryDbContext _db;

    public EntityRepository(CanonryDbContext db)
    {
        _db = db;
    }

    public async Task<List<PersistedEntity>> GetBySimulation(string simulationRunId)
    {
        return await _db.Entities
            .Where(e => e.SimulationRunId == simulationRunId)
            .ToListAsync();
    }

    public async Task<List<PersistedEntity>> GetByKind(string simulationRunId, string kind)
    {
        return await _db.Entities
            .Where(e => e.SimulationRunId == simulationRunId && e.Kind == kind)
            .ToListAsync();
    }

    public async Task<PersistedEntity?> GetById(string id, string simulationRunId)
    {
        return await _db.Entities.FindAsync(id, simulationRunId);
    }

    public async Task<List<PersistedEntity>> Search(string simulationRunId, string query)
    {
        return await _db.Entities
            .Where(e => e.SimulationRunId == simulationRunId
                && (e.Name.Contains(query) || e.Description.Contains(query)))
            .ToListAsync();
    }

    public async Task<PersistedEntity> Upsert(PersistedEntity entity)
    {
        var existing = await _db.Entities.FindAsync(entity.Id, entity.SimulationRunId);
        if (existing is null)
        {
            _db.Entities.Add(entity);
        }
        else
        {
            _db.Entry(existing).CurrentValues.SetValues(entity);
        }
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task Delete(string id, string simulationRunId)
    {
        var entity = await _db.Entities.FindAsync(id, simulationRunId);
        if (entity is not null)
        {
            _db.Entities.Remove(entity);
            await _db.SaveChangesAsync();
        }
    }
}
