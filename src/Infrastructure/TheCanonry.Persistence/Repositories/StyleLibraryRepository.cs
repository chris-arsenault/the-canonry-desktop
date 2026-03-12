namespace TheCanonry.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;

public class StyleLibraryRepository
{
    private readonly CanonryDbContext _db;

    public StyleLibraryRepository(CanonryDbContext db)
    {
        _db = db;
    }

    public async Task<StyleLibraryEntity?> GetByProject(string projectId)
    {
        return await _db.StyleLibraries
            .FirstOrDefaultAsync(s => s.ProjectId == projectId);
    }

    public async Task<StyleLibraryEntity> Upsert(StyleLibraryEntity library)
    {
        var existing = await _db.StyleLibraries
            .FirstOrDefaultAsync(s => s.ProjectId == library.ProjectId);
        if (existing is null)
        {
            _db.StyleLibraries.Add(library);
        }
        else
        {
            library.Id = existing.Id;
            _db.Entry(existing).CurrentValues.SetValues(library);
        }
        await _db.SaveChangesAsync();
        return library;
    }
}
