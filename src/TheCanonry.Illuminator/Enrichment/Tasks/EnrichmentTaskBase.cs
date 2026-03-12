using TheCanonry.ApiClients.Llm;
using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Tasks;

/// <summary>
/// Abstract base class for enrichment task executors.
/// Provides shared LLM call plumbing.
/// </summary>
public abstract class EnrichmentTaskBase : IEnrichmentTaskExecutor
{
    protected TaskContext Context { get; }

    protected EnrichmentTaskBase(TaskContext context)
    {
        Context = context;
    }

    public abstract EnrichmentType TaskType { get; }

    public abstract Task<EnrichmentResult> ExecuteAsync(
        EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct);

    /// <summary>
    /// Make an LLM call using the defaults for the given call type.
    /// </summary>
    protected async Task<LlmResponse> CallLlmAsync(
        LlmCallType callType, string systemPrompt, string userPrompt, CancellationToken ct)
    {
        var request = Context.BuildRequest(callType, systemPrompt, userPrompt);
        return await Context.LlmClient.CompleteAsync(request, ct);
    }
}
