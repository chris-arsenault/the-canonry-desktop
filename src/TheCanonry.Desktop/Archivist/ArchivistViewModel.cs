namespace TheCanonry.Desktop.Archivist;

using System.Collections.ObjectModel;
using System.Windows.Input;
using TheCanonry.Desktop.Shared;
using TheCanonry.Persistence;
using TheCanonry.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

public class ArchivistEntityItem : ViewModelBase
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Kind { get; init; } = "";
    public string Status { get; init; } = "";
    public int RelationshipCount { get; init; }
}

public class RelationshipDisplayItem
{
    public string SourceName { get; init; } = "";
    public string TargetName { get; init; } = "";
    public string Kind { get; init; } = "";
    public double Strength { get; init; }
    public string Category { get; init; } = "";
}

public class ArchivistViewModel : ViewModelBase
{
    private readonly IDbContextFactory<CanonryDbContext> _dbFactory;
    private ArchivistEntityItem? _selectedEntity;
    private string _searchText = "";
    private int _totalEntities;
    private int _totalRelationships;
    private int _kindCount;

    public ArchivistViewModel(IDbContextFactory<CanonryDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        Entities = new ObservableCollection<ArchivistEntityItem>();
        Relationships = new ObservableCollection<RelationshipDisplayItem>();

        SearchCommand = new AsyncRelayCommand(SearchAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshStatsAsync);
        FocusEntityCommand = new AsyncRelayCommand(LoadRelationshipsAsync, () => SelectedEntity is not null);
    }

    public ObservableCollection<ArchivistEntityItem> Entities { get; }
    public ObservableCollection<RelationshipDisplayItem> Relationships { get; }

    public ArchivistEntityItem? SelectedEntity
    {
        get => _selectedEntity;
        set
        {
            if (SetProperty(ref _selectedEntity, value))
            {
                ((AsyncRelayCommand)FocusEntityCommand).RaiseCanExecuteChanged();
                _ = LoadRelationshipsAsync();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public int TotalEntities
    {
        get => _totalEntities;
        private set => SetProperty(ref _totalEntities, value);
    }

    public int TotalRelationships
    {
        get => _totalRelationships;
        private set => SetProperty(ref _totalRelationships, value);
    }

    public int KindCount
    {
        get => _kindCount;
        private set => SetProperty(ref _kindCount, value);
    }

    public ICommand SearchCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand FocusEntityCommand { get; }

    private async Task SearchAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        IQueryable<PersistedEntity> query = db.Entities;

        if (!string.IsNullOrWhiteSpace(SearchText))
            query = query.Where(e => e.Name.Contains(SearchText));

        var entities = await query
            .OrderBy(e => e.Name)
            .Take(300)
            .ToListAsync();

        // Pre-fetch relationship counts
        var entityIds = entities.Select(e => e.Id).ToHashSet();
        var relCounts = await db.Relationships
            .Where(r => entityIds.Contains(r.SourceId) || entityIds.Contains(r.TargetId))
            .GroupBy(r => entityIds.Contains(r.SourceId) ? r.SourceId : r.TargetId)
            .Select(g => new { EntityId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.EntityId, g => g.Count);

        Entities.Clear();
        foreach (var entity in entities)
        {
            Entities.Add(new ArchivistEntityItem
            {
                Id = entity.Id,
                Name = entity.Name,
                Kind = entity.Kind,
                Status = entity.Status,
                RelationshipCount = relCounts.GetValueOrDefault(entity.Id, 0),
            });
        }
    }

    private async Task RefreshStatsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        TotalEntities = await db.Entities.CountAsync();
        TotalRelationships = await db.Relationships.CountAsync();
        KindCount = await db.Entities.Select(e => e.Kind).Distinct().CountAsync();
    }

    private async Task LoadRelationshipsAsync()
    {
        Relationships.Clear();
        if (SelectedEntity is null) return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var entityId = SelectedEntity.Id;

        var rels = await db.Relationships
            .Where(r => r.SourceId == entityId || r.TargetId == entityId)
            .Take(100)
            .ToListAsync();

        // Resolve entity names
        var relatedIds = rels
            .SelectMany(r => new[] { r.SourceId, r.TargetId })
            .Where(id => id != entityId)
            .Distinct()
            .ToList();

        var nameMap = await db.Entities
            .Where(e => relatedIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Name);
        nameMap[entityId] = SelectedEntity.Name;

        foreach (var rel in rels)
        {
            Relationships.Add(new RelationshipDisplayItem
            {
                SourceName = nameMap.GetValueOrDefault(rel.SourceId, rel.SourceId),
                TargetName = nameMap.GetValueOrDefault(rel.TargetId, rel.TargetId),
                Kind = rel.Kind,
                Strength = rel.Strength,
                Category = rel.Category,
            });
        }
    }
}
