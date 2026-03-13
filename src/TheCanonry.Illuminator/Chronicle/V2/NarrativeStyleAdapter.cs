using TheCanonry.Illuminator.Chronicle.PerspectiveSynthesis;
using TheCanonry.Schema.NarrativeStyles;

namespace TheCanonry.Illuminator.Chronicle.V2;

/// <summary>
/// Adapts full narrative style definitions to prompt context fields.
/// Bridges Schema.NarrativeStyles → V2 prompt builders.
/// </summary>
public static class NarrativeStyleAdapter
{
    /// <summary>
    /// Populate a StoryPromptContext with fields from a StoryNarrativeStyle.
    /// The caller provides the base context; this method returns a new context with style fields set.
    /// </summary>
    public static StoryPromptContext ApplyStyle(StoryPromptContext ctx, StoryNarrativeStyle style) =>
        ctx with
        {
            NarrativeInstructions = style.NarrativeInstructions,
            ProseInstructions = style.ProseInstructions,
            EventInstructions = style.EventInstructions,
            CraftPosture = style.CraftPosture,
            StyleName = style.Name,
            MinWords = style.Pacing.TotalWordCount.Min,
            MaxWords = style.Pacing.TotalWordCount.Max,
            MinScenes = style.Pacing.SceneCount.Min,
            MaxScenes = style.Pacing.SceneCount.Max,
        };

    /// <summary>
    /// Populate a DocumentPromptContext with fields from a DocumentNarrativeStyle.
    /// </summary>
    public static DocumentPromptContext ApplyStyle(DocumentPromptContext ctx, DocumentNarrativeStyle style) =>
        ctx with
        {
            DocumentInstructions = style.DocumentInstructions,
            EventInstructions = style.EventInstructions,
            CraftPosture = style.CraftPosture,
            StyleName = style.Name,
            MinWords = style.Pacing.WordCount.Min,
            MaxWords = style.Pacing.WordCount.Max,
        };

    /// <summary>
    /// Convert a full narrative style to the minimal NarrativeStyle record used by perspective synthesis.
    /// </summary>
    public static NarrativeStyle ToPerspectiveStyle(StoryNarrativeStyle style) =>
        new(
            Id: style.Id,
            Name: style.Name,
            Format: "story",
            Description: style.Description,
            Tags: style.Tags,
            ProseGuidance: style.ProseInstructions,
            CraftPosture: style.CraftPosture);

    /// <summary>
    /// Convert a full document style to the minimal NarrativeStyle record used by perspective synthesis.
    /// </summary>
    public static NarrativeStyle ToPerspectiveStyle(DocumentNarrativeStyle style) =>
        new(
            Id: style.Id,
            Name: style.Name,
            Format: "document",
            Description: style.Description,
            Tags: style.Tags,
            ProseGuidance: style.DocumentInstructions,
            CraftPosture: style.CraftPosture);

    /// <summary>
    /// Convert any narrative style base to the minimal perspective synthesis NarrativeStyle.
    /// </summary>
    public static NarrativeStyle ToPerspectiveStyle(NarrativeStyleBase style) =>
        style switch
        {
            StoryStyleWrapper sw => ToPerspectiveStyle(sw.Inner),
            DocumentStyleWrapper dw => ToPerspectiveStyle(dw.Inner),
            _ => new NarrativeStyle(style.Id, style.Name),
        };
}
