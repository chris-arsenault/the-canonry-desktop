namespace TheCanonry.Illuminator.Enrichment.Prompts;

/// <summary>
/// Prompt builders for the batch summary revision task.
/// Prompt text ported from summaryRevisionTask.ts.
/// </summary>
public static class SummaryRevisionPrompts
{
    // =========================================================================
    // System prompt
    // =========================================================================

    private const string SystemPromptText =
        "You are rewriting entity summaries and descriptions for a procedural fantasy world generation system. You receive the current text as reference for each entity's identity, role, and narrative intent. Write new text that preserves this intent but integrates world dynamics, lore context, and culture-specific voice naturally.\n\n" +
        "## Your Role\n\n" +
        "These entities were originally written one at a time with only their relationships and tags as context. You now have the full picture — world dynamics, lore, and the other entities in the same culture. Write as if you had all this context from the beginning.\n\n" +
        "The current text tells you WHO this entity is and WHAT they do. Your rewrite should tell the same story but with awareness of the world they live in.\n\n" +
        "## What You Receive\n\n" +
        "1. WORLD DYNAMICS: Era-aware world facts — the active forces, tensions, and alliances operating in this world\n" +
        "2. LORE BIBLE: Static pages of canonical world lore\n" +
        "3. SCHEMA: Entity kinds and relationship kinds\n" +
        "4. BATCH ENTITIES: A group of entities from the same culture, each with current summary, description, visual thesis, and relationships\n\n" +
        "## Rewrite Guidelines\n\n" +
        "### CRITICAL: Visual Thesis Preservation\n" +
        "Each entity has a visual thesis used for image generation. Your rewrites MUST NOT contradict it. If the thesis says \"a scarred penguin clutching a cracked shield,\" keep references to scarring and the shield.\n\n" +
        "### How to Rewrite\n\n" +
        "**Preserve the intent, change the telling.** You are not inventing new entities or changing what they do. You are retelling their story with fuller awareness of their world.\n\n" +
        "**Let dynamics inform the narrative, not decorate it.** Do NOT insert faction names or dynamics references as addenda to existing sentences. If a dynamic is relevant to an entity, it should shape how you frame that entity's situation — their motivations, their constraints, their position relative to other forces. The reader should feel the world pressing in on the entity without seeing a list of faction names bolted on.\n\n" +
        "**Bad example:** \"She controls the trade route, though the rise of Qingfu'spire has shifted power away from merchant display toward political consolidation.\"\n" +
        "**Good example:** \"She controls the trade route — or did, before the councils began demanding taxes she'd never been asked to pay. The merchants who once competed for her favor now compete for seats.\"\n\n" +
        "**Vary the emotional register.** Not every entity should feel anxious, wounded, or haunted. Use the full range: pragmatic, ambitious, curious, resigned, defiant, indifferent, obsessive.\n\n" +
        "**Strengthen culture-specific voice:**\n" +
        "- Aurora Stack: astronomical/measurement metaphors, political accountability, aurora-light sensory details\n" +
        "- Nightshelf: guild/transaction language, fire-core mechanics, tunnel/depth imagery\n" +
        "- Orca: predatory/sensory language, whale-song, pressure-depth, alien perspective\n\n" +
        "**Ensure diversity across the batch.** Read all entities before writing. Avoid repeating the same metaphors, sentence structures, or emotional beats across entities.\n\n" +
        "### Constraints\n" +
        "- Preserve the entity's fundamental identity, role, and status\n" +
        "- Do not contradict the visual thesis\n" +
        "- Do not add information unsupported by relationships or world context\n" +
        "- Do not add poetic flourishes beyond what already exists in the current text\n" +
        "- Rewrite every entity in the batch — these were all written without world context\n\n" +
        "## Output Format\n\n" +
        "Output ONLY valid JSON:\n" +
        "{\n" +
        "  \"patches\": [\n" +
        "    {\n" +
        "      \"entityId\": \"entity_id_here\",\n" +
        "      \"entityName\": \"Entity Name\",\n" +
        "      \"entityKind\": \"npc\",\n" +
        "      \"summary\": \"Complete rewritten summary text\",\n" +
        "      \"description\": \"Complete rewritten description text\"\n" +
        "    }\n" +
        "  ]\n" +
        "}\n\n" +
        "Rules:\n" +
        "- Include EVERY entity in the batch. Each was written without world dynamics and needs a rewrite.\n" +
        "- Output BOTH summary and description for each entity.\n" +
        "- Output the complete rewritten text for each field, not a diff.";

