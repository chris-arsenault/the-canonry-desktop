namespace TheCanonry.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;

public class ImageRepository
{
    private readonly CanonryDbContext _db;

    public ImageRepository(CanonryDbContext db)
    {
        _db = db;
    }

    public async Task<ImageRecord> Create(ImageRecord image)
    {
        _db.Images.Add(image);
        await _db.SaveChangesAsync();
        return image;
    }

    public async Task<ImageRecord?> GetById(long id)
    {
        return await _db.Images.FindAsync(id);
    }

    public async Task<List<ImageRecord>> GetByEntity(string entityId)
    {
        return await _db.Images
            .Where(i => i.EntityId == entityId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ImageRecord>> GetBySimulation(string simulationRunId)
    {
        return await _db.Images
            .Where(i => i.SimulationRunId == simulationRunId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ImageRecord>> Search(string simulationRunId, string? type = null, string? model = null)
    {
        var query = _db.Images.Where(i => i.SimulationRunId == simulationRunId);

        if (type is not null)
            query = query.Where(i => i.Type == type);

        if (model is not null)
            query = query.Where(i => i.Model == model);

        return await query.OrderByDescending(i => i.CreatedAt).ToListAsync();
    }

    public async Task Delete(long id)
    {
        var image = await _db.Images.FindAsync(id);
        if (image is not null)
        {
            _db.Images.Remove(image);
            await _db.SaveChangesAsync();
        }
    }
}
