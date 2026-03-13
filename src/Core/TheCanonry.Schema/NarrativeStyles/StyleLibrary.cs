namespace TheCanonry.Schema.NarrativeStyles;

/// <summary>
/// Combined narrative style — either story or document.
/// Discriminated by Format property.
/// </summary>
public abstract record NarrativeStyleBase
{
    public abstract NarrativeFormat Format { get; }
    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract IReadOnlyList<string> Tags { get; }
    public abstract EraNarrativeWeight EraNarrativeWeight { get; }
    public abstract string EventInstructions { get; }
    public abstract string CraftPosture { get; }
    public abstract string TitleGuidance { get; }
    public abstract IReadOnlyList<RoleDefinition> Roles { get; }
}

/// <summary>
/// Wrapper to treat StoryNarrativeStyle as NarrativeStyleBase.
/// </summary>
public sealed record StoryStyleWrapper(StoryNarrativeStyle Inner) : NarrativeStyleBase
{
    public override NarrativeFormat Format => Inner.Format;
    public override string Id => Inner.Id;
    public override string Name => Inner.Name;
    public override string Description => Inner.Description;
    public override IReadOnlyList<string> Tags => Inner.Tags;
    public override EraNarrativeWeight EraNarrativeWeight => Inner.EraNarrativeWeight;
    public override string EventInstructions => Inner.EventInstructions;
    public override string CraftPosture => Inner.CraftPosture;
    public override string TitleGuidance => Inner.TitleGuidance;
    public override IReadOnlyList<RoleDefinition> Roles => Inner.Roles;
}

/// <summary>
/// Wrapper to treat DocumentNarrativeStyle as NarrativeStyleBase.
/// </summary>
public sealed record DocumentStyleWrapper(DocumentNarrativeStyle Inner) : NarrativeStyleBase
{
    public override NarrativeFormat Format => Inner.Format;
    public override string Id => Inner.Id;
    public override string Name => Inner.Name;
    public override string Description => Inner.Description;
    public override IReadOnlyList<string> Tags => Inner.Tags;
    public override EraNarrativeWeight EraNarrativeWeight => Inner.EraNarrativeWeight;
    public override string EventInstructions => Inner.EventInstructions;
    public override string CraftPosture => Inner.CraftPosture;
    public override string TitleGuidance => Inner.TitleGuidance;
    public override IReadOnlyList<RoleDefinition> Roles => Inner.Roles;
}

/// <summary>
/// Collection of all narrative styles with lookup support.
/// </summary>
public sealed class NarrativeStyleLibrary
{
    private readonly Dictionary<string, StoryNarrativeStyle> _storyStyles;
    private readonly Dictionary<string, DocumentNarrativeStyle> _documentStyles;

    public IReadOnlyList<StoryNarrativeStyle> StoryStyles { get; }
    public IReadOnlyList<DocumentNarrativeStyle> DocumentStyles { get; }

    public NarrativeStyleLibrary(
        IReadOnlyList<StoryNarrativeStyle> storyStyles,
        IReadOnlyList<DocumentNarrativeStyle> documentStyles)
    {
        StoryStyles = storyStyles;
        DocumentStyles = documentStyles;
        _storyStyles = storyStyles.ToDictionary(s => s.Id);
        _documentStyles = documentStyles.ToDictionary(s => s.Id);
    }

    /// <summary>
    /// Find a narrative style by ID. Returns null if not found.
    /// </summary>
    public NarrativeStyleBase? FindById(string id)
    {
        if (_storyStyles.TryGetValue(id, out var story))
            return new StoryStyleWrapper(story);
        if (_documentStyles.TryGetValue(id, out var doc))
            return new DocumentStyleWrapper(doc);
        return null;
    }

    /// <summary>
    /// Find a story style by ID. Returns null if not found or if the ID is a document style.
    /// </summary>
    public StoryNarrativeStyle? FindStoryStyle(string id)
        => _storyStyles.GetValueOrDefault(id);

    /// <summary>
    /// Find a document style by ID. Returns null if not found or if the ID is a story style.
    /// </summary>
    public DocumentNarrativeStyle? FindDocumentStyle(string id)
        => _documentStyles.GetValueOrDefault(id);

    /// <summary>
    /// All styles as a flat list (story + document).
    /// </summary>
    public IEnumerable<NarrativeStyleBase> All
        => StoryStyles.Select(s => (NarrativeStyleBase)new StoryStyleWrapper(s))
            .Concat(DocumentStyles.Select(d => new DocumentStyleWrapper(d)));

    /// <summary>
    /// Create the default library with all 42 built-in styles.
    /// </summary>
    public static NarrativeStyleLibrary CreateDefault()
        => new(DefaultNarrativeStyles.AllStoryStyles, DefaultNarrativeStyles.AllDocumentStyles);
}
