using TheCanonry.ApiClients.Llm;

namespace TheCanonry.Illuminator.Types;

/// <summary>
/// All distinct LLM call types in Illuminator and their categories.
/// Each value maps to a specific prompt template and model configuration.
/// </summary>
public enum LlmCallType
{
    // Entity Description Chain
    DescriptionNarrative,
    DescriptionVisualThesis,
    DescriptionVisualTraits,

    // Image Generation
    ImagePromptFormatting,
    ImageChronicleFormatting,

    // Perspective Synthesis
    PerspectiveSynthesis,

    // Chronicle Generation
    ChronicleGeneration,
    ChronicleCompare,
    ChronicleCombine,
    ChronicleSummary,
    ChronicleTitle,
    ChronicleImageRefs,
    ChronicleCoverImageScene,
    ChronicleCopyEdit,
    ChronicleQuickCheck,
    ChronicleFactCoverage,
    ChronicleToneRanking,
    ChronicleBulkToneRanking,
    ChronicleBatchTagImageRefs,
    ChronicleBatchTagCoverImages,

    // Palette
    PaletteExpansion,

    // Dynamics
    DynamicsGeneration,

    // Revision
    RevisionSummary,
    RevisionLoreBackport,

    // Historian
    HistorianEntityReview,
    HistorianChronicleReview,
    HistorianEdition,
    HistorianChronology,
    HistorianPrep,
    HistorianEraNarrativeThreads,
    HistorianEraNarrativeGenerate,
    HistorianEraNarrativeEdit,
    HistorianEraNarrativeCoverImageScene,
    HistorianEraNarrativeImageRefs,
    HistorianMotifVariation,

    // Entity Image Styles
    EntityBatchTagImageStyles,

    // Catalog
    CatalogMetadataFill,
    CatalogTitleFill,
}

/// <summary>
/// Broad category grouping for LLM call types.
/// </summary>
public enum LlmCallCategory
{
    Description,
    Image,
    Perspective,
    Chronicle,
    Palette,
    Dynamics,
    Revision,
    Historian,
    Entity,
    Catalog,
}

/// <summary>
/// Metadata for a single LLM call type: human-readable label, description,
/// category, and default model/token settings.
/// </summary>
public sealed record LlmCallMetadata
{
    public required string Label { get; init; }
    public required string Description { get; init; }
    public required LlmCallCategory Category { get; init; }
    public required string DefaultModel { get; init; }
    public required int DefaultThinkingBudget { get; init; }
    public required int DefaultMaxTokens { get; init; }
    public double? DefaultTemperature { get; init; }
}

