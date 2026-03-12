namespace TheCanonry.Illuminator.Enrichment.Prompts;

/// <summary>
/// Prompt builders for the batch entity image style tagging task.
/// Prompt text ported from entityTagImageStylesTask.ts.
/// </summary>
public static class EntityTagImageStylesPrompts
{
    // =========================================================================
    // System prompt
    // =========================================================================

    private const string SystemPromptText =
        "You are a visual art director for an illustrated encyclopedia. " +
        "Think carefully about visual variety before assigning styles. " +
        "Respond only with the requested JSON.";

    /// <summary>
    /// Returns the system prompt for the entity image style tagging task.
    /// </summary>
    public static string BuildSystemPrompt() => SystemPromptText;

    // =========================================================================
    // Input types
    // =========================================================================

    /// <summary>
    /// An artistic style with its category grouping.
    /// </summary>
    public record StyleEntry(string Id, string Name, string Category);

    /// <summary>
    /// A composition style with its target category.
    /// </summary>
    public record CompositionEntry(string Id, string Name, string TargetCategory);

    /// <summary>
    /// A color palette with description and group.
    /// </summary>
    public record PaletteEntry(string Id, string Name, string Description, string Group);

    /// <summary>
    /// An entity's visual data for style ranking.
    /// </summary>
    public record EntityInput(
        string EntityId,
        string Kind,
        string Subtype,
        string VisualThesis,
        IReadOnlyList<string> VisualTraits);

    // =========================================================================
    // User prompt (batch tag prompt)
    // =========================================================================

    /// <summary>
    /// Builds the user prompt for batch entity image style tagging.
    /// </summary>
    public static string BuildUserPrompt(
        IReadOnlyList<EntityInput> entities,
        IReadOnlyList<StyleEntry> artisticStyles,
        IReadOnlyList<CompositionEntry> compositionStyles,
        IReadOnlyList<PaletteEntry> colorPalettes)
    {
        // Group artistic styles by category
        var stylesByCategory = artisticStyles
            .GroupBy(s => s.Category)
            .ToDictionary(g => g.Key, g => g.ToList());
        var styleList = string.Join("\n", stylesByCategory.Select(kvp =>
            $"### {kvp.Key}\n{string.Join("\n", kvp.Value.Select(s => $"  {s.Id} | {s.Name}"))}"));

        // Group composition styles by category
        var compsByCategory = compositionStyles
            .GroupBy(s => string.IsNullOrEmpty(s.TargetCategory) ? "universal" : s.TargetCategory)
            .ToDictionary(g => g.Key, g => g.ToList());
        var compList = string.Join("\n", compsByCategory.Select(kvp =>
            $"### {kvp.Key}\n{string.Join("\n", kvp.Value.Select(s => $"  {s.Id} | {s.Name}"))}"));

        // Group palettes by group
        var palettesByGroup = colorPalettes
            .GroupBy(p => p.Group)
            .ToDictionary(g => g.Key, g => g.ToList());
        var paletteList = string.Join("\n", palettesByGroup.Select(kvp =>
            $"### {kvp.Key}\n{string.Join("\n", kvp.Value.Select(p => $"  {p.Id} | {p.Name} | {p.Description}"))}"));

        var entityLines = string.Join("\n", entities.Select(e =>
        {
            var traits = e.VisualTraits.Count > 0 ? $" | {string.Join(", ", e.VisualTraits)}" : "";
            return $"[{e.EntityId}] ({e.Kind}/{e.Subtype}) {e.VisualThesis}{traits}";
        }));

        var artisticCount = artisticStyles.Count;
        var compositionCount = compositionStyles.Count;
        var paletteCount = colorPalettes.Count;

        return
            $"You are a visual art director assigning rendering styles to {entities.Count} entity illustrations for an illustrated encyclopedia of a fictional world.\n\n" +
            "Your goal is MAXIMUM VISUAL VARIETY. These images will be viewed together — monotony is the enemy.\n\n" +
            "For EACH entity, assign:\n\n" +
            "1. **tags**: 2-4 visual/atmospheric tags (lowercase, hyphenated) describing MOOD, LIGHTING, or VISUAL CHARACTER of the ideal image. Examples: intimate, dramatic-lighting, wide-vista, action, somber, crowded, isolated, mystical, violent, ceremonial, tender, ominous, serene, chaotic, regal, decrepit, lush, barren, nocturnal, golden-hour, ethereal, gritty, monumental, claustrophobic, pastoral, fiery, frozen, mournful, triumphant\n\n" +
            "2. **artisticStyleIds**: Top 3 ranked artistic style IDs, best fit first. Pick from at least 2 different categories.\n\n" +
            "3. **compositionStyleIds**: Top 3 ranked composition style IDs, best fit first. Pick from at least 2 different categories. DO NOT match composition to entity kind — a character in a sweeping landscape, a faction shown as a symbolic object study, a location as an intimate portrait detail are all encouraged. Surprising compositions create visual interest.\n\n" +
            "4. **colorPaletteIds**: Top 3 ranked color palette IDs, best fit first. Pick from at least 2 different groups.\n\n" +
            "## COVERAGE RULES — CRITICAL\n" +
            $"There are {artisticCount} artistic styles, {compositionCount} composition styles, and {paletteCount} color palettes available. A downstream algorithm will pick from your ranked lists, so every style that appears ANYWHERE in your top-3 lists becomes a candidate.\n" +
            "- **Every style/composition/palette in the library MUST appear in at least one entity's top 3 across this batch.** No exceptions. Before finalizing, verify each ID appears at least once.\n" +
            "- Actively look for entities that match niche styles. Factions suit map compositions. Artifacts suit object-study styles. Locations suit landscape or tilt-shift. Characters suit experimental styles like pixel-art or datamosh for variety. Search for an excuse to use every style.\n" +
            "- Do NOT default to the same safe picks for every entity. If you notice you keep reaching for the same favorites, stop and deliberately choose differently.\n" +
            "- Your 3 picks should span a range — don't pick 3 variations of the same aesthetic.\n\n" +
            "## SPREAD RULES\n" +
            $"Look at ALL {entities.Count} entities together before assigning:\n" +
            "- No single style should appear as #1 pick for more than ~15% of entities\n" +
            "- Composition choices should be DELIBERATELY VARIED — resist the urge to give characters portrait compositions and places landscape compositions. The unexpected is the goal.\n\n" +
            $"## Artistic Styles (grouped by category)\n{styleList}\n\n" +
            $"## Composition Styles (grouped by category)\n{compList}\n\n" +
            $"## Color Palettes (grouped by group)\n{paletteList}\n\n" +
            $"## Entities\n{entityLines}\n\n" +
            "Respond with ONLY a JSON array, no markdown fences:\n" +
            "[{\"entityId\":\"...\",\"tags\":[\"...\"],\"artisticStyleIds\":[\"id1\",\"id2\",\"id3\"],\"compositionStyleIds\":[\"id1\",\"id2\",\"id3\"],\"colorPaletteIds\":[\"id1\",\"id2\",\"id3\"]},...]";
    }
}
