using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Prompts;

// =============================================================================
// Scene Prompt Templates
// =============================================================================

/// <summary>
/// A scene prompt template that tells the LLM what kind of visual to describe.
/// </summary>
public sealed record ScenePromptTemplate(string Id, string Name, string Framing, string Instructions);

/// <summary>
/// Per-narrative-style cover image configuration.
/// </summary>
public sealed record CoverImageConfig(string ScenePromptId, string CompositionStyleId);

/// <summary>
/// Prompts and configuration for chronicle cover image generation.
/// </summary>
public static class CoverImagePrompts
{
    public static string SystemPrompt =>
        "You are a visual art director creating cover image compositions. Always respond with valid JSON.";

    /// <summary>
    /// Build the user prompt for cover image scene description generation.
    /// </summary>
    public static string BuildSceneUserPrompt(
        string chronicleContent,
        string title,
        IReadOnlyList<ChronicleRoleAssignment> roleAssignments,
        ScenePromptTemplate template,
        IReadOnlyDictionary<string, string>? visualIdentities = null)
    {
        var castLines = roleAssignments.Select(r =>
        {
            var line = $"- {r.EntityName} ({r.EntityKind}, {r.Role}{(r.IsPrimary ? ", primary" : "")})";
            if (visualIdentities is not null && visualIdentities.TryGetValue(r.EntityId, out var visual))
                line += $"\n  Visual: {visual}";
            return line;
        });
        var castList = string.Join("\n", castLines);

        return $"{template.Framing}\n\n## Chronicle Title\n{title}\n\n## Cast\n{castList}\n\n## Chronicle Content\n{chronicleContent}\n\n## Instructions\n{template.Instructions}\n\nWeave the visual identity of each cast member into the scene description so their appearance is clear without separate lookups.\n\nIMPORTANT: Keep the scene description to 100-150 words. Be vivid but concise — every word should carry visual weight. This description will be combined with style, composition, and color directives, so focus on WHAT TO SHOW, not how to render it.\n\nReturn ONLY valid JSON:\n" +
            "{\"coverImageScene\": \"...\"}";
    }

    // =========================================================================
    // Scene Prompt Templates
    // =========================================================================

    private static readonly ScenePromptTemplate[] AllTemplates =
    [
        new("montage", "Cinematic Montage",
            "Generate a visual scene description for a cinematic montage-style cover image for this chronicle. The cover image should work like a movie poster (but with NO TEXT): overlapping visual elements showing key figures and settings from the chronicle, with dramatic layering and depth.",
            """
            Write a vivid visual description of a montage composition that captures the essence of this chronicle. Describe:
            - Which key figures should be prominent and at what scale (foreground, midground, background)
            - What settings, objects, or atmospheric elements should appear
            - The overall mood and lighting
            - How elements overlap and layer (movie-poster style, NOT a single coherent scene)
            """),
        new("intimate-scene", "Intimate Scene",
            "Generate a visual scene description for a single evocative cover image for this chronicle. The cover image should capture ONE key moment — a still frame from the story, not a montage. Focus on atmosphere and emotional resonance over spectacle.",
            """
            Write a vivid visual description of one scene that captures the emotional heart of this chronicle. Describe:
            - The single moment that best represents the chronicle's core
            - The setting in sensory detail
            - Character positioning and body language
            - Environmental mood — lighting, weather, time of day
            - One or two small telling details that reward close looking
            """),
        new("symbolic-abstract", "Symbolic / Abstract",
            "Generate a visual scene description for an abstract, mood-driven cover image for this chronicle. The image should evoke the chronicle's emotional and thematic essence through symbolic visual elements rather than literal scenes.",
            """
            Write a vivid visual description of an abstract composition that captures the thematic essence of this chronicle. Describe:
            - Central symbolic elements or motifs drawn from the chronicle's themes
            - Color palette and material quality that express the mood
            - How visual elements relate spatially — the abstract composition
            - What the viewer should feel before they understand what they're looking at
            - Any recurring images or metaphors from the text rendered visually
            """),
        new("document-artifact", "Document Artifact",
            "Generate a visual scene description for a cover image that depicts the chronicle AS a physical document or artifact — something with history, handled by real hands. The composition should show the document the way it would naturally be encountered in the world, not always flat on a surface photographed from above.",
            """
            Write a vivid visual description of this chronicle as a physical document. Describe:
            - Who made this document, who it was for, and how it would naturally be encountered or displayed
            - The physical form and material quality — chosen to match the document's purpose and social status
            - The viewing angle and spatial relationship between the viewer and the document
            - Signs of the document's specific history — how it has been used, handled, or damaged
            - The immediate environment and lighting
            """),
        new("formal-tableau", "Formal Tableau",
            "Generate a visual scene description for a formal, symmetrically arranged cover image for this chronicle. The composition should feel like a ceremonial painting or official record — staged, hierarchical, deliberate.",
            """
            Write a vivid visual description of a formal tableau composition that captures the ceremony or proceedings of this chronicle. Describe:
            - The central figure or focal point of authority
            - How other figures are arranged around them and what their positioning conveys
            - The formal setting and what it tells us about the nature of the proceedings
            - Architectural or environmental elements that reinforce the formality
            - Lighting that emphasizes the power structure
            """),
        new("folk-illustration", "Folk Illustration",
            "Generate a visual scene description for a folk-art style cover image for this chronicle. The image should look like a traditional illustration — iconic, decorative, with flattened perspective. Characters are archetypes, not portraits.",
            """
            Write a vivid visual description of a folk-art illustration that captures the central narrative of this chronicle. Describe:
            - Key characters or creatures rendered as iconic, slightly stylized figures
            - Decorative framing or border elements drawn from the chronicle's world
            - A flattened, non-perspectival spatial arrangement
            - Rich pattern and texture detail
            - The central narrative moment that reads clearly as a single image
            """),
        new("vignette-sequence", "Vignette Sequence",
            "Generate a visual scene description for a multi-panel vignette cover image for this chronicle. The image should contain several small, self-contained scenes arranged together — multiple moments from the chronicle presented side by side.",
            """
            Write a vivid visual description of a vignette sequence that captures the breadth of this chronicle. Describe:
            - 3-5 distinct moments from the chronicle, each as a brief self-contained scene
            - How the vignettes are arranged relative to each other
            - Visual connectors between panels — what ties them together as a set
            - Each vignette's focal subject and mood
            - The overall rhythm of the arrangement
            """),
    ];

