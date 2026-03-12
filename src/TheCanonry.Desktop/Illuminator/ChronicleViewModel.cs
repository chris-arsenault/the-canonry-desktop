namespace TheCanonry.Desktop.Illuminator;

using System.Collections.ObjectModel;
using System.Windows.Input;
using TheCanonry.Desktop.Shared;
using TheCanonry.Persistence;
using TheCanonry.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

public class ChronicleListItem : ViewModelBase
{
    private string _title = "";
    private string? _summary;
    private bool _isAccepted;

    public long Id { get; init; }
    public string Format { get; init; } = "";
    public DateTime CreatedAt { get; init; }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string? Summary
    {
        get => _summary;
        set => SetProperty(ref _summary, value);
    }

    public bool IsAccepted
    {
        get => _isAccepted;
        set => SetProperty(ref _isAccepted, value);
    }
}

public class ChronicleViewModel : ViewModelBase
{
    private readonly IDbContextFactory<CanonryDbContext> _dbFactory;
    private ChronicleListItem? _selectedChronicle;
    private string _content = "";
    private string _title = "";
    private string? _summary;
    private bool _isGenerating;

    public ChronicleViewModel(IDbContextFactory<CanonryDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        Chronicles = new ObservableCollection<ChronicleListItem>();

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        AcceptCommand = new AsyncRelayCommand(AcceptAsync, () => SelectedChronicle is not null);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => SelectedChronicle is not null);
    }

    public ObservableCollection<ChronicleListItem> Chronicles { get; }

    public ChronicleListItem? SelectedChronicle
    {
        get => _selectedChronicle;
        set
        {
            if (SetProperty(ref _selectedChronicle, value))
            {
                _ = LoadContentAsync();
                ((AsyncRelayCommand)AcceptCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)DeleteCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string Content
    {
        get => _content;
        private set => SetProperty(ref _content, value);
    }

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public string? Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    public bool IsGenerating
    {
        get => _isGenerating;
        private set => SetProperty(ref _isGenerating, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand AcceptCommand { get; }
    public ICommand DeleteCommand { get; }

    private async Task RefreshAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var chronicles = await db.Chronicles
            .OrderByDescending(c => c.CreatedAt)
            .Take(200)
            .ToListAsync();

        Chronicles.Clear();
        foreach (var c in chronicles)
        {
            Chronicles.Add(new ChronicleListItem
            {
                Id = c.Id,
                Title = c.Title ?? $"Chronicle #{c.Id}",
                Summary = c.Summary,
                Format = c.Format,
                CreatedAt = c.CreatedAt,
                IsAccepted = c.AcceptedAt.HasValue,
            });
        }
    }

    private async Task LoadContentAsync()
    {
        if (SelectedChronicle is null)
        {
            Content = "";
            Title = "";
            Summary = null;
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var chronicle = await db.Chronicles.FindAsync(SelectedChronicle.Id);
        if (chronicle is null) return;

        Content = chronicle.Content;
        Title = chronicle.Title ?? "";
        Summary = chronicle.Summary;
    }

    private async Task AcceptAsync()
    {
        if (SelectedChronicle is null) return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var chronicle = await db.Chronicles.FindAsync(SelectedChronicle.Id);
        if (chronicle is null) return;

        chronicle.AcceptedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        SelectedChronicle.IsAccepted = true;
    }

    private async Task DeleteAsync()
    {
        if (SelectedChronicle is null) return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var chronicle = await db.Chronicles.FindAsync(SelectedChronicle.Id);
        if (chronicle is null) return;

        db.Chronicles.Remove(chronicle);
        await db.SaveChangesAsync();

        var toRemove = SelectedChronicle;
        SelectedChronicle = null;
        Chronicles.Remove(toRemove);
    }
}
