using System.Collections.ObjectModel;
using System.Windows.Input;
using TheCanonry.Desktop.Shared;
using TheCanonry.Persistence;
using TheCanonry.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace TheCanonry.Desktop.Illuminator;

internal sealed class EntityListItem : ViewModelBase
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Kind { get; init; } = "";
    public string Subtype { get; init; } = "";
    public string Status { get; init; } = "";
    public string Culture { get; init; } = "";
    public double Prominence { get; init; }
    public string Description { get; init; } = "";
    public bool HasEnrichment { get; init; }
}

internal sealed class EntityBrowserViewModel : ViewModelBase
{
    private readonly IDbContextFactory<CanonryDbContext> _dbFactory;
    private EntityListItem? _selectedEntity;
    private string _searchText = "";
    private string _kindFilter = "";
    private string _statusFilter = "";
    private int _totalCount;

    public EntityBrowserViewModel(IDbContextFactory<CanonryDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        Entities = new ObservableCollection<EntityListItem>();
        AvailableKinds = new ObservableCollection<string>();
        AvailableStatuses = new ObservableCollection<string>();

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        SearchCommand = new AsyncRelayCommand(RefreshAsync);

        _ = RefreshAsync();
    }

    public ObservableCollection<EntityListItem> Entities { get; }
    public ObservableCollection<string> AvailableKinds { get; }
    public ObservableCollection<string> AvailableStatuses { get; }

    public EntityListItem? SelectedEntity
    {
        get => _selectedEntity;
        set => SetProperty(ref _selectedEntity, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string KindFilter
    {
        get => _kindFilter;
        set
        {
            if (SetProperty(ref _kindFilter, value))
                _ = RefreshAsync();
        }
    }

    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
                _ = RefreshAsync();
        }
    }

    public int TotalCount
    {
        get => _totalCount;
        private set => SetProperty(ref _totalCount, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand SearchCommand { get; }

    private async Task RefreshAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        IQueryable<PersistedEntity> query = db.Entities;

        if (!string.IsNullOrWhiteSpace(KindFilter))
            query = query.Where(e => e.Kind == KindFilter);

        if (!string.IsNullOrWhiteSpace(StatusFilter))
            query = query.Where(e => e.Status == StatusFilter);

        if (!string.IsNullOrWhiteSpace(SearchText))
            query = query.Where(e => e.Name.Contains(SearchText) || e.Description.Contains(SearchText));

        TotalCount = await query.CountAsync();

        var entities = await query
            .OrderBy(e => e.Kind)
            .ThenBy(e => e.Name)
            .Take(500)
            .ToListAsync();

        Entities.Clear();
        foreach (var entity in entities)
        {
            Entities.Add(new EntityListItem
            {
                Id = entity.Id,
                Name = entity.Name,
                Kind = entity.Kind,
                Subtype = entity.Subtype,
                Status = entity.Status,
                Culture = entity.Culture,
                Prominence = entity.Prominence,
                Description = entity.Description,
                HasEnrichment = entity.EnrichmentJson is not null,
            });
        }

        // Populate filter options
        if (AvailableKinds.Count == 0)
        {
            var kinds = await db.Entities.Select(e => e.Kind).Distinct().OrderBy(k => k).ToListAsync();
            AvailableKinds.Clear();
            AvailableKinds.Add(""); // All
            foreach (var kind in kinds)
                AvailableKinds.Add(kind);
        }

        if (AvailableStatuses.Count == 0)
        {
            var statuses = await db.Entities.Select(e => e.Status).Distinct().OrderBy(s => s).ToListAsync();
            AvailableStatuses.Clear();
            AvailableStatuses.Add(""); // All
            foreach (var status in statuses)
                AvailableStatuses.Add(status);
        }
    }
}
