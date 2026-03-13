using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;

namespace TheCanonry.Persistence.Repositories;

public class ContentTreeRepository
{
    private readonly CanonryDbContext _db;

    public ContentTreeRepository(CanonryDbContext db)
    {
        _db = db;
    }

    public async Task<ContentTreeEntity?> Get(string projectId, string simulationRunId)
    {
        return await _db.ContentTrees
            .FirstOrDefaultAsync(t => t.ProjectId == projectId && t.SimulationRunId == simulationRunId);
    }

    public async Task<ContentTreeEntity> Upsert(ContentTreeEntity tree)
    {
        var existing = await _db.ContentTrees
            .FirstOrDefaultAsync(t => t.ProjectId == tree.ProjectId && t.SimulationRunId == tree.SimulationRunId);
        if (existing is null)
        {
            _db.ContentTrees.Add(tree);
        }
        else
        {
            tree.Id = existing.Id;
            _db.Entry(existing).CurrentValues.SetValues(tree);
        }
        await _db.SaveChangesAsync();
        return tree;
    }
}
