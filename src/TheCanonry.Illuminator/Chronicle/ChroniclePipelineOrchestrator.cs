using TheCanonry.ApiClients.Llm;
using TheCanonry.Illuminator.Chronicle.PerspectiveSynthesis;
using TheCanonry.Illuminator.Chronicle.V2;
using TheCanonry.Illuminator.Enrichment;
using TheCanonry.Illuminator.Enrichment.Prompts;
using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Chronicle;

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
            ? StoryPromptBuilder.SystemPrompt
            : DocumentPromptBuilder.SystemPrompt;

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
    /// Run compare step — analyzes multiple draft versions and produces a comparison report.
    /// Returns the full report text and extracted combine instructions.
    /// </summary>
    public async Task<(string Report, string? CombineInstructions, int InputTokens, int OutputTokens)> RunCompareAsync(
        IReadOnlyList<(string Label, string Content)> versions,
        string narrativeStyleName,
        bool isDocument,
        string? narrativeStyleBlock = null,
        string? worldFactsBlock = null,
        string? narrativeDirection = null,
        string model = "claude-sonnet-4-6-20250514",
        IProgress<TaskProgress>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report(new TaskProgress("Comparing versions...", 0.4));

        var systemPrompt = ChroniclePrompts.BuildCompareSystemPrompt(isDocument);
        var userPrompt = ChroniclePrompts.BuildCompareUserPrompt(
            versions, narrativeStyleName, isDocument, narrativeStyleBlock, worldFactsBlock, narrativeDirection);

        var request = new LlmRequest
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            Model = model,
            MaxTokens = 4096,
            Temperature = 0.3,
        };

        var response = await _llm.CompleteAsync(request, ct);
        if (response.Error is not null)
            throw new InvalidOperationException($"Compare failed: {response.Error}");

        // Extract combine instructions from the report (fuzzy heading match)
        var combineInstructions = ExtractCombineInstructions(response.Text);

        return (response.Text, combineInstructions, response.Usage.InputTokens, response.Usage.OutputTokens);
    }

    /// <summary>
    /// Run combine step — merges multiple draft versions into one superior version.
    /// </summary>
    public async Task<(string Content, string SystemPrompt, string UserPrompt, int InputTokens, int OutputTokens)> RunCombineAsync(
        IReadOnlyList<(string Label, string Content)> versions,
        string narrativeStyleName,
        bool isDocument,
        string generationSystemPrompt,
        string? craftPosture = null,
        string? narrativeDirection = null,
        string? combineInstructions = null,
        string model = "claude-sonnet-4-6-20250514",
        int maxTokens = 8192,
        IProgress<TaskProgress>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report(new TaskProgress("Combining versions...", 0.5));

        var systemPrompt = ChroniclePrompts.BuildCombineSystemPrompt(isDocument);
        var userPrompt = ChroniclePrompts.BuildCombineUserPrompt(
            versions, narrativeStyleName, isDocument, generationSystemPrompt,
            craftPosture, narrativeDirection, combineInstructions);

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
            throw new InvalidOperationException($"Combine failed: {response.Error}");

        return (response.Text, systemPrompt, userPrompt, response.Usage.InputTokens, response.Usage.OutputTokens);
    }

    /// <summary>
    /// Extract combine instructions from a compare report by finding the "Combine Instructions" heading.
    /// </summary>
    private static string? ExtractCombineInstructions(string report)
    {
        // Fuzzy match: any heading level (#-####), case-insensitive, optional colon
        var match = System.Text.RegularExpressions.Regex.Match(
            report,
            @"^#{1,4}\s+combine\s+instructions:?\s*$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);

        if (!match.Success)
            return null;

        var instructions = report[(match.Index + match.Length)..].Trim();
        return instructions.Length > 0 ? instructions : null;
    }

    /// <summary>
    /// Run creative generation step — same PS outputs as structured, different framing.
    /// Story-only (no document creative variant).
    /// </summary>
    public async Task<(string Content, string SystemPrompt, string UserPrompt, int InputTokens, int OutputTokens)> RunCreativeGenerationAsync(
        StoryPromptContext storyCtx,
        string model = "claude-sonnet-4-6-20250514",
        int maxTokens = 8192,
        IProgress<TaskProgress>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report(new TaskProgress("Generating creative version...", 0.3));

        var systemPrompt = CreativeStoryPromptBuilder.SystemPrompt;
        var userPrompt = CreativeStoryPromptBuilder.BuildUserPrompt(storyCtx);

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
            throw new InvalidOperationException($"Creative generation failed: {response.Error}");

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
            ? CopyEditPromptBuilder.StorySystemPrompt
            : CopyEditPromptBuilder.DocumentSystemPrompt;

        var currentWordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var userPrompt = CopyEditPromptBuilder.BuildUserPrompt(text, styleName, minWords, maxWords,
            currentWordCount, craftPosture, narrativeVoice, motifs);

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

    /// <summary>
    /// Run quick check — fast scan for unanchored entity references.
    /// Returns raw JSON to be parsed into QuickCheckReport.
    /// </summary>
    public async Task<(string ReportJson, int InputTokens, int OutputTokens)> RunQuickCheckAsync(
        string chronicleContent,
        IReadOnlyList<ChronicleRoleAssignment> roleAssignments,
        IReadOnlyList<TertiaryCastEntry>? tertiaryCast = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? nameBank = null,
        IReadOnlyList<EntityDirective>? entityDirectives = null,
        string model = "claude-haiku-4-5-20251001",
        IProgress<TaskProgress>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report(new TaskProgress("Running quick check...", 0.9));

        var request = new LlmRequest
        {
            SystemPrompt = QuickCheckPrompts.SystemPrompt,
            UserPrompt = QuickCheckPrompts.BuildUserPrompt(
                chronicleContent, roleAssignments, tertiaryCast, nameBank, entityDirectives),
            Model = model,
            MaxTokens = 2048,
            Temperature = 0.2,
        };

        var response = await _llm.CompleteAsync(request, ct);
        if (response.Error is not null)
            throw new InvalidOperationException($"Quick check failed: {response.Error}");

        return (response.Text, response.Usage.InputTokens, response.Usage.OutputTokens);
    }

    /// <summary>
    /// Run cover image scene description generation.
    /// Uses narrative-style-aware scene prompt templates (montage, intimate, symbolic, etc.).
    /// </summary>
    public async Task<(string SceneJson, int InputTokens, int OutputTokens)> RunCoverImageSceneAsync(
        string chronicleContent,
        string title,
        IReadOnlyList<ChronicleRoleAssignment> roleAssignments,
        string narrativeStyleId,
        IReadOnlyDictionary<string, string>? visualIdentities = null,
        string model = "claude-haiku-4-5-20251001",
        IProgress<TaskProgress>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report(new TaskProgress("Generating cover image scene...", 0.9));

        var coverConfig = CoverImagePrompts.GetCoverImageConfig(narrativeStyleId);
        var template = CoverImagePrompts.GetScenePromptTemplate(coverConfig.ScenePromptId);

        var request = new LlmRequest
        {
            SystemPrompt = CoverImagePrompts.SystemPrompt,
            UserPrompt = CoverImagePrompts.BuildSceneUserPrompt(
                chronicleContent, title, roleAssignments, template, visualIdentities),
            Model = model,
            MaxTokens = 512,
            Temperature = 0.5,
        };

        var response = await _llm.CompleteAsync(request, ct);
        if (response.Error is not null)
            throw new InvalidOperationException($"Cover image scene failed: {response.Error}");

        return (response.Text, response.Usage.InputTokens, response.Usage.OutputTokens);
    }

    /// <summary>
    /// Run cohesion validation — full narrative quality check across 6 dimensions.
    /// Returns raw JSON to be parsed into CohesionReport.
    /// </summary>
    public async Task<(string ReportJson, int InputTokens, int OutputTokens)> RunCohesionReportAsync(
        string chronicleContent,
        IReadOnlyList<ChronicleRoleAssignment> roleAssignments,
        IReadOnlyList<string>? canonFacts = null,
        IReadOnlyList<(string SectionId, string SectionName, string Goal)>? sectionGoals = null,
        string? narrativeStyleName = null,
        string? themeGuidance = null,
        string model = "claude-haiku-4-5-20251001",
        IProgress<TaskProgress>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report(new TaskProgress("Validating cohesion...", 0.9));

        var request = new LlmRequest
        {
            SystemPrompt = CohesionReportPrompts.SystemPrompt,
            UserPrompt = CohesionReportPrompts.BuildUserPrompt(
                chronicleContent, roleAssignments, canonFacts, sectionGoals,
                narrativeStyleName, themeGuidance),
            Model = model,
            MaxTokens = 2048,
            Temperature = 0.2,
        };

        var response = await _llm.CompleteAsync(request, ct);
        if (response.Error is not null)
            throw new InvalidOperationException($"Cohesion validation failed: {response.Error}");

        return (response.Text, response.Usage.InputTokens, response.Usage.OutputTokens);
    }

    /// <summary>
    /// Run temporal alignment check — detects era misalignment, anachronisms,
    /// and missing temporal grounding. Returns free-text report.
    /// </summary>
    public async Task<(string Report, int InputTokens, int OutputTokens)> RunTemporalCheckAsync(
        TemporalCheckInput input,
        string model = "claude-sonnet-4-6-20250514",
        IProgress<TaskProgress>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report(new TaskProgress("Checking temporal alignment...", 0.9));

        var request = new LlmRequest
        {
            SystemPrompt = TemporalCheckPrompts.SystemPrompt,
            UserPrompt = TemporalCheckPrompts.BuildUserPrompt(input),
            Model = model,
            MaxTokens = 2048,
            Temperature = 0.3,
        };

        var response = await _llm.CompleteAsync(request, ct);
        if (response.Error is not null)
            throw new InvalidOperationException($"Temporal check failed: {response.Error}");

        return (response.Text, response.Usage.InputTokens, response.Usage.OutputTokens);
    }
}
