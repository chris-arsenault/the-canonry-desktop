using System.Text.Json;
using TheCanonry.ApiClients.Llm;
using TheCanonry.Illuminator.Enrichment;
using TheCanonry.Illuminator.Enrichment.Tasks;
using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Chronicle;

/// <summary>
/// Enrichment result data from chronicle generation.
/// </summary>
public sealed record ChronicleData
{
    public required string ChronicleId { get; init; }
    public required string Content { get; init; }
    public string? Summary { get; init; }
    public string? Title { get; init; }
    public IReadOnlyList<string>? TitleCandidates { get; init; }
}

/// <summary>
/// Multi-step chronicle generation task.
/// Steps: generate -> summary -> title -> image refs (each optional, controlled by step).
/// </summary>
public sealed class ChronicleTask : EnrichmentTaskBase
{
    private static readonly JsonSerializerOptions TitleParseOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
    };

    private readonly Func<Schema.Ids.EntityId, ChronicleGenerationContext?> _contextResolver;

    public override EnrichmentType TaskType => EnrichmentType.EntityChronicle;

    public ChronicleTask(
        TaskContext context,
        Func<Schema.Ids.EntityId, ChronicleGenerationContext?> contextResolver)
        : base(context)
    {
        _contextResolver = contextResolver;
    }

    public override async Task<EnrichmentResult> ExecuteAsync(
        EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        var genCtx = _contextResolver(job.TargetEntityId);
        if (genCtx is null)
            return new EnrichmentResult { Success = false, Error = $"No chronicle context for entity {job.TargetEntityId}" };

        var totalInput = 0;
        var totalOutput = 0;

        // Step 1: Generate chronicle content
        progress.Report(new TaskProgress("Generating chronicle...", 0.0));

        var genSys = ChroniclePrompts.BuildGenerationSystemPrompt(genCtx, "narrative");
        var genUser = ChroniclePrompts.BuildGenerationUserPrompt(genCtx);

        var genResponse = await CallLlmAsync(LlmCallType.ChronicleGeneration, genSys, genUser, ct);
        totalInput += genResponse.Usage.InputTokens;
        totalOutput += genResponse.Usage.OutputTokens;

        if (genResponse.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Generation failed: {genResponse.Error}" };

        var content = genResponse.Text.Trim();
        if (string.IsNullOrEmpty(content))
            return new EnrichmentResult { Success = false, Error = "Generation returned empty content" };

        // Step 2: Generate summary
        progress.Report(new TaskProgress("Generating summary...", 0.4));

        var sumSys = ChroniclePrompts.BuildSummarySystemPrompt();
        var sumUser = ChroniclePrompts.BuildSummaryUserPrompt(content);

        var sumResponse = await CallLlmAsync(LlmCallType.ChronicleSummary, sumSys, sumUser, ct);
        totalInput += sumResponse.Usage.InputTokens;
        totalOutput += sumResponse.Usage.OutputTokens;

        var summary = sumResponse.Error is null ? sumResponse.Text.Trim() : null;

        // Step 3: Generate title
        progress.Report(new TaskProgress("Generating title...", 0.6));

        var titleSys = ChroniclePrompts.BuildTitleSystemPrompt();
        var titleUser = ChroniclePrompts.BuildTitleUserPrompt(content, summary);

        var titleResponse = await CallLlmAsync(LlmCallType.ChronicleTitle, titleSys, titleUser, ct);
        totalInput += titleResponse.Usage.InputTokens;
        totalOutput += titleResponse.Usage.OutputTokens;

        string? title = null;
        IReadOnlyList<string>? titleCandidates = null;
        if (titleResponse.Error is null)
        {
            titleCandidates = ParseTitleCandidates(titleResponse.Text);
            title = titleCandidates.Count > 0 ? titleCandidates[0] : null;
        }

        // Step 4: Image refs (optional — caller can invoke separately)
        progress.Report(new TaskProgress("Chronicle complete.", 1.0));

        var chronicleId = $"chr_{job.TargetEntityId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        return new EnrichmentResult
        {
            Success = true,
            Data = new ChronicleData
            {
                ChronicleId = chronicleId,
                Content = content,
                Summary = summary,
                Title = title,
                TitleCandidates = titleCandidates,
            },
            TokenUsage = new TokenUsage(totalInput, totalOutput),
        };
    }

    private static IReadOnlyList<string> ParseTitleCandidates(string text)
    {
        try
        {
            var json = LlmResponseParser.ExtractJson(text);
            var candidates = JsonSerializer.Deserialize<List<string>>(json, TitleParseOptions);
            return candidates?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? [];
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return [];
        }
    }
}
