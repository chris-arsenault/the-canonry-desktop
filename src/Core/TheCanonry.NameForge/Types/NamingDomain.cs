namespace TheCanonry.NameForge.Types;

/// <summary>
/// Phonology profile: controls sound inventory and allowed sequences.
/// </summary>
public sealed record PhonologyProfile
{
    /// <summary>Available consonant sounds.</summary>
    public required string[] Consonants { get; init; }

    /// <summary>Available vowel sounds.</summary>
    public required string[] Vowels { get; init; }

    /// <summary>Syllable patterns like CV, CVC, CVV.</summary>
    public required string[] SyllableTemplates { get; init; }

    /// <summary>Phoneme sequences to avoid.</summary>
    public string[] ForbiddenClusters { get; init; } = [];

    /// <summary>Phoneme sequences to boost probability.</summary>
    public string[] FavoredClusters { get; init; } = [];

    /// <summary>Weight for each consonant (defaults to uniform).</summary>
    public double[] ConsonantWeights { get; init; } = [];

    /// <summary>Weight for each vowel (defaults to uniform).</summary>
    public double[] VowelWeights { get; init; } = [];

    /// <summary>Weight for each syllable template (defaults to uniform).</summary>
    public double[] TemplateWeights { get; init; } = [];

    /// <summary>Min and max syllable count [min, max].</summary>
    public required int[] LengthRange { get; init; }

    /// <summary>Multiplier for favored cluster probability (default: 2.0).</summary>
    public double FavoredClusterBoost { get; init; } = 2.0;
}

/// <summary>
/// Morphology profile: controls prefixes, suffixes, and word structure.
/// </summary>
public sealed record MorphologyProfile
{
    /// <summary>Available prefixes.</summary>
    public string[] Prefixes { get; init; } = [];

    /// <summary>Available suffixes.</summary>
    public string[] Suffixes { get; init; } = [];

    /// <summary>Available infixes.</summary>
    public string[] Infixes { get; init; } = [];

    /// <summary>Pre-defined word roots for compound names.</summary>
    public string[] WordRoots { get; init; } = [];

    /// <summary>Titles and honorifics.</summary>
    public string[] Honorifics { get; init; } = [];

    /// <summary>Structural patterns like "root", "root-suffix", "prefix-root-suffix".</summary>
    public required string[] Structure { get; init; }

    /// <summary>Weight for each prefix.</summary>
    public double[] PrefixWeights { get; init; } = [];

    /// <summary>Weight for each suffix.</summary>
    public double[] SuffixWeights { get; init; } = [];

    /// <summary>Weight for each structural pattern.</summary>
    public double[] StructureWeights { get; init; } = [];
}

/// <summary>
/// Style rules: controls fine-grained stylistic transforms.
/// </summary>
public sealed record StyleRules
{
    /// <summary>Probability of inserting apostrophes (0-1, default: 0).</summary>
    public double ApostropheRate { get; init; }

    /// <summary>Probability of inserting hyphens (0-1, default: 0).</summary>
    public double HyphenRate { get; init; }

    /// <summary>Capitalization style (default: title).</summary>
    public Capitalization Capitalization { get; init; } = Capitalization.Title;

    /// <summary>Suffix patterns to boost in final selection.</summary>
    public string[] PreferredEndings { get; init; } = [];

    /// <summary>Multiplier for preferred ending probability (default: 2.0).</summary>
    public double PreferredEndingBoost { get; init; } = 2.0;

    /// <summary>Overall phonetic rhythm preference (default: neutral).</summary>
    public RhythmBias RhythmBias { get; init; } = RhythmBias.Neutral;
}

/// <summary>
/// Capitalization styles.
/// </summary>
public enum Capitalization
{
    /// <summary>First letter uppercase, rest lowercase.</summary>
    Title,
    /// <summary>Each word capitalized.</summary>
    TitleWords,
    /// <summary>All uppercase.</summary>
    AllCaps,
    /// <summary>All lowercase.</summary>
    Lowercase,
    /// <summary>Alternating case.</summary>
    Mixed
}

/// <summary>
/// Rhythm bias for phonetic character.
/// </summary>
public enum RhythmBias
{
    Soft,
    Harsh,
    Staccato,
    Flowing,
    Neutral
}

/// <summary>
/// Entity matching criteria for a domain.
/// </summary>
public sealed record AppliesTo
{
    /// <summary>Entity kinds this domain matches.</summary>
    public required string[] Kind { get; init; }

    /// <summary>Specific subtypes.</summary>
    public string[] SubKind { get; init; } = [];

    /// <summary>Required tags.</summary>
    public string[] Tags { get; init; } = [];
}

/// <summary>
/// Complete naming domain: combines phonology, morphology, and style.
/// </summary>
public sealed record NamingDomain
{
    /// <summary>Unique domain identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Entity matching criteria.</summary>
    public required AppliesTo AppliesTo { get; init; }

    /// <summary>Sound generation rules.</summary>
    public required PhonologyProfile Phonology { get; init; }

    /// <summary>Word structure rules.</summary>
    public required MorphologyProfile Morphology { get; init; }

    /// <summary>Stylistic transforms.</summary>
    public required StyleRules Style { get; init; }
}
