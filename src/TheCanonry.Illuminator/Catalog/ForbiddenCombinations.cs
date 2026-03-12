namespace TheCanonry.Illuminator.Catalog;

/// <summary>
/// Forbidden artistic×composition pairs ported from DEFAULT_RANDOM_EXCLUSIONS
/// in packages/world-schema/src/styleExclusions.ts.
///
/// The TS source uses category-level patterns ("cat:X") expanded at runtime
/// against style/composition libraries. Because the C# port operates without
/// the full style library at assignment time, this class resolves the rules
/// against the concrete style IDs used in the default libraries.
///
/// Rules:
///   1. Document/archival styles clash with non-artifact subjects
///   2. Logo/badge compositions need clean graphic styles
///   3. Tilt-shift miniature effect needs scenes with spatial depth
///   4. Technical compositions need precise rendering styles
///   5. Eldritch biomechanical clashes with clean/precise compositions
///   6. Cosmic chrome needs spatial depth; clashes with flat/technical compositions
/// </summary>
public static class ForbiddenCombinations
{
    // -------------------------------------------------------------------------
    // Artistic style IDs per category (from artisticStyles.ts)
    // -------------------------------------------------------------------------

    // cat:document
    private static readonly HashSet<string> DocumentStyles = new(StringComparer.Ordinal)
    {
        "illuminated-manuscript", "cartographic-manuscript", "legal-document",
        "newspaper-print", "ledger-accounting", "bureaucratic-form",
    };

    // cat:painting
    private static readonly HashSet<string> PaintingStyles = new(StringComparer.Ordinal)
    {
        "oil-painting", "watercolor", "gouache", "acrylic-painting",
        "fresco", "tempera", "impasto",
    };

    // cat:camera
    private static readonly HashSet<string> CameraStyles = new(StringComparer.Ordinal)
    {
        "hdr-nature-photography", "cinematic-still", "daguerreotype",
        "double-exposure", "tilt-shift", "film-noir-photography",
        "documentary-photography",
    };

    // cat:experimental
    private static readonly HashSet<string> ExperimentalStyles = new(StringComparer.Ordinal)
    {
        "eldritch-biomechanical", "cosmic-chrome", "glitch-artifact",
        "infrared-dreamscape", "cyanotype-tonal",
    };

    // -------------------------------------------------------------------------
    // Composition IDs per category (from compositionStyles.ts)
    // -------------------------------------------------------------------------

    // cat:character
    private static readonly HashSet<string> CharacterCompositions = new(StringComparer.Ordinal)
    {
        "portrait-closeup", "portrait-bust", "portrait-three-quarter",
        "portrait-full-body", "portrait-silhouette",
    };

    // cat:pair
    private static readonly HashSet<string> PairCompositions = new(StringComparer.Ordinal)
    {
        "pair-confrontation", "pair-alliance", "pair-contrast",
    };

    // cat:pose
    private static readonly HashSet<string> PoseCompositions = new(StringComparer.Ordinal)
    {
        "action-pose", "contemplative-pose", "triumphant-pose",
    };

    // cat:place
    private static readonly HashSet<string> PlaceCompositions = new(StringComparer.Ordinal)
    {
        "establishing-wide", "interior-medium", "detail-closeup",
        "aerial-overview",
    };

    // cat:landscape
    private static readonly HashSet<string> LandscapeCompositions = new(StringComparer.Ordinal)
    {
        "panoramic-vista", "atmospheric-mood", "dramatic-sky",
        "intimate-corner",
    };

    // cat:concept
    private static readonly HashSet<string> ConceptCompositions = new(StringComparer.Ordinal)
    {
        "symbolic-allegory", "abstract-motif", "emblematic-symbol",
    };

    // cat:object
    private static readonly HashSet<string> ObjectCompositions = new(StringComparer.Ordinal)
    {
        "display-case", "field-study", "artifact-diagram",
    };

    // -------------------------------------------------------------------------
    // Pre-expanded forbidden pairs
    // -------------------------------------------------------------------------

    private static readonly HashSet<(string Artistic, string Composition)> ForbiddenSet;

