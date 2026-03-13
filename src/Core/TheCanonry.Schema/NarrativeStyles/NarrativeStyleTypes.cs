namespace TheCanonry.Schema.NarrativeStyles;

/// <summary>
/// Narrative format type — distinguishes stories from documents.
/// </summary>
public enum NarrativeFormat
{
    Story,
    Document,
}

/// <summary>
/// Era narrative weight — determines how a chronicle using this style
/// is weighted in era narrative prompt assembly.
/// </summary>
public enum EraNarrativeWeight
{
    /// <summary>Defines the era's trajectory. These chronicles ARE the major events.</summary>
    Structural,
    /// <summary>Provides institutional, political, or personal framing.</summary>
    Contextual,
    /// <summary>World texture. Color and atmosphere, not arc-defining.</summary>
    Flavor,
}

/// <summary>
/// Role definition for entity casting in a narrative style.
/// </summary>
public sealed record RoleDefinition
{
    public required string Role { get; init; }
    public required RoleCount Count { get; init; }
    public required string Description { get; init; }
    public string SelectionCriteria { get; init; } = "";
}

/// <summary>
/// Min/max count for a role.
/// </summary>
public sealed record RoleCount(int Min, int Max);

/// <summary>
/// Word count range.
/// </summary>
public sealed record WordRange(int Min, int Max);

/// <summary>
/// Pacing configuration for story narrative styles.
/// </summary>
public sealed record StoryPacingConfig
{
    public required WordRange TotalWordCount { get; init; }
    public required WordRange SceneCount { get; init; }
}

/// <summary>
/// Pacing configuration for document narrative styles.
/// </summary>
public sealed record DocumentPacingConfig
{
    public required WordRange WordCount { get; init; }
}

/// <summary>
/// Story narrative style — freeform text blocks over structured micro-fields.
/// </summary>
public sealed record StoryNarrativeStyle
{
    public NarrativeFormat Format { get; } = NarrativeFormat.Story;

    // Metadata
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public required EraNarrativeWeight EraNarrativeWeight { get; init; }

    // Freeform text blocks (injected directly into prompts)
    public required string NarrativeInstructions { get; init; }
    public required string ProseInstructions { get; init; }
    public required string EventInstructions { get; init; }
    public required string CraftPosture { get; init; }
    public required string TitleGuidance { get; init; }

    // Structured data
    public required IReadOnlyList<RoleDefinition> Roles { get; init; }
    public required StoryPacingConfig Pacing { get; init; }
}

/// <summary>
/// Document narrative style — for in-universe document formats.
/// </summary>
public sealed record DocumentNarrativeStyle
{
    public NarrativeFormat Format { get; } = NarrativeFormat.Document;

    // Metadata
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public required EraNarrativeWeight EraNarrativeWeight { get; init; }

    // Freeform text blocks
    public required string DocumentInstructions { get; init; }
    public required string EventInstructions { get; init; }
    public required string CraftPosture { get; init; }
    public required string TitleGuidance { get; init; }

    // Structured data
    public required IReadOnlyList<RoleDefinition> Roles { get; init; }
    public required DocumentPacingConfig Pacing { get; init; }
}
