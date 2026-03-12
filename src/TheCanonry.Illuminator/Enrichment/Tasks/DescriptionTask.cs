using System.Text.Json;
using TheCanonry.ApiClients.Llm;
using TheCanonry.Illuminator.Enrichment.Prompts;
using TheCanonry.Illuminator.Types;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

namespace TheCanonry.Illuminator.Enrichment.Tasks;

/// <summary>
/// Enrichment result data for the description chain.
/// </summary>
public sealed record DescriptionData
{
    public required string Summary { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<string> Aliases { get; init; }
    public required string VisualThesis { get; init; }
    public required IReadOnlyList<string> VisualTraits { get; init; }
}

/// <summary>
/// Three-step description enrichment task:
/// 1. Narrative -> summary, description, aliases
/// 2. Visual Thesis -> one-sentence visual signal
/// 3. Visual Traits -> 2-4 supporting visual details
/// </summary>
public sealed class DescriptionTask : EnrichmentTaskBase
{
    private readonly Func<EntityId, Entity?> _entityResolver;
    private readonly string _kindInstructions;

    public override EnrichmentType TaskType => EnrichmentType.Description;

    public DescriptionTask(
        TaskContext context,
        Func<EntityId, Entity?> entityResolver,
        string kindInstructions = "Focus on physical form, posture, and distinguishing features.")
        : base(context)
    {
        _entityResolver = entityResolver;
        _kindInstructions = kindInstructions;
    }

    public override async Task<EnrichmentResult> ExecuteAsync(
        EnrichmentJob job, IProgress<TaskProgress> progress, CancellationToken ct)
    {
        var entity = _entityResolver(job.TargetEntityId);
        if (entity is null)
            return new EnrichmentResult { Success = false, Error = $"Entity {job.TargetEntityId} not found" };

        var totalUsage = new TokenUsageAccumulator();

        // Step 1: Narrative
        progress.Report(new TaskProgress("Generating narrative...", 0.0));

        var entityContext = $"Entity: {entity.Name} ({entity.Kind}, {entity.Subtype})\nStatus: {entity.Status}\nCulture: {entity.Culture}";
        var (narrativeSys, narrativeUser) = DescriptionPrompts.BuildNarrativePrompt(entity, entityContext);

        var narrativeResponse = await CallLlmAsync(
            LlmCallType.DescriptionNarrative, narrativeSys, narrativeUser, ct);
        totalUsage.Add(narrativeResponse.Usage);

        if (narrativeResponse.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Narrative step failed: {narrativeResponse.Error}" };

        var (summary, description, aliases) = ParseNarrativeResponse(narrativeResponse.Text);
        if (string.IsNullOrEmpty(summary) || string.IsNullOrEmpty(description))
            return new EnrichmentResult { Success = false, Error = "Narrative step returned empty summary or description" };

        // Step 2: Visual Thesis
        progress.Report(new TaskProgress("Generating visual thesis...", 0.33));

        var (thesisSys, thesisUser) = DescriptionPrompts.BuildVisualThesisPrompt(
            entity, description, _kindInstructions);

        var thesisResponse = await CallLlmAsync(
            LlmCallType.DescriptionVisualThesis, thesisSys, thesisUser, ct);
        totalUsage.Add(thesisResponse.Usage);

        if (thesisResponse.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Visual thesis step failed: {thesisResponse.Error}" };

        var visualThesis = thesisResponse.Text.Trim();
        if (string.IsNullOrEmpty(visualThesis))
            return new EnrichmentResult { Success = false, Error = "Visual thesis step returned empty response" };

        // Step 3: Visual Traits
        progress.Report(new TaskProgress("Generating visual traits...", 0.66));

        var (traitsSys, traitsUser) = DescriptionPrompts.BuildVisualTraitsPrompt(
            entity, visualThesis, description, _kindInstructions);

        var traitsResponse = await CallLlmAsync(
            LlmCallType.DescriptionVisualTraits, traitsSys, traitsUser, ct);
        totalUsage.Add(traitsResponse.Usage);

        if (traitsResponse.Error is not null)
            return new EnrichmentResult { Success = false, Error = $"Visual traits step failed: {traitsResponse.Error}" };

        var visualTraits = ParseVisualTraits(traitsResponse.Text);

        progress.Report(new TaskProgress("Description chain complete.", 1.0));

        return new EnrichmentResult
        {
            Success = true,
            Data = new DescriptionData
            {
                Summary = summary,
                Description = description,
                Aliases = aliases,
                VisualThesis = visualThesis,
                VisualTraits = visualTraits,
            },
            TokenUsage = totalUsage.ToTokenUsage(),
        };
    }

    private static (string Summary, string Description, IReadOnlyList<string> Aliases) ParseNarrativeResponse(string text)
    {
        try
        {
            var json = LlmResponseParser.ExtractJson(text);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var summary = root.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "";
            var description = root.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
            var aliases = new List<string>();
            if (root.TryGetProperty("aliases", out var a) && a.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in a.EnumerateArray())
                {
                    var alias = item.GetString();
                    if (!string.IsNullOrWhiteSpace(alias))
                        aliases.Add(alias.Trim());
                }
            }

            return (summary.Trim(), description.Trim(), aliases);
        }
        catch
        {
            return ("", "", []);
        }
    }

    private static IReadOnlyList<string> ParseVisualTraits(string text)
    {
        return text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimStart('-', '*', '\u2022', ' ').Trim())
            .Where(line => line.Length > 0)
            .ToList();
    }

    /// <summary>
    /// Accumulates token usage across multiple LLM calls.
    /// </summary>
    private sealed class TokenUsageAccumulator
    {
        private int _input;
        private int _output;

        public void Add(TokenUsage usage)
        {
            _input += usage.InputTokens;
            _output += usage.OutputTokens;
        }

        public TokenUsage ToTokenUsage() => new(_input, _output);
    }
}
