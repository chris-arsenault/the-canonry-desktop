namespace TheCanonry.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;

public class SimulationSlotRepository
{
    private readonly CanonryDbContext _db;

    public SimulationSlotRepository(CanonryDbContext db)
    {
        _db = db;
    }

    public async Task<List<SimulationSlotEntity>> GetByProject(string projectId)
    {
        return await _db.SimulationSlots
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.SlotIndex)
            .ToListAsync();
    }

    public async Task<SimulationSlotEntity?> GetByIndex(string projectId, int slotIndex)
    {
        return await _db.SimulationSlots
            .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.SlotIndex == slotIndex);
    }

    public async Task<SimulationSlotEntity> Upsert(SimulationSlotEntity slot)
    {
        var existing = await _db.SimulationSlots
            .FirstOrDefaultAsync(s => s.ProjectId == slot.ProjectId && s.SlotIndex == slot.SlotIndex);
        if (existing is null)
        {
            _db.SimulationSlots.Add(slot);
        }
        else
        {
            slot.Id = existing.Id;
            _db.Entry(existing).CurrentValues.SetValues(slot);
        }
        await _db.SaveChangesAsync();
        return slot;
    }

    public async Task Delete(long id)
    {
        var slot = await _db.SimulationSlots.FindAsync(id);
        if (slot is not null)
        {
            _db.SimulationSlots.Remove(slot);
            await _db.SaveChangesAsync();
        }
    }
}
