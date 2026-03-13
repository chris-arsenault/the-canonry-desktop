using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;

namespace TheCanonry.Persistence.Repositories;

public class CostSummaryEntry
{
    public string Model { get; set; } = "";
    public string TaskType { get; set; } = "";
    public int TotalInputTokens { get; set; }
    public int TotalOutputTokens { get; set; }
    public decimal TotalCost { get; set; }
    public int Count { get; set; }
}

public class CostRepository
{
    private readonly CanonryDbContext _db;

    public CostRepository(CanonryDbContext db)
    {
        _db = db;
    }

    public async Task<ApiCostEntry> Create(ApiCostEntry entry)
    {
        _db.Costs.Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    public async Task<List<ApiCostEntry>> GetBySimulation(string simulationRunId)
    {
        return await _db.Costs
            .Where(c => c.SimulationRunId == simulationRunId)
            .OrderByDescending(c => c.Timestamp)
            .ToListAsync();
    }

    public async Task<List<CostSummaryEntry>> Summarize(string simulationRunId)
    {
        return await _db.Costs
            .Where(c => c.SimulationRunId == simulationRunId)
            .GroupBy(c => new { c.Model, c.TaskType })
            .Select(g => new CostSummaryEntry
            {
                Model = g.Key.Model,
                TaskType = g.Key.TaskType,
                TotalInputTokens = g.Sum(x => x.InputTokens),
                TotalOutputTokens = g.Sum(x => x.OutputTokens),
                TotalCost = g.Sum(x => x.Cost),
                Count = g.Count(),
            })
            .ToListAsync();
    }

    public async Task<decimal> GetTotal(string simulationRunId)
    {
        return await _db.Costs
            .Where(c => c.SimulationRunId == simulationRunId)
            .SumAsync(c => c.Cost);
    }
}
