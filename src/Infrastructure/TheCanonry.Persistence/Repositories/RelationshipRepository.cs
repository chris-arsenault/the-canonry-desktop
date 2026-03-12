namespace TheCanonry.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;

public class RelationshipRepository
{
    private readonly CanonryDbContext _db;

    public RelationshipRepository(CanonryDbContext db)
    {
        _db = db;
    }

    public async Task<List<PersistedRelationship>> GetBySimulation(string simulationRunId)
    {
        return await _db.Relationships
            .Where(r => r.SimulationRunId == simulationRunId)
            .ToListAsync();
    }

    public async Task<List<PersistedRelationship>> GetByEntity(string simulationRunId, string entityId)
    {
        return await _db.Relationships
            .Where(r => r.SimulationRunId == simulationRunId
                && (r.SourceId == entityId || r.TargetId == entityId))
            .ToListAsync();
    }

    public async Task<PersistedRelationship> Create(PersistedRelationship relationship)
    {
        _db.Relationships.Add(relationship);
        await _db.SaveChangesAsync();
        return relationship;
    }

    public async Task Delete(long id)
    {
        var relationship = await _db.Relationships.FindAsync(id);
        if (relationship is not null)
        {
            _db.Relationships.Remove(relationship);
            await _db.SaveChangesAsync();
        }
    }
}