/// <summary>
/// Default metadata for every LLM call type.
/// </summary>
public static class LlmCallDefaults
{
    public static readonly IReadOnlyDictionary<LlmCallType, LlmCallMetadata> Metadata =
        new Dictionary<LlmCallType, LlmCallMetadata>
        {
            // Description Chain
            [LlmCallType.DescriptionNarrative] = new()
            {
                Label = "Narrative",
                Description = "Generates summary, description, and aliases for an entity",
                Category = LlmCallCategory.Description,
                DefaultModel = LlmModel.Haiku45,
                DefaultThinkingBudget = 0,
                DefaultMaxTokens = 1024,
            },
            [LlmCallType.DescriptionVisualThesis] = new()
            {
                Label = "Visual Thesis",
                Description = "Creates the core visual silhouette from narrative description",
                Category = LlmCallCategory.Description,
                DefaultModel = LlmModel.Sonnet46,
                DefaultThinkingBudget = 4096,
                DefaultMaxTokens = 256,
            },
            [LlmCallType.DescriptionVisualTraits] = new()
            {
                Label = "Visual Traits",
                Description = "Generates distinctive visual details that complement the thesis",
                Category = LlmCallCategory.Description,
                DefaultModel = LlmModel.Haiku45,
                DefaultThinkingBudget = 0,
                DefaultMaxTokens = 512,
            },

            // Image Generation
            [LlmCallType.ImagePromptFormatting] = new()
            {
                Label = "Prompt Formatting",
                Description = "Reformats description for the image generation model",
                Category = LlmCallCategory.Image,
                DefaultModel = LlmModel.Haiku45,
                DefaultThinkingBudget = 0,
                DefaultMaxTokens = 2048,
            },
            [LlmCallType.ImageChronicleFormatting] = new()
            {
                Label = "Chronicle Prompt Formatting",
                Description = "Synthesizes chronicle scene/montage prompts for the image generation model",
                Category = LlmCallCategory.Image,
                DefaultModel = LlmModel.Haiku45,
                DefaultThinkingBudget = 0,
                DefaultMaxTokens = 2048,
            },

            // Perspective Synthesis
            [LlmCallType.PerspectiveSynthesis] = new()
            {
                Label = "Perspective Synthesis",
                Description = "Synthesizes a world perspective brief from entity constellation analysis",
                Category = LlmCallCategory.Perspective,
                DefaultModel = LlmModel.Sonnet46,
                DefaultThinkingBudget = 4096,
                DefaultMaxTokens = 1024,
            },

            // Chronicle Generation
            [LlmCallType.ChronicleGeneration] = new()
            {
                Label = "Generation",
                Description = "Creates the initial chronicle narrative content",
                Category = LlmCallCategory.Chronicle,
                DefaultModel = LlmModel.Sonnet46,
                DefaultThinkingBudget = 0,
                DefaultMaxTokens = 0,
                DefaultTemperature = 1.0,
            },
            [LlmCallType.ChronicleCompare] = new()
            {
                Label = "Compare Versions",
                Description = "Comparative analysis of multiple chronicle drafts (report only)",
                Category = LlmCallCategory.Chronicle,
                DefaultModel = LlmModel.Sonnet46,
                DefaultThinkingBudget = 4096,
                DefaultMaxTokens = 4096,
            },
            [LlmCallType.ChronicleCombine] = new()
            {
                Label = "Combine Versions",
                Description = "Synthesizes multiple chronicle drafts into one final version",
                Category = LlmCallCategory.Chronicle,
                DefaultModel = LlmModel.Opus46,
                DefaultThinkingBudget = 4096,
                DefaultMaxTokens = 8192,
            },
            [LlmCallType.ChronicleSummary] = new()
            {
                Label = "Summary",
                Description = "Generates a concise summary for the chronicle",
                Category = LlmCallCategory.Chronicle,
                DefaultModel = LlmModel.Haiku45,
                DefaultThinkingBudget = 0,
                DefaultMaxTokens = 512,
            },
            [LlmCallType.ChronicleTitle] = new()
            {
                Label = "Title",
                Description = "Single-pass title generation (candidate list)",
                Category = LlmCallCategory.Chronicle,
                DefaultModel = LlmModel.Haiku45,
                DefaultThinkingBudget = 0,
                DefaultMaxTokens = 256,
            },
            [LlmCallType.ChronicleImageRefs] = new()
            {
                Label = "Image References",
                Description = "Extracts image-worthy moments from the narrative",
                Category = LlmCallCategory.Chronicle,
                DefaultModel = LlmModel.Haiku45,
                DefaultThinkingBudget = 0,
                DefaultMaxTokens = 2048,
            },
            [LlmCallType.ChronicleCoverImageScene] = new()
            {
                Label = "Cover Image Scene",
                Description = "Generates a montage-style scene description for the chronicle cover image",
                Category = LlmCallCategory.Chronicle,
                DefaultModel = LlmModel.Haiku45,
                DefaultThinkingBudget = 0,
                DefaultMaxTokens = 512,
            },
            [LlmCallType.ChronicleCopyEdit] = new()
            {
                Label = "Copy-Edit",
                Description = "Polish pass — smooths voice, trims to word count, tightens prose without changing content",
                Category = LlmCallCategory.Chronicle,
                DefaultModel = LlmModel.Sonnet46,
                DefaultThinkingBudget = 0,
                DefaultMaxTokens = 8192,
            },
            [LlmCallType.ChronicleQuickCheck] = new()
            {
                Label = "Quick Check",
                Description = "Detects unanchored entity references — proper nouns not in the known cast or name bank",
                Category = LlmCallCategory.Chronicle,
                DefaultModel = LlmModel.Haiku45,
                DefaultThinkingBudget = 0,
                DefaultMaxTokens = 1024,
            },
            [LlmCallType.ChronicleFactCoverage] = new()
            {
                Label = "Fact Coverage Analysis",
                Description = "Classify canon fact presence in chronicle narratives",
                Category = LlmCallCategory.Chronicle,
                DefaultModel = LlmModel.Haiku45,
                DefaultThinkingBudget = 0,
                DefaultMaxTokens = 2048,
            },
            [LlmCallType.ChronicleToneRanking] = new()
            {
                Label = "Tone Ranking",
                Description = "Rank historian annotation tones by suitability for a chronicle summary",
                Category = LlmCallCategory.Chronicle,
                DefaultModel = LlmModel.Haiku45,
                DefaultThinkingBudget = 0,
                DefaultMaxTokens = 512,
            },
            [LlmCallType.ChronicleBulkToneRanking] = new()
            {
                Label = "Bulk Tone Ranking",
                Description = "Batch tone ranking — all chronicles evaluated in a single call with relative comparison",
                Category = LlmCallCategory.Chronicle,
                DefaultModel = LlmModel.Sonnet46,
                DefaultThinkingBudget = 16384,
                DefaultMaxTokens = 8192,
            },
            [LlmCallType.ChronicleBatchTagImageRefs] = new()
            {
                Label = "Batch Tag Image Refs",
                Description = "Assigns artistic style, composition, and color palette rankings to image refs across chronicles",
                Category = LlmCallCategory.Chronicle,
                DefaultModel = LlmModel.Sonnet46,
                DefaultThinkingBudget = 10000,
                DefaultMaxTokens = 16384,
            },
            [LlmCallType.ChronicleBatchTagCoverImages] = new()
            {
                Label = "Batch Tag Cover Images",
                Description = "Assigns artistic style, composition, and color palette rankings to cover images across chronicles",
                Category = LlmCallCategory.Chronicle,
                DefaultModel = LlmModel.Sonnet46,
                DefaultThinkingBudget = 10000,
                DefaultMaxTokens = 16384,
            },

            // Palette
            [LlmCallType.PaletteExpansion] = new()
            {
                Label = "Palette Expansion",
                Description = "Curates visual trait categories with extended reasoning",
                Category = LlmCallCategory.Palette,
                DefaultModel = LlmModel.Sonnet46,
                DefaultThinkingBudget = 8192,
                DefaultMaxTokens = 4096,
            },

            // Dynamics
            [LlmCallType.DynamicsGeneration] = new()
            {
                Label = "Dynamics Generation",
                Description = "Multi-turn world dynamics synthesis from lore and entity data",
                Category = LlmCallCategory.Dynamics,
                DefaultModel = LlmModel.Sonnet46,
                DefaultThinkingBudget = 4096,
                DefaultMaxTokens = 4096,
            },

            // Revision
            [LlmCallType.RevisionSummary] = new()
            {
                Label = "Summary Revision",
                Description = "Batch revision of entity summaries/descriptions using world dynamics",
                Category = LlmCallCategory.Revision,
                DefaultModel = LlmModel.Opus46,
                DefaultThinkingBudget = 4096,
                DefaultMaxTokens = 8192,
            },
            [LlmCallType.RevisionLoreBackport] = new()
            {
                Label = "Lore Backport",
                Description = "Extracts lore from published chronicles and backports to entity descriptions",
                Category = LlmCallCategory.Revision,
                DefaultModel = LlmModel.Sonnet46,
                DefaultThinkingBudget = 4096,
                DefaultMaxTokens = 8192,
            },

            // Historian
            [LlmCallType.HistorianEntityReview] = new()
            {
                Label = "Entity Annotations",
                Description = "Historian margin notes on an entity description — scholarly commentary and corrections",
                Category = LlmCallCategory.Historian,
                DefaultModel = LlmModel.Sonnet46,
                DefaultThinkingBudget = 4096,
                DefaultMaxTokens = 4096,
            },
            [LlmCallType.HistorianChronicleReview] = new()
            {
                Label = "Chronicle Annotations",
                Description = "Historian margin notes on a chronicle narrative — scholarly footnotes and commentary",
                Category = LlmCallCategory.Historian,
                DefaultModel = LlmModel.Sonnet46,
                DefaultThinkingBudget = 4096,
                DefaultMaxTokens = 8192,
            },
            [LlmCallType.HistorianEdition] = new()
            {
                Label = "Copy Edit",
                Description = "Historian-voiced rewrite of entity description from the full description archive",
                Category = LlmCallCategory.Historian,
                DefaultModel = LlmModel.Sonnet46,
                DefaultThinkingBudget = 4096,
                DefaultMaxTokens = 4096,
            },
            [LlmCallType.HistorianChronology] = new()
            {
                Label = "Chronology",
                Description = "Historian assigns year numbers to chronicles within an era",
                Category = LlmCallCategory.Historian,
                DefaultModel = LlmModel.Sonnet46,
                DefaultThinkingBudget = 8192,
                DefaultMaxTokens = 4096,
            },
            [LlmCallType.HistorianPrep] = new()
            {
                Label = "Prep Brief",
                Description = "Historian reading notes for a chronicle — private observations and thematic threads",
                Category = LlmCallCategory.Historian,
                DefaultModel = LlmModel.Sonnet46,
                DefaultThinkingBudget = 4096,
                DefaultMaxTokens = 1024,
            },
            [LlmCallType.HistorianEraNarrativeThreads] = new()
            {
                Label = "Era Narrative - Threads",
                Description = "Thread synthesis: identify cultural arcs, movement plan, thesis, and motifs from prep briefs",
                Category = LlmCallCategory.Historian,
                DefaultModel = LlmModel.Opus46,
                DefaultThinkingBudget = 16384,
                DefaultMaxTokens = 8192,
            },
            [LlmCallType.HistorianEraNarrativeGenerate] = new()
            {
                Label = "Era Narrative - Generate",
                Description = "Prose generation: write the era narrative (5-7K words) from thread synthesis",
                Category = LlmCallCategory.Historian,
                DefaultModel = LlmModel.Opus46,
                DefaultThinkingBudget = 16384,
                DefaultMaxTokens = 16384,
            },
            [LlmCallType.HistorianEraNarrativeEdit] = new()
            {
                Label = "Era Narrative - Edit",
                Description = "Copy-edit pass: tighten prose, cut modern register, strengthen mythic voice",
                Category = LlmCallCategory.Historian,
                DefaultModel = LlmModel.Opus46,
                DefaultThinkingBudget = 16384,
                DefaultMaxTokens = 16384,
            },
            [LlmCallType.HistorianEraNarrativeCoverImageScene] = new()
            {
                Label = "Era Narrative - Cover Scene",
                Description = "Generates a scene description for the era narrative cover image",
                Category = LlmCallCategory.Historian,
                DefaultModel = LlmModel.Haiku45,
                DefaultThinkingBudget = 0,
                DefaultMaxTokens = 512,
            },
            [LlmCallType.HistorianEraNarrativeImageRefs] = new()
            {
                Label = "Era Narrative - Image Refs",
                Description = "Places image references throughout the era narrative",
                Category = LlmCallCategory.Historian,
                DefaultModel = LlmModel.Haiku45,
                DefaultThinkingBudget = 0,
                DefaultMaxTokens = 2048,
            },
            [LlmCallType.HistorianMotifVariation] = new()
            {
                Label = "Motif Variation",
                Description = "Rewrite annotation clauses to vary overused phrases while preserving voice",
                Category = LlmCallCategory.Historian,
                DefaultModel = LlmModel.Opus46,
                DefaultThinkingBudget = 10000,
                DefaultMaxTokens = 8192,
            },

            // Entity Image Styles
            [LlmCallType.EntityBatchTagImageStyles] = new()
            {
                Label = "Batch Tag Entity Image Styles",
                Description = "Assigns artistic style, composition, and color palette rankings to entity images",
                Category = LlmCallCategory.Entity,
                DefaultModel = LlmModel.Sonnet46,
                DefaultThinkingBudget = 10000,
                DefaultMaxTokens = 16384,
            },

            // Catalog
            [LlmCallType.CatalogMetadataFill] = new()
            {
                Label = "Classify Fill",
                Description = "Classify images into artistic styles, composition styles, color palettes, and tags",
                Category = LlmCallCategory.Catalog,
                DefaultModel = LlmModel.Haiku45,
                DefaultThinkingBudget = 0,
                DefaultMaxTokens = 4096,
            },
            [LlmCallType.CatalogTitleFill] = new()
            {
                Label = "Title Fill",
                Description = "Generate evocative titles for images from their prompt and style context",
                Category = LlmCallCategory.Catalog,
                DefaultModel = LlmModel.Opus46,
                DefaultThinkingBudget = 4096,
                DefaultMaxTokens = 4096,
            },
        };

    /// <summary>
    /// Resolve the metadata for a given call type, returning defaults if not found.
    /// </summary>
    public static LlmCallMetadata GetMetadata(LlmCallType callType)
    {
        if (Metadata.TryGetValue(callType, out var meta))
            return meta;
        throw new ArgumentOutOfRangeException(nameof(callType), callType, "Unknown LLM call type");
    }
}
