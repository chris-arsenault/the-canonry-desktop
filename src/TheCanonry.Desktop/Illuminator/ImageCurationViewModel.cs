using System.Collections.ObjectModel;
using System.Windows.Input;
using TheCanonry.Desktop.Shared;
using TheCanonry.Persistence;
using TheCanonry.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace TheCanonry.Desktop.Illuminator;

internal sealed class ImageListItem : ViewModelBase
{
    private string? _title;
    private string? _tags;
    private string? _artisticStyleId;
    private string? _compositionStyleId;
    private string? _colorPaletteId;

    public long Id { get; init; }
    public string? EntityId { get; init; }
    public string EntityName { get; init; } = "";
    public string Prompt { get; init; } = "";
    public string Model { get; init; } = "";
    public int Width { get; init; }
    public int Height { get; init; }
    public string Aspect { get; init; } = "";
    public string Type { get; init; } = "";
    public string FilePath { get; init; } = "";
    public bool HasHq { get; init; }
    public DateTime CreatedAt { get; init; }

    public string? Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string? Tags
    {
        get => _tags;
        set => SetProperty(ref _tags, value);
    }

    public string? ArtisticStyleId
    {
        get => _artisticStyleId;
        set => SetProperty(ref _artisticStyleId, value);
    }

    public string? CompositionStyleId
    {
        get => _compositionStyleId;
        set => SetProperty(ref _compositionStyleId, value);
    }

    public string? ColorPaletteId
    {
        get => _colorPaletteId;
        set => SetProperty(ref _colorPaletteId, value);
    }
}

internal sealed class ImageCurationViewModel : ViewModelBase
{
    private readonly IDbContextFactory<CanonryDbContext> _dbFactory;
    private ImageListItem? _selectedImage;
    private string _typeFilter = "";
    private string _searchText = "";
    private string _catalogFilter = "";
    private int _totalCount;

    public ImageCurationViewModel(IDbContextFactory<CanonryDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        Images = new ObservableCollection<ImageListItem>();
        CatalogFilterOptions = ["", "missing-title", "missing-tags", "missing-style", "missing-any", "has-title"];

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        DeleteImageCommand = new AsyncRelayCommand(DeleteImageAsync, () => SelectedImage is not null);
        SaveCatalogCommand = new AsyncRelayCommand(SaveCatalogAsync, () => SelectedImage is not null);

        _ = RefreshAsync();
    }

    public ObservableCollection<ImageListItem> Images { get; }

    public ImageListItem? SelectedImage
    {
        get => _selectedImage;
        set
        {
            if (SetProperty(ref _selectedImage, value))
            {
                ((AsyncRelayCommand)DeleteImageCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)SaveCatalogCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string TypeFilter
    {
        get => _typeFilter;
        set
        {
            if (SetProperty(ref _typeFilter, value))
                _ = RefreshAsync();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string CatalogFilter
    {
        get => _catalogFilter;
        set
        {
            if (SetProperty(ref _catalogFilter, value))
                _ = RefreshAsync();
        }
    }

    public IReadOnlyList<string> CatalogFilterOptions { get; }

    public int TotalCount
    {
        get => _totalCount;
        private set => SetProperty(ref _totalCount, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand DeleteImageCommand { get; }
    public ICommand SaveCatalogCommand { get; }

    private async Task RefreshAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        IQueryable<ImageRecord> query = db.Images;

        if (!string.IsNullOrWhiteSpace(TypeFilter))
            query = query.Where(i => i.Type == TypeFilter);

        if (!string.IsNullOrWhiteSpace(SearchText))
            query = query.Where(i => i.Prompt.Contains(SearchText));

        // Catalog completeness filters
        query = CatalogFilter switch
        {
            "missing-title" => query.Where(i => i.Title == null || i.Title == ""),
            "missing-tags" => query.Where(i => i.Tags == null || i.Tags == ""),
            "missing-style" => query.Where(i =>
                (i.ArtisticStyleId == null || i.ArtisticStyleId == "") ||
                (i.CompositionStyleId == null || i.CompositionStyleId == "") ||
                (i.ColorPaletteId == null || i.ColorPaletteId == "")),
            "missing-any" => query.Where(i =>
                (i.Title == null || i.Title == "") ||
                (i.Tags == null || i.Tags == "") ||
                (i.ArtisticStyleId == null || i.ArtisticStyleId == "") ||
                (i.CompositionStyleId == null || i.CompositionStyleId == "") ||
                (i.ColorPaletteId == null || i.ColorPaletteId == "")),
            "has-title" => query.Where(i => i.Title != null && i.Title != ""),
            _ => query,
        };

        TotalCount = await query.CountAsync();

        var images = await query
            .OrderByDescending(i => i.CreatedAt)
            .Take(200)
            .ToListAsync();

        Images.Clear();
        foreach (var img in images)
        {
            Images.Add(new ImageListItem
            {
                Id = img.Id,
                EntityId = img.EntityId,
                Prompt = img.Prompt,
                Model = img.Model,
                Width = img.Width,
                Height = img.Height,
                Aspect = img.Aspect,
                Type = img.Type,
                FilePath = img.FilePath,
                HasHq = img.HqFilePath is not null,
                CreatedAt = img.CreatedAt,
                Title = img.Title,
                Tags = img.Tags,
                ArtisticStyleId = img.ArtisticStyleId,
                CompositionStyleId = img.CompositionStyleId,
                ColorPaletteId = img.ColorPaletteId,
            });
        }
    }

    private async Task DeleteImageAsync()
    {
        if (SelectedImage is null) return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var image = await db.Images.FindAsync(SelectedImage.Id);
        if (image is null) return;

        db.Images.Remove(image);
        await db.SaveChangesAsync();

        var toRemove = SelectedImage;
        SelectedImage = null;
        Images.Remove(toRemove);
    }

    private async Task SaveCatalogAsync()
    {
        if (SelectedImage is null) return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var image = await db.Images.FindAsync(SelectedImage.Id);
        if (image is null) return;

        image.Title = SelectedImage.Title;
        image.Tags = SelectedImage.Tags;
        image.ArtisticStyleId = SelectedImage.ArtisticStyleId;
        image.CompositionStyleId = SelectedImage.CompositionStyleId;
        image.ColorPaletteId = SelectedImage.ColorPaletteId;

        await db.SaveChangesAsync();
    }
}
