using System.Collections.ObjectModel;
using System.Windows.Input;
using TheCanonry.Desktop.Shared;
using TheCanonry.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TheCanonry.Desktop.Illuminator;

internal sealed class FieldCoverageItem : ViewModelBase
{
    public string FieldName { get; init; } = "";
    public int FilledCount { get; init; }
    public int TotalCount { get; init; }
    public double Percentage => TotalCount > 0 ? (double)FilledCount / TotalCount * 100 : 0;
}

internal sealed class CatalogViewModel : ViewModelBase
{
    private readonly IDbContextFactory<CanonryDbContext> _dbFactory;
    private int _imageCount;
    private int _entityCount;
    private double _completionPercentage;
    private double _imageCompletionPercentage;
    private bool _isAnalyzing;

    public CatalogViewModel(IDbContextFactory<CanonryDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        CoverageItems = new ObservableCollection<FieldCoverageItem>();
        ImageCoverageItems = new ObservableCollection<FieldCoverageItem>();

        RunAnalysisCommand = new AsyncRelayCommand(RunAnalysisAsync, () => !IsAnalyzing);

        _ = RunAnalysisAsync();
    }

    public ObservableCollection<FieldCoverageItem> CoverageItems { get; }
    public ObservableCollection<FieldCoverageItem> ImageCoverageItems { get; }

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

    public double ImageCompletionPercentage
    {
        get => _imageCompletionPercentage;
        private set => SetProperty(ref _imageCompletionPercentage, value);
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
            ImageCoverageItems.Clear();

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

            // Image catalog field coverage
            var images = await db.Images.ToListAsync();
            var totalImages = images.Count;

            if (totalImages > 0)
            {
                ImageCoverageItems.Add(new FieldCoverageItem
                {
                    FieldName = "Title",
                    FilledCount = images.Count(i => !string.IsNullOrWhiteSpace(i.Title)),
                    TotalCount = totalImages,
                });

                ImageCoverageItems.Add(new FieldCoverageItem
                {
                    FieldName = "Tags",
                    FilledCount = images.Count(i => !string.IsNullOrWhiteSpace(i.Tags)),
                    TotalCount = totalImages,
                });

                ImageCoverageItems.Add(new FieldCoverageItem
                {
                    FieldName = "Artistic Style",
                    FilledCount = images.Count(i => !string.IsNullOrWhiteSpace(i.ArtisticStyleId)),
                    TotalCount = totalImages,
                });

                ImageCoverageItems.Add(new FieldCoverageItem
                {
                    FieldName = "Composition Style",
                    FilledCount = images.Count(i => !string.IsNullOrWhiteSpace(i.CompositionStyleId)),
                    TotalCount = totalImages,
                });

                ImageCoverageItems.Add(new FieldCoverageItem
                {
                    FieldName = "Color Palette",
                    FilledCount = images.Count(i => !string.IsNullOrWhiteSpace(i.ColorPaletteId)),
                    TotalCount = totalImages,
                });

                var imgFilled = ImageCoverageItems.Sum(c => c.FilledCount);
                var imgPossible = ImageCoverageItems.Sum(c => c.TotalCount);
                ImageCompletionPercentage = imgPossible > 0 ? (double)imgFilled / imgPossible * 100 : 0;
            }
        }
        finally
        {
            IsAnalyzing = false;
        }
    }
}
