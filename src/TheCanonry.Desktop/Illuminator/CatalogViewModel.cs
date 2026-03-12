namespace TheCanonry.Desktop.Illuminator;

using System.Collections.ObjectModel;
using System.Windows.Input;
using TheCanonry.Desktop.Shared;
using TheCanonry.Persistence;
using Microsoft.EntityFrameworkCore;

public class FieldCoverageItem : ViewModelBase
{
    public string FieldName { get; init; } = "";
    public int FilledCount { get; init; }
    public int TotalCount { get; init; }
    public double Percentage => TotalCount > 0 ? (double)FilledCount / TotalCount * 100 : 0;
}

public class CatalogViewModel : ViewModelBase
{
    private readonly IDbContextFactory<CanonryDbContext> _dbFactory;
    private int _imageCount;
    private int _entityCount;
    private double _completionPercentage;
    private bool _isAnalyzing;

    public CatalogViewModel(IDbContextFactory<CanonryDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        CoverageItems = new ObservableCollection<FieldCoverageItem>();

        RunAnalysisCommand = new AsyncRelayCommand(RunAnalysisAsync, () => !IsAnalyzing);
    }

    public ObservableCollection<FieldCoverageItem> CoverageItems { get; }

    public int ImageCount
    {
        get => _imageCount;
        private set => SetProperty(ref _imageCount, value);
    }

    public int EntityCount
    {
        get => _entityCount;
        private set => SetProperty(ref _entityCount, value);
    }

    public double CompletionPercentage
    {
        get => _completionPercentage;
        private set => SetProperty(ref _completionPercentage, value);
    }

    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        private set
        {
            if (SetProperty(ref _isAnalyzing, value))
                ((AsyncRelayCommand)RunAnalysisCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand RunAnalysisCommand { get; }

    private async Task RunAnalysisAsync()
    {
        IsAnalyzing = true;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            EntityCount = await db.Entities.CountAsync();
            ImageCount = await db.Images.CountAsync();

            var entities = await db.Entities.ToListAsync();
            var totalEntities = entities.Count;

            CoverageItems.Clear();

            if (totalEntities > 0)
            {
                CoverageItems.Add(new FieldCoverageItem
                {
                    FieldName = "Name",
                    FilledCount = entities.Count(e => !string.IsNullOrWhiteSpace(e.Name)),
                    TotalCount = totalEntities,
                });

                CoverageItems.Add(new FieldCoverageItem
                {
                    FieldName = "Description",
                    FilledCount = entities.Count(e => !string.IsNullOrWhiteSpace(e.Description)),
                    TotalCount = totalEntities,
                });

                CoverageItems.Add(new FieldCoverageItem
                {
                    FieldName = "Summary",
                    FilledCount = entities.Count(e => !string.IsNullOrWhiteSpace(e.Summary)),
                    TotalCount = totalEntities,
                });

                CoverageItems.Add(new FieldCoverageItem
                {
                    FieldName = "Enrichment",
                    FilledCount = entities.Count(e => e.EnrichmentJson is not null),
                    TotalCount = totalEntities,
                });

                CoverageItems.Add(new FieldCoverageItem
                {
                    FieldName = "Culture",
                    FilledCount = entities.Count(e => !string.IsNullOrWhiteSpace(e.Culture)),
                    TotalCount = totalEntities,
                });

                // Images per entity
                var entitiesWithImages = await db.Images
                    .Where(i => i.EntityId != null)
                    .Select(i => i.EntityId)
                    .Distinct()
                    .CountAsync();

                CoverageItems.Add(new FieldCoverageItem
                {
                    FieldName = "Has Image",
                    FilledCount = entitiesWithImages,
                    TotalCount = totalEntities,
                });

                var totalFilled = CoverageItems.Sum(c => c.FilledCount);
                var totalPossible = CoverageItems.Sum(c => c.TotalCount);
                CompletionPercentage = totalPossible > 0 ? (double)totalFilled / totalPossible * 100 : 0;
            }
        }
        finally
        {
            IsAnalyzing = false;
        }
    }
}
