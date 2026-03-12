namespace TheCanonry.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;

public class TraitPaletteRepository
{
    private readonly CanonryDbContext _db;

    public TraitPaletteRepository(CanonryDbContext db)
    {
        _db = db;
    }

    public async Task<List<TraitPaletteEntity>> GetByProject(string projectId)
    {
        return await _db.TraitPalettes
            .Where(p => p.ProjectId == projectId)
            .ToListAsync();
    }

    public async Task<TraitPaletteEntity?> GetByKind(string projectId, string kind)
    {
        return await _db.TraitPalettes
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.EntityKind == kind);
    }

    public async Task<TraitPaletteEntity> Upsert(TraitPaletteEntity palette)
    {
        var existing = await _db.TraitPalettes
            .FirstOrDefaultAsync(p => p.ProjectId == palette.ProjectId && p.EntityKind == palette.EntityKind);
        if (existing is null)
        {
            _db.TraitPalettes.Add(palette);
        }
        else
        {
            palette.Id = existing.Id;
            _db.Entry(existing).CurrentValues.SetValues(palette);
        }
        await _db.SaveChangesAsync();
        return palette;
    }
}
