namespace TheCanonry.Illuminator.Chronicle;

using TheCanonry.ApiClients.Llm;
using TheCanonry.Illuminator.Chronicle.PerspectiveSynthesis;
using TheCanonry.Illuminator.Chronicle.V2;
using TheCanonry.Illuminator.Enrichment;
using TheCanonry.Illuminator.Types;

/// <summary>
/// Result of a single pipeline step execution.
/// </summary>
public sealed record PipelineStepResult(string StepName, bool Success, string? Error = null);

/// <summary>
/// High-level orchestrator for the full chronicle pipeline.
/// Each step is independently callable for resume/retry.
/// Pipeline order: perspective synthesis → generation → copy-edit → summary → title → image refs.
/// </summary>
public class ChroniclePipelineOrchestrator
{
    private readonly ILlmClient _llm;

    public ChroniclePipelineOrchestrator(ILlmClient llm)
    {
        _llm = llm;
    }

    /// <summary>
    /// Run perspective synthesis.
    /// </summary>
    public async Task<PerspectiveSynthesisResult> RunPerspectiveAsync(
        PerspectiveSynthesisInput input,
        IProgress<TaskProgress>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report(new TaskProgress("Synthesizing perspective...", 0.1));
        var synthesizer = new PerspectiveSynthesizer(_llm);
        return await synthesizer.SynthesizeAsync(input, ct);
    }

    /// <summary>
    /// Run generation step — builds V2 prompts, calls LLM.
    /// </summary>
    public async Task<(string Content, string SystemPrompt, string UserPrompt, int InputTokens, int OutputTokens)> RunGenerationAsync(
        ChronicleGenerationContext ctx,
        ChronicleFormat format,
        StoryPromptContext? storyCtx = null,
        DocumentPromptContext? docCtx = null,
        string model = "claude-sonnet-4-6-20250514",
        int maxTokens = 8192,
        IProgress<TaskProgress>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report(new TaskProgress("Generating chronicle...", 0.3));

        var systemPrompt = format == ChronicleFormat.Story
            ? StoryPromptBuilder.GetSystemPrompt()
            : DocumentPromptBuilder.GetSystemPrompt();

        var userPrompt = format == ChronicleFormat.Story && storyCtx is not null
            ? StoryPromptBuilder.BuildUserPrompt(storyCtx)
            : docCtx is not null
                ? DocumentPromptBuilder.BuildUserPrompt(docCtx)
                : ChroniclePrompts.BuildGenerationUserPrompt(ctx);

        var request = new LlmRequest
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            Model = model,
            MaxTokens = maxTokens,
            Temperature = 1.0,
        };

        var response = await _llm.CompleteAsync(request, ct);
        if (response.Error is not null)
            throw new InvalidOperationException($"Generation failed: {response.Error}");

        return (response.Text, systemPrompt, userPrompt, response.Usage.InputTokens, response.Usage.OutputTokens);
    }

    /// <summary>
    /// Run copy-edit step.
    /// </summary>
    public async Task<(string Content, int InputTokens, int OutputTokens)> RunCopyEditAsync(
        string text,
        ChronicleFormat format,
        string styleName,
        int minWords,
        int maxWords,
        string? craftPosture = null,
        IReadOnlyDictionary<string, string>? narrativeVoice = null,
        IReadOnlyList<string>? motifs = null,
        string model = "claude-sonnet-4-6-20250514",
        IProgress<TaskProgress>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report(new TaskProgress("Copy-editing...", 0.5));

        var systemPrompt = format == ChronicleFormat.Story
            ? CopyEditPromptBuilder.GetStorySystemPrompt()
            : CopyEditPromptBuilder.GetDocumentSystemPrompt();

        var userPrompt = CopyEditPromptBuilder.BuildUserPrompt(text, styleName, minWords, maxWords,
            craftPosture, narrativeVoice, motifs);

        var request = new LlmRequest
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            Model = model,
            MaxTokens = 8192,
        };

        var response = await _llm.CompleteAsync(request, ct);
        if (response.Error is not null)
            throw new InvalidOperationException($"Copy-edit failed: {response.Error}");

        return (response.Text, response.Usage.InputTokens, response.Usage.OutputTokens);
    }

    /// <summary>
    /// Run summary generation.
    /// </summary>
    public async Task<(string Summary, int InputTokens, int OutputTokens)> RunSummaryAsync(
        string chronicleContent,
        string model = "claude-haiku-4-5-20251001",
        IProgress<TaskProgress>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report(new TaskProgress("Generating summary...", 0.7));

        var request = new LlmRequest
        {
            SystemPrompt = ChroniclePrompts.BuildSummarySystemPrompt(),
            UserPrompt = ChroniclePrompts.BuildSummaryUserPrompt(chronicleContent),
            Model = model,
            MaxTokens = 512,
        };

        var response = await _llm.CompleteAsync(request, ct);
        return (response.Text, response.Usage.InputTokens, response.Usage.OutputTokens);
    }

    /// <summary>
    /// Run title generation.
    /// </summary>
    public async Task<(string TitlesJson, int InputTokens, int OutputTokens)> RunTitleAsync(
        string chronicleContent,
        string? summary = null,
        string model = "claude-haiku-4-5-20251001",
        IProgress<TaskProgress>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report(new TaskProgress("Generating titles...", 0.8));

        var request = new LlmRequest
        {
            SystemPrompt = ChroniclePrompts.BuildTitleSystemPrompt(),
            UserPrompt = ChroniclePrompts.BuildTitleUserPrompt(chronicleContent, summary),
            Model = model,
            MaxTokens = 256,
        };

        var response = await _llm.CompleteAsync(request, ct);
        return (response.Text, response.Usage.InputTokens, response.Usage.OutputTokens);
    }

    /// <summary>
    /// Run image refs extraction.
    /// </summary>
    public async Task<(string ImageRefsJson, int InputTokens, int OutputTokens)> RunImageRefsAsync(
        string chronicleContent,
        IReadOnlyList<EntityContext> entities,
        string model = "claude-haiku-4-5-20251001",
        IProgress<TaskProgress>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report(new TaskProgress("Extracting image references...", 0.9));

        var request = new LlmRequest
        {
            SystemPrompt = ChroniclePrompts.BuildImageRefsSystemPrompt(),
            UserPrompt = ChroniclePrompts.BuildImageRefsUserPrompt(chronicleContent, entities),
            Model = model,
            MaxTokens = 2048,
        };

        var response = await _llm.CompleteAsync(request, ct);
        return (response.Text, response.Usage.InputTokens, response.Usage.OutputTokens);
    }
}
