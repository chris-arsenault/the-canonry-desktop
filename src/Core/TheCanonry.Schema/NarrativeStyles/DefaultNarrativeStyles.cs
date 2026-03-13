using System.Collections.Immutable;

namespace TheCanonry.Schema.NarrativeStyles;

/// <summary>
/// Aggregates all default narrative style definitions (22 story + 20 document = 42 total).
/// Individual category files are partial class members.
/// </summary>
internal static partial class DefaultNarrativeStyles
{
    private static IReadOnlyList<StoryNarrativeStyle>? _allStoryStyles;
    private static IReadOnlyList<DocumentNarrativeStyle>? _allDocumentStyles;

    /// <summary>All 22 story narrative styles.</summary>
    public static IReadOnlyList<StoryNarrativeStyle> AllStoryStyles =>
        _allStoryStyles ??= ImmutableArray.Create(
        [
            .. ClassicStyles,
            .. ExperimentalStyles,
            .. GenreStyles,
            .. IntimateStyles,
            .. ClimaticStyles,
        ]);

    /// <summary>All 20 document narrative styles.</summary>
    public static IReadOnlyList<DocumentNarrativeStyle> AllDocumentStyles =>
        _allDocumentStyles ??= ImmutableArray.Create(
        [
            .. OfficialDocumentStyles,
            .. ProfessionalDocumentStyles,
            .. LiteraryDocumentStyles,
        ]);
}
