namespace TheCanonry.NameForge.Types;

/// <summary>
/// Lexeme list: a collection of words for use in grammars.
/// </summary>
public sealed record LexemeList
{
    /// <summary>Unique list identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Description of the list's purpose.</summary>
    public string Description { get; init; } = "";

    /// <summary>The lexeme entries.</summary>
    public required string[] Entries { get; init; }

    /// <summary>Source of the entries.</summary>
    public string Source { get; init; } = "";
}

/// <summary>
/// Grammar rule: a context-free grammar for name generation.
/// Rules map symbols to productions (each production is an array of tokens).
/// </summary>
public sealed record Grammar
{
    /// <summary>Unique grammar identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Description of the grammar's purpose.</summary>
    public string Description { get; init; } = "";

    /// <summary>Starting symbol (e.g., "name").</summary>
    public required string Start { get; init; }

    /// <summary>
    /// Maps symbols to productions. Each production is a list of tokens.
    /// Token types: slot:listId, domain:domainId, markov:modelId, context:key, symbol, literal.
    /// </summary>
    public required Dictionary<string, string[][]> Rules { get; init; }

    /// <summary>Capitalization style to apply to final output.</summary>
    public Capitalization Capitalization { get; init; } = Capitalization.Title;
}

/// <summary>
/// Conditions for strategy group activation.
/// </summary>
public sealed record GroupConditions
{
    /// <summary>Entity kinds that activate this group.</summary>
    public string[] EntityKinds { get; init; } = [];

    /// <summary>Subtypes that activate this group.</summary>
    public string[] Subtypes { get; init; } = [];

    /// <summary>If true, ALL subtypes must match (default: ANY).</summary>
    public bool SubtypeMatchAll { get; init; }

    /// <summary>Prominence levels that activate this group.</summary>
    public string[] Prominence { get; init; } = [];

    /// <summary>Tags that activate this group.</summary>
    public string[] Tags { get; init; } = [];

    /// <summary>If true, ALL tags must match (default: ANY).</summary>
    public bool TagMatchAll { get; init; }
}

/// <summary>
/// Naming strategy: how to generate a single name.
/// </summary>
public sealed record Strategy
{
    /// <summary>Strategy type: grammar or phonotactic.</summary>
    public required StrategyType Type { get; init; }

    /// <summary>Relative weight for random selection.</summary>
    public double Weight { get; init; }

    /// <summary>Grammar ID (for grammar type).</summary>
    public string GrammarId { get; init; } = "";

    /// <summary>Domain ID (for phonotactic type).</summary>
    public string DomainId { get; init; } = "";
}

/// <summary>
/// Strategy type enum.
/// </summary>
public enum StrategyType
{
    Grammar,
    Phonotactic
}

/// <summary>
/// Strategy group: a set of strategies with shared activation conditions.
/// </summary>
public sealed record StrategyGroup
{
    /// <summary>Display name.</summary>
    public string Name { get; init; } = "";

    /// <summary>Priority (higher number = higher priority, checked first).</summary>
    public int Priority { get; init; }

    /// <summary>Activation conditions (null = always active / matches all).</summary>
    public GroupConditions? Conditions { get; init; }

    /// <summary>Available strategies.</summary>
    public required Strategy[] Strategies { get; init; }
}

/// <summary>
/// Naming profile: defines how to generate names for a culture.
/// </summary>
public sealed record Profile
{
    /// <summary>Unique profile identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Display name.</summary>
    public string Name { get; init; } = "";

    /// <summary>Entity kinds this profile applies to (empty = use IsDefault logic).</summary>
    public string[] EntityKinds { get; init; } = [];

    /// <summary>Mark as default fallback profile.</summary>
    public bool IsDefault { get; init; }

    /// <summary>Strategy groups for name generation.</summary>
    public required StrategyGroup[] StrategyGroups { get; init; }
}

/// <summary>
/// Culture: owns all naming resources for a cultural group.
/// </summary>
public sealed record Culture
{
    /// <summary>Unique culture identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Display name.</summary>
    public required string Name { get; init; }

    /// <summary>Description.</summary>
    public string Description { get; init; } = "";

    /// <summary>Phonotactic domains.</summary>
    public required NamingDomain[] Domains { get; init; }

    /// <summary>Lexeme lists keyed by ID.</summary>
    public required Dictionary<string, LexemeList> LexemeLists { get; init; }

    /// <summary>Grammar rules.</summary>
    public required Grammar[] Grammars { get; init; }

    /// <summary>Naming profiles.</summary>
    public required Profile[] Profiles { get; init; }
}
