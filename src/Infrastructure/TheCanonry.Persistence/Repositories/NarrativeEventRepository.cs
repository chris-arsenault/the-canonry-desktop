namespace TheCanonry.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;

public class NarrativeEventRepository
{
    private readonly CanonryDbContext _db;

    public NarrativeEventRepository(CanonryDbContext db)
    {
        _db = db;
    }

    public async Task<List<NarrativeEventEntity>> GetBySimulation(string simulationRunId)
    {
        return await _db.NarrativeEvents
            .Where(e => e.SimulationRunId == simulationRunId)
            .OrderBy(e => e.Tick)
            .ToListAsync();
    }

    public async Task<List<NarrativeEventEntity>> GetByEra(string simulationRunId, string eraId)
    {
        return await _db.NarrativeEvents
            .Where(e => e.SimulationRunId == simulationRunId && e.EraId == eraId)
            .OrderBy(e => e.Tick)
            .ToListAsync();
    }

    public async Task<List<NarrativeEventEntity>> GetByEntity(string simulationRunId, string entityId)
    {
        return await _db.NarrativeEvents
            .Where(e => e.SimulationRunId == simulationRunId && e.SubjectId == entityId)
            .OrderBy(e => e.Tick)
            .ToListAsync();
    }

    public async Task<NarrativeEventEntity> Create(NarrativeEventEntity narrativeEvent)
    {
        _db.NarrativeEvents.Add(narrativeEvent);
        await _db.SaveChangesAsync();
        return narrativeEvent;
    }

    public async Task BulkCreate(List<NarrativeEventEntity> events)
    {
        _db.NarrativeEvents.AddRange(events);
        await _db.SaveChangesAsync();
    }
}