    static ForbiddenCombinations()
    {
        ForbiddenSet = new HashSet<(string, string)>();

        // Rule 1: document styles × (character | pair | pose | place | landscape | concept |
        //         group-scene | action-battle | formation | council-chamber |
        //         chronicle-panorama | chronicle-overview | chronicle-intimate |
        //         chronicle-symbolic | chronicle-tableau | chronicle-folk)
        var rule1Comps = new HashSet<string>(StringComparer.Ordinal);
        rule1Comps.UnionWith(CharacterCompositions);
        rule1Comps.UnionWith(PairCompositions);
        rule1Comps.UnionWith(PoseCompositions);
        rule1Comps.UnionWith(PlaceCompositions);
        rule1Comps.UnionWith(LandscapeCompositions);
        rule1Comps.UnionWith(ConceptCompositions);
        rule1Comps.UnionWith(new[]
        {
            "group-scene", "action-battle", "formation", "council-chamber",
            "chronicle-panorama", "chronicle-overview", "chronicle-intimate",
            "chronicle-symbolic", "chronicle-tableau", "chronicle-folk",
        });
        foreach (var a in DocumentStyles)
            foreach (var c in rule1Comps)
                ForbiddenSet.Add((a, c));

        // Rule 2: (painting | camera | experimental) × (logo-mark | badge-crest)
        var rule2Arts = new HashSet<string>(StringComparer.Ordinal);
        rule2Arts.UnionWith(PaintingStyles);
        rule2Arts.UnionWith(CameraStyles);
        rule2Arts.UnionWith(ExperimentalStyles);
        var rule2Comps = new[] { "logo-mark", "badge-crest" };
        foreach (var a in rule2Arts)
            foreach (var c in rule2Comps)
                ForbiddenSet.Add((a, c));

        // Rule 3: tilt-shift × (character | pair | pose | object | concept)
        var rule3Comps = new HashSet<string>(StringComparer.Ordinal);
        rule3Comps.UnionWith(CharacterCompositions);
        rule3Comps.UnionWith(PairCompositions);
        rule3Comps.UnionWith(PoseCompositions);
        rule3Comps.UnionWith(ObjectCompositions);
        rule3Comps.UnionWith(ConceptCompositions);
        foreach (var c in rule3Comps)
            ForbiddenSet.Add(("tilt-shift", c));

        // Rule 4: (painting | experimental | hdr-nature-photography | cinematic-still |
        //          daguerreotype | double-exposure) × (scientific-drawing | schematic | artifact-diagram)
        var rule4Arts = new HashSet<string>(StringComparer.Ordinal);
        rule4Arts.UnionWith(PaintingStyles);
        rule4Arts.UnionWith(ExperimentalStyles);
        rule4Arts.UnionWith(new[] { "hdr-nature-photography", "cinematic-still", "daguerreotype", "double-exposure" });
        var rule4Comps = new[] { "scientific-drawing", "schematic", "artifact-diagram" };
        foreach (var a in rule4Arts)
            foreach (var c in rule4Comps)
                ForbiddenSet.Add((a, c));

        // Rule 5: eldritch-biomechanical × (logo-mark | badge-crest | scientific-drawing |
        //          schematic | display-case | field-study)
        var rule5Comps = new[]
        {
            "logo-mark", "badge-crest", "scientific-drawing",
            "schematic", "display-case", "field-study",
        };
        foreach (var c in rule5Comps)
            ForbiddenSet.Add(("eldritch-biomechanical", c));

        // Rule 6: cosmic-chrome × (logo-mark | badge-crest | scientific-drawing | schematic |
        //          artifact-diagram | display-case | map-view | chronicle-document | chronicle-vignette)
        var rule6Comps = new[]
        {
            "logo-mark", "badge-crest", "scientific-drawing", "schematic",
            "artifact-diagram", "display-case", "map-view",
            "chronicle-document", "chronicle-vignette",
        };
        foreach (var c in rule6Comps)
            ForbiddenSet.Add(("cosmic-chrome", c));
    }

    /// <summary>
    /// Returns true if the given artistic × composition combination is forbidden.
    /// </summary>
    public static bool IsForbidden(string artisticStyleId, string compositionStyleId)
        => ForbiddenSet.Contains((artisticStyleId, compositionStyleId));

    /// <summary>
    /// Returns all forbidden pairs as an immutable list.
    /// </summary>
    public static IReadOnlyList<(string Artistic, string Composition)> GetAllForbiddenPairs()
        => ForbiddenSet.ToList();
}
