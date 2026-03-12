namespace TheCanonry.Desktop.Illuminator;

using System.Collections.ObjectModel;
using System.Windows.Input;
using TheCanonry.Desktop.Shared;
using TheCanonry.Persistence;
using TheCanonry.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

public class ImageListItem : ViewModelBase
{
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
}

public class ImageCurationViewModel : ViewModelBase
{
    private readonly IDbContextFactory<CanonryDbContext> _dbFactory;
    private ImageListItem? _selectedImage;
    private string _typeFilter = "";
    private string _searchText = "";
    private int _totalCount;

    public ImageCurationViewModel(IDbContextFactory<CanonryDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        Images = new ObservableCollection<ImageListItem>();

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        DeleteImageCommand = new AsyncRelayCommand(DeleteImageAsync, () => SelectedImage is not null);
    }

    public ObservableCollection<ImageListItem> Images { get; }

    public ImageListItem? SelectedImage
    {
        get => _selectedImage;
        set
        {
            if (SetProperty(ref _selectedImage, value))
                ((AsyncRelayCommand)DeleteImageCommand).RaiseCanExecuteChanged();
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

    public int TotalCount
    {
        get => _totalCount;
        private set => SetProperty(ref _totalCount, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand DeleteImageCommand { get; }

    private async Task RefreshAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        IQueryable<ImageRecord> query = db.Images;

        if (!string.IsNullOrWhiteSpace(TypeFilter))
            query = query.Where(i => i.Type == TypeFilter);

        if (!string.IsNullOrWhiteSpace(SearchText))
            query = query.Where(i => i.Prompt.Contains(SearchText));

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
}
