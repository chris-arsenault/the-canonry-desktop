namespace TheCanonry.Persistence;

using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;

public class CanonryDbContext : DbContext
{
    public CanonryDbContext(DbContextOptions<CanonryDbContext> options) : base(options) { }

    public DbSet<SimulationSlotEntity> SimulationSlots => Set<SimulationSlotEntity>();
    public DbSet<PersistedEntity> Entities => Set<PersistedEntity>();
    public DbSet<PersistedRelationship> Relationships => Set<PersistedRelationship>();
    public DbSet<EnrichmentJobEntity> EnrichmentJobs => Set<EnrichmentJobEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SimulationSlotEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProjectId, x.SlotIndex }).IsUnique();
            e.HasIndex(x => x.SimulationRunId);
        });

        modelBuilder.Entity<PersistedEntity>(e =>
        {
            e.HasKey(x => new { x.Id, x.SimulationRunId });
            e.HasIndex(x => x.SimulationRunId);
            e.HasIndex(x => x.Kind);
            e.HasIndex(x => new { x.SimulationRunId, x.Kind });
        });

        modelBuilder.Entity<PersistedRelationship>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SimulationRunId);
            e.HasIndex(x => x.SourceId);
            e.HasIndex(x => x.TargetId);
        });

        modelBuilder.Entity<EnrichmentJobEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SlotSimulationRunId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.TaskType);
        });
    }
}
