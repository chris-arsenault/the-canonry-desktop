using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;

namespace TheCanonry.Persistence.Repositories;

public class PageLayoutRepository
{
    private readonly CanonryDbContext _db;

    public PageLayoutRepository(CanonryDbContext db)
    {
        _db = db;
    }

    public async Task<PageLayoutEntity?> Get(string simulationRunId, string pageId)
    {
        return await _db.PageLayouts
            .FirstOrDefaultAsync(l => l.SimulationRunId == simulationRunId && l.PageId == pageId);
    }

    public async Task<List<PageLayoutEntity>> GetBySimulation(string simulationRunId)
    {
        return await _db.PageLayouts
            .Where(l => l.SimulationRunId == simulationRunId)
            .ToListAsync();
    }

    public async Task<PageLayoutEntity> Upsert(PageLayoutEntity layout)
    {
        var existing = await _db.PageLayouts
            .FirstOrDefaultAsync(l => l.SimulationRunId == layout.SimulationRunId && l.PageId == layout.PageId);
        if (existing is null)
        {
            _db.PageLayouts.Add(layout);
        }
        else
        {
            layout.Id = existing.Id;
            _db.Entry(existing).CurrentValues.SetValues(layout);
        }
        await _db.SaveChangesAsync();
        return layout;
    }
}
