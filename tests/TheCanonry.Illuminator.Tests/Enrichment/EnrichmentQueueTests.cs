using TheCanonry.ApiClients.Llm;
using TheCanonry.Illuminator.Enrichment;
using TheCanonry.Illuminator.Types;
using TheCanonry.Schema.Ids;

namespace TheCanonry.Illuminator.Tests.Enrichment;

public sealed class EnrichmentQueueTests
{
    private static EnrichmentJob MakeJob(
        EnrichmentType type = EnrichmentType.Description,
        string entityId = "e1") =>
        new(type, new EntityId(entityId), new SimulationSlotId(1));

    private sealed class StubExecutor : IEnrichmentTaskExecutor
    {
        public EnrichmentType TaskType { get; }
        private readonly Func<EnrichmentJob, CancellationToken, Task<EnrichmentResult>> _handler;

        public StubExecutor(
            EnrichmentType type,
            Func<EnrichmentJob, CancellationToken, Task<EnrichmentResult>>? handler = null)
        {
            TaskType = type;
            _handler = handler ?? ((_, _) => Task.FromResult(new EnrichmentResult
            {
                Success = true,
                TokenUsage = new TokenUsage(10, 20),
            }));
        }

        public Task<EnrichmentResult> ExecuteAsync(
            EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct) =>
            _handler(job, ct);
    }

    [Fact]
    public void NewJob_HasQueuedStatus()
    {
        var job = MakeJob();
        Assert.Equal(JobStatus.Queued, job.Status);
        Assert.NotEqual(0, job.Id);
    }

    [Fact]
    public void MarkRunning_TransitionsToRunning()
    {
        var job = MakeJob();
        job.MarkRunning();
        Assert.Equal(JobStatus.Running, job.Status);
        Assert.NotNull(job.StartedAt);
        Assert.Equal(1, job.AttemptCount);
    }

    [Fact]
    public void MarkCompleted_TransitionsToCompleted()
    {
        var job = MakeJob();
        job.MarkRunning();
        job.MarkCompleted(new TokenUsage(100, 200));
        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.NotNull(job.CompletedAt);
        Assert.Equal(100, job.InputTokens);
        Assert.Equal(200, job.OutputTokens);
    }

    [Fact]
    public void MarkFailed_CapturesException()
    {
        var job = MakeJob();
        job.MarkRunning();
        var ex = new InvalidOperationException("test error");
        job.MarkFailed(ex);
        Assert.Equal(JobStatus.Failed, job.Status);
        Assert.Equal("test error", job.ErrorMessage);
        Assert.Contains("InvalidOperationException", job.ErrorDetail);
    }

    [Fact]
    public void MarkCancelled_TransitionsToCancelled()
    {
        var job = MakeJob();
        job.MarkRunning();
        job.MarkCancelled();
        Assert.Equal(JobStatus.Cancelled, job.Status);
        Assert.NotNull(job.CompletedAt);
    }

    [Fact]
    public async Task Queue_ProcessesJobSuccessfully()
    {
        var queue = new EnrichmentQueue();
        queue.RegisterExecutor(new StubExecutor(EnrichmentType.Description));

        var job = MakeJob();
        EnrichmentJob? completedJob = null;
        queue.JobCompleted += j => completedJob = j;

        using var cts = new CancellationTokenSource();
        var processTask = Task.Run(() => queue.ProcessAsync(1, cts.Token));

        await queue.EnqueueAsync(job);

        // Wait for completion
        for (var i = 0; i < 50 && completedJob is null; i++)
            await Task.Delay(50);

        await cts.CancelAsync();
        try { await processTask; } catch (OperationCanceledException) { }

        Assert.NotNull(completedJob);
        Assert.Equal(JobStatus.Completed, completedJob.Status);
        Assert.Equal(10, completedJob.InputTokens);
    }

    [Fact]
    public async Task Queue_ReportsFailureWhenExecutorThrows()
    {
        var queue = new EnrichmentQueue();
        queue.RegisterExecutor(new StubExecutor(
            EnrichmentType.Description,
            (_, _) => throw new InvalidOperationException("boom")));

        var job = MakeJob();
        EnrichmentJob? failedJob = null;
        queue.JobFailed += j => failedJob = j;

        using var cts = new CancellationTokenSource();
        var processTask = Task.Run(() => queue.ProcessAsync(1, cts.Token));

        await queue.EnqueueAsync(job);

        for (var i = 0; i < 50 && failedJob is null; i++)
            await Task.Delay(50);

        await cts.CancelAsync();
        try { await processTask; } catch (OperationCanceledException) { }

        Assert.NotNull(failedJob);
        Assert.Equal(JobStatus.Failed, failedJob.Status);
        Assert.Equal("boom", failedJob.ErrorMessage);
    }

