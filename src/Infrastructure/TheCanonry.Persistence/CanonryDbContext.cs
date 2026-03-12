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
    public DbSet<Chronicle> Chronicles => Set<Chronicle>();
    public DbSet<HistorianRun> HistorianRuns => Set<HistorianRun>();
    public DbSet<EraNarrative> EraNarratives => Set<EraNarrative>();
    public DbSet<ImageRecord> Images => Set<ImageRecord>();
    public DbSet<ApiCostEntry> Costs => Set<ApiCostEntry>();
    public DbSet<NarrativeEventEntity> NarrativeEvents => Set<NarrativeEventEntity>();
    public DbSet<TraitPaletteEntity> TraitPalettes => Set<TraitPaletteEntity>();
    public DbSet<StaticPageEntity> StaticPages => Set<StaticPageEntity>();
    public DbSet<ContentTreeEntity> ContentTrees => Set<ContentTreeEntity>();
    public DbSet<PageLayoutEntity> PageLayouts => Set<PageLayoutEntity>();
    public DbSet<StyleLibraryEntity> StyleLibraries => Set<StyleLibraryEntity>();
    public DbSet<WorldSchemaEntity> WorldSchemas => Set<WorldSchemaEntity>();
    public DbSet<DynamicsRunEntity> DynamicsRuns => Set<DynamicsRunEntity>();
    public DbSet<SummaryRevisionRunEntity> SummaryRevisionRuns => Set<SummaryRevisionRunEntity>();
    public DbSet<ConfigSlotEntity> ConfigSlots => Set<ConfigSlotEntity>();

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

        modelBuilder.Entity<Chronicle>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SimulationRunId);
        });

        modelBuilder.Entity<HistorianRun>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SimulationRunId);
            e.HasIndex(x => x.EntityId);
        });

        modelBuilder.Entity<EraNarrative>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SimulationRunId);
            e.HasIndex(x => x.EraId);
        });

        modelBuilder.Entity<ImageRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SimulationRunId);
            e.HasIndex(x => x.EntityId);
            e.HasIndex(x => x.Type);
        });

        modelBuilder.Entity<ApiCostEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SimulationRunId);
            e.HasIndex(x => x.TaskType);
            e.HasIndex(x => x.Model);
        });

        modelBuilder.Entity<NarrativeEventEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SimulationRunId);
            e.HasIndex(x => x.EraId);
            e.HasIndex(x => x.Tick);
        });

        modelBuilder.Entity<TraitPaletteEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProjectId, x.EntityKind }).IsUnique();
        });

        modelBuilder.Entity<StaticPageEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SimulationRunId);
            e.HasIndex(x => x.PageId);
        });

        modelBuilder.Entity<ContentTreeEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProjectId, x.SimulationRunId }).IsUnique();
        });

        modelBuilder.Entity<PageLayoutEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.SimulationRunId, x.PageId }).IsUnique();
        });

        modelBuilder.Entity<StyleLibraryEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProjectId).IsUnique();
        });

        modelBuilder.Entity<WorldSchemaEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProjectId).IsUnique();
        });

        modelBuilder.Entity<DynamicsRunEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SimulationRunId);
            e.HasIndex(x => x.RunId);
        });

        modelBuilder.Entity<SummaryRevisionRunEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SimulationRunId);
            e.HasIndex(x => x.RunId);
        });

        modelBuilder.Entity<ConfigSlotEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProjectId, x.ConfigType }).IsUnique();
        });
    }
}