    private static readonly IReadOnlyDictionary<string, ScenePromptTemplate> TemplateMap =
        AllTemplates.ToDictionary(t => t.Id, StringComparer.Ordinal);

    /// <summary>
    /// Get a scene prompt template by ID. Falls back to "montage".
    /// </summary>
    public static ScenePromptTemplate GetScenePromptTemplate(string id) =>
        TemplateMap.TryGetValue(id, out var template) ? template : TemplateMap["montage"];

    // =========================================================================
    // Per-Narrative-Style Configuration
    // =========================================================================

    private static readonly CoverImageConfig DefaultConfig = new("montage", "chronicle-overview");

    private static readonly IReadOnlyDictionary<string, CoverImageConfig> StyleConfigs =
        new Dictionary<string, CoverImageConfig>(StringComparer.Ordinal)
        {
            // Story styles
            ["epic-drama"] = new("montage", "chronicle-overview"),
            ["action-adventure"] = new("montage", "chronicle-overview"),
            ["heroic-fantasy"] = new("montage", "chronicle-overview"),
            ["treasure-hunt"] = new("montage", "chronicle-overview"),
            ["political-intrigue"] = new("montage", "chronicle-overview"),
            ["tragedy"] = new("montage", "chronicle-overview"),
            ["mystery-suspense"] = new("montage", "chronicle-overview"),
            ["haunted-relic"] = new("montage", "chronicle-overview"),
            ["lost-legacy"] = new("montage", "chronicle-overview"),
            ["apocalyptic-vision"] = new("symbolic-abstract", "chronicle-symbolic"),
            ["romance"] = new("intimate-scene", "chronicle-intimate"),
            ["slice-of-life"] = new("intimate-scene", "chronicle-intimate"),
            ["confession"] = new("intimate-scene", "chronicle-intimate"),
            ["poetic-lyrical"] = new("symbolic-abstract", "chronicle-symbolic"),
            ["dreamscape"] = new("symbolic-abstract", "chronicle-symbolic"),
            ["dark-comedy"] = new("vignette-sequence", "chronicle-vignette"),
            ["fable"] = new("folk-illustration", "chronicle-folk"),
            ["trial-judgment"] = new("formal-tableau", "chronicle-tableau"),
            ["last-stand"] = new("montage", "chronicle-overview"),
            ["breakthrough"] = new("montage", "chronicle-overview"),
            ["common-ground"] = new("intimate-scene", "chronicle-intimate"),
            ["rashomon"] = new("vignette-sequence", "chronicle-vignette"),
            // Document styles
            ["heralds-dispatch"] = new("document-artifact", "chronicle-document"),
            ["merchants-broadsheet"] = new("document-artifact", "chronicle-document"),
            ["collected-letters"] = new("document-artifact", "chronicle-document"),
            ["chronicle-entry"] = new("document-artifact", "chronicle-document"),
            ["wanted-notice"] = new("document-artifact", "chronicle-document"),
            ["field-report"] = new("document-artifact", "chronicle-document"),
            ["personal-diary"] = new("intimate-scene", "chronicle-intimate"),
            ["interrogation-record"] = new("formal-tableau", "chronicle-tableau"),
            ["diplomatic-accord"] = new("formal-tableau", "chronicle-tableau"),
            ["treatise-powers"] = new("document-artifact", "chronicle-document"),
            ["sacred-text"] = new("symbolic-abstract", "chronicle-symbolic"),
            ["creation-myth"] = new("symbolic-abstract", "chronicle-symbolic"),
            ["origin-myth"] = new("montage", "chronicle-overview"),
            ["artisans-catalogue"] = new("vignette-sequence", "chronicle-vignette"),
            ["tavern-notices"] = new("vignette-sequence", "chronicle-vignette"),
            ["product-reviews"] = new("vignette-sequence", "chronicle-vignette"),
            ["folk-song"] = new("folk-illustration", "chronicle-folk"),
            ["nursery-rhymes"] = new("folk-illustration", "chronicle-folk"),
            ["haiku-collection"] = new("symbolic-abstract", "chronicle-symbolic"),
            ["proverbs-sayings"] = new("symbolic-abstract", "chronicle-symbolic"),
        };

    /// <summary>
    /// Get cover image configuration for a narrative style. Falls back to montage/chronicle-overview.
    /// </summary>
    public static CoverImageConfig GetCoverImageConfig(string narrativeStyleId) =>
        StyleConfigs.TryGetValue(narrativeStyleId, out var config) ? config : DefaultConfig;
}