    [Fact]
    public async Task Queue_ProcessesFIFO()
    {
        var processed = new List<long>();
        var queue = new EnrichmentQueue();
        queue.RegisterExecutor(new StubExecutor(
            EnrichmentType.Description,
            async (job, _) =>
            {
                lock (processed) { processed.Add(job.Id); }
                await Task.Delay(10);
                return new EnrichmentResult { Success = true };
            }));

        var job1 = MakeJob();
        var job2 = MakeJob(entityId: "e2");
        var job3 = MakeJob(entityId: "e3");

        using var cts = new CancellationTokenSource();
        // maxConcurrency=1 ensures strict ordering
        var processTask = Task.Run(() => queue.ProcessAsync(1, cts.Token));

        await queue.EnqueueAsync(job1);
        await queue.EnqueueAsync(job2);
        await queue.EnqueueAsync(job3);

        for (var i = 0; i < 100 && processed.Count < 3; i++)
            await Task.Delay(50);

        await cts.CancelAsync();
        try { await processTask; } catch (OperationCanceledException) { }

        Assert.Equal(3, processed.Count);
        Assert.Equal(job1.Id, processed[0]);
        Assert.Equal(job2.Id, processed[1]);
        Assert.Equal(job3.Id, processed[2]);
    }

    [Fact]
    public async Task Queue_CancellationStopsProcessing()
    {
        var started = new TaskCompletionSource();
        var queue = new EnrichmentQueue();
        queue.RegisterExecutor(new StubExecutor(
            EnrichmentType.Description,
            async (_, ct) =>
            {
                started.SetResult();
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
                return new EnrichmentResult { Success = true };
            }));

        var job = MakeJob();
        EnrichmentJob? cancelledJob = null;
        queue.JobCancelled += j => cancelledJob = j;

        using var cts = new CancellationTokenSource();
        var processTask = Task.Run(() => queue.ProcessAsync(1, cts.Token));

        await queue.EnqueueAsync(job);
        await started.Task;

        await cts.CancelAsync();
        try { await processTask; } catch (OperationCanceledException) { }

        // Allow time for the background task cancellation to propagate
        for (var i = 0; i < 50 && cancelledJob is null; i++)
            await Task.Delay(50);

        Assert.NotNull(cancelledJob);
        Assert.Equal(JobStatus.Cancelled, cancelledJob.Status);
    }

    [Fact]
    public async Task Queue_ConcurrentProcessingUsedSemaphore()
    {
        var concurrentCount = 0;
        var maxConcurrent = 0;
        var allDone = new TaskCompletionSource();
        var completed = 0;
        const int totalJobs = 4;

        var queue = new EnrichmentQueue();
        queue.RegisterExecutor(new StubExecutor(
            EnrichmentType.Description,
            async (_, _) =>
            {
                var current = Interlocked.Increment(ref concurrentCount);
                var maxSoFar = maxConcurrent;
                while (current > maxSoFar)
                {
                    Interlocked.CompareExchange(ref maxConcurrent, current, maxSoFar);
                    maxSoFar = maxConcurrent;
                }

                await Task.Delay(100);
                Interlocked.Decrement(ref concurrentCount);

                if (Interlocked.Increment(ref completed) == totalJobs)
                    allDone.TrySetResult();

                return new EnrichmentResult { Success = true };
            }));

        using var cts = new CancellationTokenSource();
        var processTask = Task.Run(() => queue.ProcessAsync(2, cts.Token));

        for (var i = 0; i < totalJobs; i++)
            await queue.EnqueueAsync(MakeJob(entityId: $"e{i}"));

        await Task.WhenAny(allDone.Task, Task.Delay(5000));

        await cts.CancelAsync();
        try { await processTask; } catch (OperationCanceledException) { }

        Assert.Equal(totalJobs, completed);
        Assert.True(maxConcurrent <= 2, $"Max concurrent was {maxConcurrent}, expected <= 2");
        Assert.True(maxConcurrent >= 2, $"Max concurrent was {maxConcurrent}, expected >= 2 (parallelism not reached)");
    }

    [Fact]
    public async Task Queue_FiresEnqueuedEvent()
    {
        var queue = new EnrichmentQueue();
        EnrichmentJob? enqueuedJob = null;
        queue.JobEnqueued += j => enqueuedJob = j;

        var job = MakeJob();
        await queue.EnqueueAsync(job);

        Assert.NotNull(enqueuedJob);
        Assert.Equal(job.Id, enqueuedJob.Id);
    }

    [Fact]
    public async Task Queue_FailsGracefullyWithNoExecutor()
    {
        var queue = new EnrichmentQueue();
        // Deliberately do not register any executor

        var job = MakeJob();
        EnrichmentJob? failedJob = null;
        queue.JobFailed += j => failedJob = j;

        using var cts = new CancellationTokenSource();
        var processTask = Task.Run(() => queue.ProcessAsync(1, cts.Token));

        await queue.EnqueueAsync(job);

        for (var i = 0; i < 50 && failedJob is null; i++)
            await Task.Delay(50);

        await cts.CancelAsync();
        try { await processTask; } catch (OperationCanceledException) { }

        Assert.NotNull(failedJob);
        Assert.Equal(JobStatus.Failed, failedJob.Status);
        Assert.Contains("No executor registered", failedJob.ErrorMessage);
    }
}
