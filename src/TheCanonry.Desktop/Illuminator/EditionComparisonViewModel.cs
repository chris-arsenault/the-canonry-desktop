using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using TheCanonry.Desktop.Shared;
using TheCanonry.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TheCanonry.Desktop.Illuminator;

internal sealed class EditionItem : ViewModelBase
{
    public string Label { get; init; } = "";
    public string Content { get; init; } = "";
    public int WordCount { get; init; }
    public string GeneratedAt { get; init; } = "";
}

internal sealed class EntityEditionItem : ViewModelBase
{
    public string EntityId { get; init; } = "";
    public string Name { get; init; } = "";
    public string Kind { get; init; } = "";
    public int EditionCount { get; init; }
}

internal sealed class EditionComparisonViewModel : ViewModelBase
{
    private readonly IDbContextFactory<CanonryDbContext> _dbFactory;
    private EntityEditionItem? _selectedEntity;
    private EditionItem? _leftEdition;
    private EditionItem? _rightEdition;
    private bool _isLoading;

    public EditionComparisonViewModel(IDbContextFactory<CanonryDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        Entities = new ObservableCollection<EntityEditionItem>();
        Editions = new ObservableCollection<EditionItem>();

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);

        _ = RefreshAsync();
    }

    public ObservableCollection<EntityEditionItem> Entities { get; }
    public ObservableCollection<EditionItem> Editions { get; }

    public EntityEditionItem? SelectedEntity
    {
        get => _selectedEntity;
        set
        {
            if (SetProperty(ref _selectedEntity, value))
                _ = LoadEditionsAsync();
        }
    }

    public EditionItem? LeftEdition
    {
        get => _leftEdition;
        set => SetProperty(ref _leftEdition, value);
    }

    public EditionItem? RightEdition
    {
        get => _rightEdition;
        set => SetProperty(ref _rightEdition, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public ICommand RefreshCommand { get; }

    private async Task RefreshAsync()
    {
        IsLoading = true;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            // Find entities that have historian runs (editions)
            var entityIds = await db.HistorianRuns
                .Select(h => h.EntityId)
                .Distinct()
                .ToListAsync();

            var entities = await db.Entities
                .Where(e => entityIds.Contains(e.Id))
                .ToListAsync();

            // Count editions per entity
            var editionCounts = await db.HistorianRuns
                .GroupBy(h => h.EntityId)
                .Select(g => new { EntityId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EntityId, x => x.Count);

            Entities.Clear();
            foreach (var entity in entities.OrderBy(e => e.Name))
            {
                editionCounts.TryGetValue(entity.Id, out var count);
                Entities.Add(new EntityEditionItem
                {
                    EntityId = entity.Id,
                    Name = entity.Name,
                    Kind = entity.Kind,
                    EditionCount = count,
                });
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadEditionsAsync()
    {
        Editions.Clear();
        LeftEdition = null;
        RightEdition = null;

        if (SelectedEntity is null) return;

        await using var db = await _dbFactory.CreateDbContextAsync();

        // Get the entity's current description as the baseline
        var entity = await db.Entities.FirstOrDefaultAsync(e => e.Id == SelectedEntity.EntityId);

        // Get all historian runs for this entity (each is an "edition")
        var runs = await db.HistorianRuns
            .Where(h => h.EntityId == SelectedEntity.EntityId)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync();

        // Baseline: current entity description
        if (entity is not null)
        {
            Editions.Add(new EditionItem
            {
                Label = "Current Description",
                Content = entity.Description,
                WordCount = CountWords(entity.Description),
                GeneratedAt = "Baseline",
            });
        }

        // Each historian run is an edition
        var editionIndex = 1;
        foreach (var run in runs)
        {
            // The historian run stores notes, but the enrichment JSON on the entity
            // may have description revisions. For now, show the historian notes as editions.
            var notesSummary = SummarizeNotes(run.Notes);
            Editions.Add(new EditionItem
            {
                Label = $"Edition {editionIndex} ({run.Status})",
                Content = notesSummary,
                WordCount = CountWords(notesSummary),
                GeneratedAt = run.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
            });
            editionIndex++;
        }

        // Auto-select first and last for comparison
        if (Editions.Count >= 2)
        {
            LeftEdition = Editions[0];
            RightEdition = Editions[^1];
        }
        else if (Editions.Count == 1)
        {
            LeftEdition = Editions[0];
        }
    }

    private static int CountWords(string text) =>
        string.IsNullOrWhiteSpace(text) ? 0 : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    private static string SummarizeNotes(string notesJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(notesJson);
            var notes = doc.RootElement;
            if (notes.ValueKind != JsonValueKind.Array || notes.GetArrayLength() == 0)
                return "(no notes)";

            var lines = new List<string>();
            foreach (var note in notes.EnumerateArray())
            {
                var type = note.TryGetProperty("Type", out var t) ? t.GetString() : "note";
                var text = note.TryGetProperty("Text", out var tx) ? tx.GetString() : "";
                var anchor = note.TryGetProperty("AnchorPhrase", out var a) ? a.GetString() : "";
                lines.Add($"[{type}] \"{anchor}\": {text}");
            }
            return string.Join("\n\n", lines);
        }
        catch (JsonException)
        {
            return notesJson;
        }
    }
}
