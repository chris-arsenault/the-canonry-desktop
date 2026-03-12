namespace TheCanonry.Illuminator.Enrichment.Prompts;

/// <summary>
/// Prompt builders for the visual trait palette expansion task.
/// Prompt text ported from paletteExpansionTask.ts.
/// </summary>
public static class PaletteExpansionPrompts
{
    // =========================================================================
    // System prompt
    // =========================================================================

    private const string SystemPromptText =
        "You curate visual trait palettes for worldbuilding. Your prompt contains:\n\n" +
        "WORLD DATA:\n" +
        "- World: Setting and tone\n" +
        "- Cultures: Visual traditions\n" +
        "- Subtypes: Allowed variations (use ONLY these exact values)\n" +
        "- Eras: Time periods\n\n" +
        "TASK DATA:\n" +
        "- Output: Subtype categories (6-10) + era categories (one per era)\n\n" +
        "Silhouette test: visible at 128px, an artist would draw it differently.";

    /// <summary>
    /// Returns the system prompt for the palette expansion task.
    /// </summary>
    public static string BuildSystemPrompt() => SystemPromptText;

    // =========================================================================
    // User prompt
    // =========================================================================

    /// <summary>
    /// Represents a culture's visual identity for the palette prompt.
    /// </summary>
    public record CultureContext(
        string Name,
        string? Description,
        IReadOnlyDictionary<string, string>? VisualIdentity);

    /// <summary>
    /// Represents an era for the palette prompt.
    /// </summary>
    public record EraContext(string Id, string Name, string? Description);

    /// <summary>
    /// Builds the user prompt for palette expansion.
    /// </summary>
    public static string BuildUserPrompt(
        string entityKind,
        string worldContext,
        IReadOnlyList<string> subtypes,
        IReadOnlyList<EraContext> eras,
        IReadOnlyList<CultureContext>? cultureContext = null)
    {
        if (subtypes.Count == 0)
            throw new ArgumentException(
                $"Cannot generate palette for {entityKind}: no subtypes defined. Define subtypes in the schema.",
                nameof(subtypes));

        // Culture section
        var cultureSection = "";
        if (cultureContext is { Count: > 0 })
        {
            var cultureLines = cultureContext.Select(c =>
            {
                var parts = new List<string> { c.Name };
                if (!string.IsNullOrEmpty(c.Description)) parts.Add(c.Description);
                if (c.VisualIdentity is { Count: > 0 })
                {
                    var traditions = string.Join("; ", c.VisualIdentity.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                    if (!string.IsNullOrEmpty(traditions)) parts.Add($"Visual: {traditions}");
                }
                return $"- {string.Join(" - ", parts)}";
            });
            cultureSection = $"\nCultures in this world:\n{string.Join("\n", cultureLines)}\n";
        }

        // Subtypes section
        var subtypesList = string.Join(", ", subtypes);
        var subtypesSection = $"\nALLOWED SUBTYPES for {entityKind} (use ONLY these exact values): {subtypesList}\n";

        // Eras section
        var erasSection = "";
        if (eras.Count > 0)
        {
            var eraLines = eras.Select(e =>
            {
                var descSuffix = !string.IsNullOrEmpty(e.Description) ? $" - {e.Description}" : "";
                return $"- {e.Id}: \"{e.Name}\"{descSuffix}";
            });
            erasSection = $"\nERAS in this world (use exact IDs):\n{string.Join("\n", eraLines)}\n";
        }

        // Dimension hints
        var dimensionHints = entityKind == "location"
            ? "shape/architecture, surface/texture, condition/age, atmosphere, activity, cultural markers"
            : "body shape, surface patterns, condition/scars, movement/gait, equipment, presence/aura";

        var erasTaskText = eras.Count > 0
            ? $"For EACH era listed above, create exactly ONE category specific to \"{entityKind}\".\n" +
              "- Era categories reflect material conditions or dominant activities of that time\n" +
              "- Era categories apply to ALL subtypes (leave subtypes empty)\n" +
              "- Use the exact era ID from the list above"
            : "No eras defined - skip era categories.";

        return
            $"Generate a visual trait palette for \"{entityKind}\" entities.\n\n" +
            $"WORLD: {(string.IsNullOrEmpty(worldContext) ? "A fantasy world." : worldContext)}\n" +
            $"{cultureSection}{subtypesSection}{erasSection}\n" +
            "TASK:\n" +
            "Generate TWO types of categories:\n\n" +
            $"## PART 1: Subtype Categories (6-10 categories)\n" +
            $"Cover the visual dimensions ({dimensionHints}).\n\n" +
            "CRITICAL RULES FOR SUBTYPES:\n" +
            $"- Every category MUST have a \"subtypes\" array with 1+ values from: [{subtypesList}]\n" +
            "- You can ONLY use these exact subtype values - do NOT invent new ones\n" +
            "- Each category should apply to 1-2 subtypes (be specific, not universal)\n" +
            "- Ensure good coverage: each subtype should have 3-5 categories that include it\n" +
            "- Categories that would \"apply to all\" should instead be split into subtype-specific variants\n\n" +
            $"## PART 2: Era Categories (one per era)\n" +
            $"{erasTaskText}\n\n" +
            "Each category must pass the SILHOUETTE TEST:\n" +
            "- Visible at 128px or in black silhouette\n" +
            "- An artist would draw this differently from other categories\n" +
            "- Changes shape, motion, or spatial presence (not just color/texture)\n\n" +
            "OUTPUT (JSON only):\n" +
            "{\n" +
            "  \"categories\": [\n" +
            "    {\n" +
            "      \"category\": \"Name\",\n" +
            "      \"description\": \"What this means visually\",\n" +
            "      \"examples\": [\"example 1\", \"example 2\", \"example 3\"],\n" +
            $"      \"subtypes\": [\"{subtypes[0]}\"],  // REQUIRED: 1+ subtypes from allowed list\n" +
            "      \"era\": null\n" +
            "    },\n" +
            "    {\n" +
            "      \"category\": \"Era-Specific Name\",\n" +
            $"      \"description\": \"How this era manifests for {entityKind}\",\n" +
            "      \"examples\": [\"example 1\", \"example 2\", \"example 3\"],\n" +
            "      \"subtypes\": [],  // Era categories: empty (apply to all)\n" +
            "      \"era\": \"era-id\"\n" +
            "    }\n" +
            "  ]\n" +
            "}";
    }
}
