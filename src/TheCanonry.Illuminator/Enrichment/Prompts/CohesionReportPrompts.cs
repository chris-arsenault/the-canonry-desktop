using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Prompts;

/// <summary>
/// Prompts for the cohesion validation step — full narrative quality check across 6 dimensions.
/// </summary>
public static class CohesionReportPrompts
{
    public static string SystemPrompt =>
        """
        You are a narrative quality validator for fictional chronicles. You evaluate generated narratives against their specifications across six dimensions. Be rigorous but fair — flag real problems, not stylistic preferences.

        Return ONLY valid JSON. No markdown wrapping.
        """;

    /// <summary>
    /// Build the cohesion validation user prompt from chronicle data.
    /// </summary>
    public static string BuildUserPrompt(
        string chronicleContent,
        IReadOnlyList<ChronicleRoleAssignment> roleAssignments,
        IReadOnlyList<string>? canonFacts = null,
        IReadOnlyList<(string SectionId, string SectionName, string Goal)>? sectionGoals = null,
        string? narrativeStyleName = null,
        string? themeGuidance = null)
    {
        var sections = new List<string>();

        // Cast list
        if (roleAssignments.Count > 0)
        {
            var castLines = roleAssignments.Select(ra =>
                $"- {ra.EntityName} ({ra.EntityKind}, {ra.Role}{(ra.IsPrimary ? ", primary" : "")})");
            sections.Add($"== CAST ==\n{string.Join("\n", castLines)}");
        }

        // Canon facts for factual accuracy check
        if (canonFacts is { Count: > 0 })
        {
            var factLines = canonFacts.Select(f => $"- {f}");
            sections.Add($"== CANON FACTS ==\n{string.Join("\n", factLines)}");
        }

        // Section goals for section-by-section validation
        if (sectionGoals is { Count: > 0 })
        {
            var goalLines = sectionGoals.Select(sg =>
                $"- Section \"{sg.SectionName}\" (id: {sg.SectionId}): {sg.Goal}");
            sections.Add($"== SECTION GOALS ==\n{string.Join("\n", goalLines)}");
        }

        // Narrative style context
        if (!string.IsNullOrEmpty(narrativeStyleName))
            sections.Add($"== NARRATIVE STYLE ==\n{narrativeStyleName}");

        // Theme guidance
        if (!string.IsNullOrEmpty(themeGuidance))
            sections.Add($"== THEME GUIDANCE ==\n{themeGuidance}");

        // Chronicle text
        sections.Add($"== CHRONICLE TEXT ==\n{chronicleContent}");

        // Task and output format
        sections.Add("""
            == TASK ==
            Evaluate the chronicle text across six dimensions. For each check, determine if it passes and provide concise notes explaining your assessment.

            1. **plotStructure**: Does the narrative have a clear arc — setup, rising action, climax, resolution? Are there structural problems like abrupt endings, missing transitions, or scenes that don't advance the story?
            2. **entityConsistency**: Are all cast members used consistently? Do characters behave in ways that match their roles and kinds? Are any cast members mentioned but never meaningfully used, or used in contradictory ways?
            3. **sectionGoals**: For each section goal listed above, did the chronicle fulfill that goal? (If no section goals were provided, return an empty array.)
            4. **resolution**: Does the chronicle reach a satisfying conclusion? Are plot threads resolved or deliberately left open? Does the ending feel earned rather than abrupt?
            5. **factualAccuracy**: Does the chronicle contradict any canon facts listed above? Are world details consistent with the provided context?
            6. **themeExpression**: Does the chronicle express its themes through action and character rather than exposition? Is there thematic coherence across the piece?

            Also identify specific issues — places where the text could be improved. Each issue should reference a check type and optionally a section.

            Finally, assign an overall score from 0 to 100 reflecting the overall quality.

            Return JSON:
            {
              "overallScore": 0-100,
              "checks": {
                "plotStructure": { "pass": true/false, "notes": "..." },
                "entityConsistency": { "pass": true/false, "notes": "..." },
                "sectionGoals": [
                  { "sectionId": "...", "pass": true/false, "notes": "..." }
                ],
                "resolution": { "pass": true/false, "notes": "..." },
                "factualAccuracy": { "pass": true/false, "notes": "..." },
                "themeExpression": { "pass": true/false, "notes": "..." }
              },
              "issues": [
                {
                  "severity": "critical" | "minor",
                  "sectionId": "optional section id",
                  "checkType": "plotStructure|entityConsistency|sectionGoals|resolution|factualAccuracy|themeExpression",
                  "description": "what the problem is",
                  "suggestion": "how to fix it"
                }
              ]
            }
            """);

        return string.Join("\n\n", sections);
    }
}