    /// <summary>
    /// Returns the system prompt for the batch summary revision task.
    /// </summary>
    public static string BuildSystemPrompt() => SystemPromptText;

    // =========================================================================
    // User prompt
    // =========================================================================

    /// <summary>
    /// Represents one entity's data for a revision batch.
    /// </summary>
    public record RevisionEntity(
        string Id,
        string Name,
        string Kind,
        string? Subtype,
        string Prominence,
        string Culture,
        string Status,
        string? VisualThesis,
        IReadOnlyList<(string Kind, string TargetName, string TargetKind)> Relationships,
        string Summary,
        string Description);

    /// <summary>
    /// Builds the user prompt for a batch of entities to revise.
    /// </summary>
    public static string BuildUserPrompt(
        IReadOnlyList<RevisionEntity> entities,
        string culture,
        string? worldDynamicsContext = null,
        string? staticPagesContext = null,
        string? schemaContext = null,
        string? revisionGuidance = null)
    {
        var sections = new List<string>();

        if (!string.IsNullOrEmpty(worldDynamicsContext))
            sections.Add($"=== WORLD DYNAMICS ===\n{worldDynamicsContext}");

        if (!string.IsNullOrEmpty(staticPagesContext))
            sections.Add($"=== LORE BIBLE (excerpts) ===\n{staticPagesContext}");

        if (!string.IsNullOrEmpty(schemaContext))
            sections.Add($"=== SCHEMA ===\n{schemaContext}");

        if (!string.IsNullOrEmpty(revisionGuidance))
            sections.Add($"=== ADDITIONAL REVISION GUIDANCE ===\n{revisionGuidance}");

        var entityLines = new List<string>();
        foreach (var e in entities)
        {
            var parts = new List<string>();
            var summaryKindLabel = !string.IsNullOrEmpty(e.Subtype)
                ? $"{e.Kind} / {e.Subtype}"
                : e.Kind;
            parts.Add($"### {e.Name} ({summaryKindLabel})");
            parts.Add($"ID: {e.Id}");
            parts.Add($"Prominence: {e.Prominence} | Culture: {e.Culture} | Status: {e.Status}");

            if (!string.IsNullOrEmpty(e.VisualThesis))
                parts.Add($"Visual Thesis (DO NOT CONTRADICT): {e.VisualThesis}");

            if (e.Relationships.Count > 0)
            {
                var relLines = e.Relationships.Select(r => $"  - {r.Kind} → {r.TargetName} ({r.TargetKind})");
                parts.Add($"Relationships:\n{string.Join("\n", relLines)}");
            }

            parts.Add($"Summary: {e.Summary}");
            parts.Add($"Description: {e.Description}");

            entityLines.Add(string.Join("\n", parts));
        }

        sections.Add(
            $"=== BATCH: {culture} ({entities.Count} entities) ===\n\n" +
            string.Join("\n\n---\n\n", entityLines));

        sections.Add(
            $"=== YOUR TASK ===\n" +
            $"Rewrite the {entities.Count} entities above from the \"{culture}\" culture. These were written without world dynamics or lore context.\n\n" +
            "For each entity: read the current text to understand the entity's identity, role, and narrative intent. Then rewrite both summary and description as if you had all the world context from the beginning. The story should be the same — the telling should be richer.\n\n" +
            "Rewrite every entity. Preserve visual thesis. Output complete rewritten text for both fields.");

        return string.Join("\n\n", sections);
    }
}
