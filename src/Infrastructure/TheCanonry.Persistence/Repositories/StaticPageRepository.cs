using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;

namespace TheCanonry.Persistence.Repositories;

public class StaticPageRepository
{
    private readonly CanonryDbContext _db;

    public StaticPageRepository(CanonryDbContext db)
    {
        _db = db;
    }

    public async Task<List<StaticPageEntity>> GetBySimulation(string simulationRunId)
    {
        return await _db.StaticPages
            .Where(p => p.SimulationRunId == simulationRunId)
            .OrderBy(p => p.Title)
            .ToListAsync();
    }

    public async Task<StaticPageEntity?> GetByPageId(string pageId)
    {
        return await _db.StaticPages
            .FirstOrDefaultAsync(p => p.PageId == pageId);
    }

    public async Task<StaticPageEntity> Create(StaticPageEntity page)
    {
        _db.StaticPages.Add(page);
        await _db.SaveChangesAsync();
        return page;
    }

    public async Task<StaticPageEntity> Update(StaticPageEntity page)
    {
        _db.StaticPages.Update(page);
        await _db.SaveChangesAsync();
        return page;
    }

    public async Task Delete(long id)
    {
        var page = await _db.StaticPages.FindAsync(id);
        if (page is not null)
        {
            _db.StaticPages.Remove(page);
            await _db.SaveChangesAsync();
        }
    }
}
