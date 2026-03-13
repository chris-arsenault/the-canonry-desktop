namespace TheCanonry.Illuminator.Enrichment.Prompts;

/// <summary>
/// Input context for the temporal alignment check.
/// </summary>
public sealed class TemporalCheckInput
{
    public required string ChronicleContent { get; init; }
    public required string TemporalNarrative { get; init; }

    // Focal era
    public string? FocalEraName { get; init; }
    public string? TemporalScope { get; init; }
    public string? TemporalDescription { get; init; }

    // Era context
    public IReadOnlyList<EraInfo>? AllEras { get; init; }
    public IReadOnlyList<string>? TouchedEraIds { get; init; }
    public bool IsMultiEra { get; init; }
    public (int Start, int End) ChronicleTickRange { get; init; }

    // World dynamics (resolved text lines)
    public IReadOnlyList<string>? WorldDynamicsResolved { get; init; }

    public sealed record EraInfo(string Id, string Name, string? Summary, int StartTick, int EndTick, int Duration);
}

/// <summary>
/// Prompts for the temporal alignment check step — detects era misalignment,
/// anachronisms, and missing temporal grounding.
/// </summary>
public static class TemporalCheckPrompts
{
    public static string SystemPrompt =>
        "You are an editorial analyst checking whether a chronicle's narrative is temporally grounded in the correct era. You have access to the focal era, the temporal narrative (synthesized stakes), and the chronicle text. Your job is to identify passages where the chronicle's narrative contradicts, ignores, or is misaligned with the temporal context it was supposed to be grounded in. Some chronicles intentionally depict transitions between eras — these should be evaluated for how well they portray the shift, not penalized for containing elements of both eras.";

    /// <summary>
    /// Build the temporal check user prompt.
    /// </summary>
    public static string BuildUserPrompt(TemporalCheckInput input)
    {
        var sections = new List<string>();

        // Temporal context header
        var focalLine = $"**Focal Era:** {input.FocalEraName ?? "unknown"}";
        if (!string.IsNullOrEmpty(input.TemporalScope))
            focalLine += $" (scope: {input.TemporalScope})";

        var headerLines = new List<string> { "## Temporal Context", "", focalLine };
        if (!string.IsNullOrEmpty(input.TemporalDescription))
            headerLines.Add($"**Temporal Description:** {input.TemporalDescription}");

        sections.Add(string.Join("\n", headerLines));

        // Era timeline
        if (input.AllEras is { Count: > 0 })
        {
            var eraBlock = BuildEraContextBlock(input.AllEras, input.FocalEraName, input.TouchedEraIds ?? []);
            sections.Add($"**Era Timeline:**\n{eraBlock}");
        }

        // Era boundary analysis
        var boundaryBlock = BuildEraBoundaryBlock(input);
        if (!string.IsNullOrEmpty(boundaryBlock))
            sections.Add(boundaryBlock);

        // Temporal narrative
        sections.Add($"## Temporal Narrative (from Perspective Synthesis)\nThis is the synthesized narrative grounding \u2014 the story-specific stakes derived from world dynamics and era conditions. The chronicle should reflect these conditions:\n\n> {input.TemporalNarrative}");

        // World dynamics
        if (input.WorldDynamicsResolved is { Count: > 0 })
        {
            var dynamicsLines = input.WorldDynamicsResolved.Select((d, i) => $"{i + 1}. {d}");
            sections.Add($"## World Dynamics (as provided to generation)\n{string.Join("\n", dynamicsLines)}");
        }

        // Chronicle text
        sections.Add($"## Chronicle Text\n\n{input.ChronicleContent}");

        // Task
        sections.Add("""
            ---

            ## Task

            Analyze the chronicle text against the temporal context above. Look for:

            1. **Era Misalignment** — Does the chronicle reference conditions, events, or tensions from a DIFFERENT era than the focal era? Does it describe circumstances that belong to an earlier or later period?

            2. **Temporal Narrative Contradictions** — Does the chronicle contradict the synthesized stakes? Are the pressures, conditions, or world dynamics described in the temporal narrative absent, inverted, or replaced with incompatible ones?

            3. **Anachronistic Details** — Are there references to entities, places, factions, or customs that wouldn't exist or wouldn't apply during the focal era?

            4. **Missing Temporal Grounding** — Does the chronicle feel temporally unanchored — like it could happen in any era? Does it fail to use the specific conditions the temporal narrative establishes?

            5. **Era Boundary / Transition Assessment** — If this chronicle touches multiple eras or sits near an era boundary: Does it function as a transition narrative? Does it meaningfully depict the *shift* from one era's conditions to another? Are elements from adjacent eras serving the transition narrative, or do they appear as temporal errors? A chronicle depicting the collapse of one era's certainties into the next era's tensions is doing legitimate narrative work.

            ## Output Format

            For each issue found, cite the specific passage and explain the misalignment. If no issues are found, say so explicitly.

            Rate the overall temporal alignment: **Strong**, **Adequate**, **Weak**, or **Misaligned**.

            If the chronicle is a boundary/transition chronicle, also rate: **Transition: Strong / Adequate / Weak** — how well does it portray the era shift?

            Keep the total output under 800 words.
            """);

        return string.Join("\n\n", sections);
    }

