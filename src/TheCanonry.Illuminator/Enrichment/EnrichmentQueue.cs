using System.Threading.Channels;
using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment;

/// <summary>
/// A channel-based enrichment job queue with concurrency control and lifecycle events.
/// Jobs are enqueued as IDs; the queue reads them, dispatches to the appropriate
/// executor, and manages job lifecycle transitions.
/// </summary>
public sealed class EnrichmentQueue
{
    private readonly Channel<long> _channel = Channel.CreateUnbounded<long>(
        new UnboundedChannelOptions { SingleReader = false });

    private readonly Dictionary<long, EnrichmentJob> _jobs = new();
    private readonly Dictionary<EnrichmentType, IEnrichmentTaskExecutor> _executors = new();
    private readonly object _lock = new();

    // Lifecycle events
    public event Action<EnrichmentJob>? JobEnqueued;
    public event Action<EnrichmentJob>? JobStarted;
    public event Action<EnrichmentJob>? JobCompleted;
    public event Action<EnrichmentJob>? JobFailed;
    public event Action<EnrichmentJob>? JobCancelled;

    /// <summary>
    /// Register a task executor for a given enrichment type.
    /// </summary>
    public void RegisterExecutor(IEnrichmentTaskExecutor executor)
    {
        _executors[executor.TaskType] = executor;
    }

    /// <summary>
    /// Enqueue a job for processing.
    /// </summary>
    public async ValueTask EnqueueAsync(EnrichmentJob job)
    {
        lock (_lock)
        {
            _jobs[job.Id] = job;
        }
        await _channel.Writer.WriteAsync(job.Id);
        JobEnqueued?.Invoke(job);
    }

    /// <summary>
    /// Get a job by ID.
    /// </summary>
    public EnrichmentJob? GetJob(long jobId)
    {
        lock (_lock)
        {
            return _jobs.GetValueOrDefault(jobId);
        }
    }

    /// <summary>
    /// Get all tracked jobs.
    /// </summary>
    public IReadOnlyList<EnrichmentJob> GetAllJobs()
    {
        lock (_lock)
        {
            return _jobs.Values.ToList();
        }
    }

    /// <summary>
    /// Process jobs from the channel with bounded concurrency.
    /// Runs until the cancellation token is triggered.
    /// </summary>
    public async Task ProcessAsync(int maxConcurrency, CancellationToken ct)
    {
        using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        await foreach (var jobId in _channel.Reader.ReadAllAsync(ct))
        {
            EnrichmentJob? job;
            lock (_lock)
            {
                _jobs.TryGetValue(jobId, out job);
            }
            if (job is null) continue;
            if (job.Status == JobStatus.Cancelled) continue;

            await semaphore.WaitAsync(ct);

            _ = Task.Run(async () =>
            {
                try
                {
                    await ExecuteJobAsync(job, ct);
                }
                finally
                {
                    semaphore.Release();
                }
            }, ct);
        }
    }

    private async Task ExecuteJobAsync(EnrichmentJob job, CancellationToken ct)
    {
        if (!_executors.TryGetValue(job.TaskType, out var executor))
        {
            job.MarkFailed(new InvalidOperationException(
                $"No executor registered for enrichment type {job.TaskType}"));
            JobFailed?.Invoke(job);
            return;
        }

        job.MarkRunning();
        JobStarted?.Invoke(job);

        using var jobCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var progress = new Progress<TaskProgress>(p => job.ReportProgress(p.Message, p.Fraction));

        try
        {
            var result = await executor.ExecuteAsync(job, progress, jobCts.Token);

            if (ct.IsCancellationRequested)
            {
                job.MarkCancelled();
                JobCancelled?.Invoke(job);
                return;
            }

            if (result.Success)
            {
                job.MarkCompleted(result.TokenUsage);
                JobCompleted?.Invoke(job);
            }
            else
            {
                job.MarkFailed(new InvalidOperationException(result.Error ?? "Task returned failure"));
                JobFailed?.Invoke(job);
            }
        }
        catch (OperationCanceledException)
        {
            job.MarkCancelled();
            JobCancelled?.Invoke(job);
        }
        catch (Exception ex)
        {
            job.MarkFailed(ex);
            JobFailed?.Invoke(job);
        }
    }
}
