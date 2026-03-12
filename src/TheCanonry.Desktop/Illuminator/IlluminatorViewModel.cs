namespace TheCanonry.Desktop.Illuminator;

using System.Collections.ObjectModel;
using System.Windows.Input;
using TheCanonry.Desktop.Shared;
using TheCanonry.Persistence;
using TheCanonry.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

public class EnrichmentJobItem : ViewModelBase
{
    private string _status = "";
    private string? _progressMessage;
    private double? _progressFraction;

    public long Id { get; init; }
    public string TaskType { get; init; } = "";
    public string TargetEntityId { get; init; } = "";
    public string EntityName { get; init; } = "";
    public DateTime QueuedAt { get; init; }
    public decimal EstimatedCost { get; init; }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string? ProgressMessage
    {
        get => _progressMessage;
        set => SetProperty(ref _progressMessage, value);
    }

    public double? ProgressFraction
    {
        get => _progressFraction;
        set => SetProperty(ref _progressFraction, value);
    }
}

public class IlluminatorViewModel : ViewModelBase
{
    private readonly IDbContextFactory<CanonryDbContext> _dbFactory;
    private EnrichmentJobItem? _selectedJob;
    private int _queueLength;
    private int _activeJobCount;
    private decimal _totalCost;

    public IlluminatorViewModel(IDbContextFactory<CanonryDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        EnrichmentJobs = new ObservableCollection<EnrichmentJobItem>();

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        CancelJobCommand = new RelayCommand<EnrichmentJobItem>(CancelJob);
        RetryJobCommand = new RelayCommand<EnrichmentJobItem>(RetryJob);
        ClearCompletedCommand = new AsyncRelayCommand(ClearCompletedAsync);
    }

    public ObservableCollection<EnrichmentJobItem> EnrichmentJobs { get; }

    public EnrichmentJobItem? SelectedJob
    {
        get => _selectedJob;
        set => SetProperty(ref _selectedJob, value);
    }

    public int QueueLength
    {
        get => _queueLength;
        private set => SetProperty(ref _queueLength, value);
    }

    public int ActiveJobCount
    {
        get => _activeJobCount;
        private set => SetProperty(ref _activeJobCount, value);
    }

    public decimal TotalCost
    {
        get => _totalCost;
        private set => SetProperty(ref _totalCost, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand CancelJobCommand { get; }
    public ICommand RetryJobCommand { get; }
    public ICommand ClearCompletedCommand { get; }

    private async Task RefreshAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var jobs = await db.EnrichmentJobs
            .OrderByDescending(j => j.QueuedAt)
            .Take(200)
            .ToListAsync();

        EnrichmentJobs.Clear();
        foreach (var job in jobs)
        {
            EnrichmentJobs.Add(new EnrichmentJobItem
            {
                Id = job.Id,
                TaskType = job.TaskType,
                TargetEntityId = job.TargetEntityId,
                Status = job.Status,
                QueuedAt = job.QueuedAt,
                EstimatedCost = job.EstimatedCost,
                ProgressMessage = job.ProgressMessage,
                ProgressFraction = job.ProgressFraction,
            });
        }

        QueueLength = jobs.Count(j => j.Status == "queued");
        ActiveJobCount = jobs.Count(j => j.Status == "running");
        TotalCost = jobs.Sum(j => j.EstimatedCost);
    }

    private void CancelJob(EnrichmentJobItem? job)
    {
        if (job is null) return;
        job.Status = "cancelled";
    }

    private void RetryJob(EnrichmentJobItem? job)
    {
        if (job is null) return;
        job.Status = "queued";
    }

    private async Task ClearCompletedAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var completed = await db.EnrichmentJobs
            .Where(j => j.Status == "completed" || j.Status == "cancelled")
            .ToListAsync();
        db.EnrichmentJobs.RemoveRange(completed);
        await db.SaveChangesAsync();
        await RefreshAsync();
    }
}