    private static string BuildEraContextBlock(
        IReadOnlyList<TemporalCheckInput.EraInfo> allEras,
        string? focalEraName,
        IReadOnlyList<string> touchedEraIds)
    {
        return string.Join("\n", allEras.Select(era =>
        {
            var isFocal = era.Name == focalEraName ? " \u2190 FOCAL ERA" : "";
            var isTouched = touchedEraIds.Contains(era.Id) ? " (touched)" : "";
            var summarySuffix = era.Summary is not null ? $": {era.Summary}" : "";
            return $"- **{era.Name}** (years {era.StartTick}\u2013{era.EndTick}){isFocal}{isTouched}{summarySuffix}";
        }));
    }

    private static string BuildEraBoundaryBlock(TemporalCheckInput input)
    {
        if (input.AllEras is not { Count: > 1 }) return "";
        var focalEra = input.AllEras.FirstOrDefault(e => e.Name == input.FocalEraName);
        if (focalEra is null) return "";

        var sortedEras = input.AllEras.OrderBy(e => e.StartTick).ToList();
        var focalIdx = sortedEras.FindIndex(e => e.Id == focalEra.Id);
        if (focalIdx < 0) return "";

        var tickRange = input.ChronicleTickRange;
        var touchedEraIds = input.TouchedEraIds ?? [];
        var adjacent = new List<(TemporalCheckInput.EraInfo Era, int Boundary, string Direction)>();

        if (focalIdx > 0)
        {
            var prev = sortedEras[focalIdx - 1];
            var boundary = focalEra.StartTick;
            if (tickRange.Start - boundary < focalEra.Duration * 0.25 || touchedEraIds.Contains(prev.Id))
                adjacent.Add((prev, boundary, "preceding"));
        }

        if (focalIdx < sortedEras.Count - 1)
        {
            var next = sortedEras[focalIdx + 1];
            var boundary = focalEra.EndTick;
            if (boundary - tickRange.End < focalEra.Duration * 0.25 || touchedEraIds.Contains(next.Id))
                adjacent.Add((next, boundary, "following"));
        }

        if (adjacent.Count == 0 && !input.IsMultiEra) return "";

        var lines = new List<string> { "## Era Boundary Analysis" };

        if (input.IsMultiEra)
        {
            var touchedNames = touchedEraIds
                .Select(id => input.AllEras.FirstOrDefault(e => e.Id == id)?.Name)
                .Where(n => n is not null);
            lines.Add($"This chronicle touches {touchedEraIds.Count} eras: {string.Join(", ", touchedNames)}. Focal era: {focalEra.Name}.");
        }

        foreach (var (era, boundary, direction) in adjacent)
        {
            var pair = direction == "preceding"
                ? $"{era.Name} and {focalEra.Name}"
                : $"{focalEra.Name} and {era.Name}";
            lines.Add($"The chronicle's year range [{tickRange.Start}\u2013{tickRange.End}] is near the boundary at year {boundary} (between {pair}).");
            if (era.Summary is not null)
                lines.Add($"**{era.Name}:** {era.Summary}");
        }

        lines.Add("\nThis may be a **transition/boundary chronicle** depicting the shift between eras. Elements from adjacent eras may be narratively appropriate if they serve the transition.");
        return string.Join("\n", lines);
    }
}
