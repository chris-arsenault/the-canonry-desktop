namespace TheCanonry.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;

public class WorldSchemaRepository
{
    private readonly CanonryDbContext _db;

    public WorldSchemaRepository(CanonryDbContext db)
    {
        _db = db;
    }

    public async Task<WorldSchemaEntity?> GetByProject(string projectId)
    {
        return await _db.WorldSchemas
            .FirstOrDefaultAsync(s => s.ProjectId == projectId);
    }

    public async Task<WorldSchemaEntity> Upsert(WorldSchemaEntity schema)
    {
        var existing = await _db.WorldSchemas
            .FirstOrDefaultAsync(s => s.ProjectId == schema.ProjectId);
        if (existing is null)
        {
            _db.WorldSchemas.Add(schema);
        }
        else
        {
            schema.Id = existing.Id;
            _db.Entry(existing).CurrentValues.SetValues(schema);
        }
        await _db.SaveChangesAsync();
        return schema;
    }
}
