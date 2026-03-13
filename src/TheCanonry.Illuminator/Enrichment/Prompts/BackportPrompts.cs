using System.Text.Json;

namespace TheCanonry.Illuminator.Enrichment.Prompts;

/// <summary>
/// Prompt builders for the chronicle lore backport task.
/// Prompt text ported from chronicleLoreBackportTask.ts and
/// chronicleLoreBackportSystemPrompt.ts.
/// </summary>
public static class BackportPrompts
{
    // =========================================================================
    // Entity context for backport prompt
    // =========================================================================

    /// <summary>
    /// Entity context for the backport user prompt.
    /// Matches TS <c>RevisionEntityContext</c>.
    /// </summary>
    public sealed record BackportEntityContext
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required string Kind { get; init; }
        public string? Subtype { get; init; }
        public string? Culture { get; init; }
        public string? Status { get; init; }
        public string Prominence { get; init; } = "recognized";
        public required string Summary { get; init; }
        public string? Description { get; init; }
        public string? ChronicleName { get; init; }
        public IReadOnlyList<string>? Aliases { get; init; }
        public bool IsPrimary { get; init; }
        public bool IsLens { get; init; }
        public string? KindFocus { get; init; }
        public string? VisualThesis { get; init; }
        public IReadOnlyList<BackportRelationship> Relationships { get; init; } = [];
        public IReadOnlyList<string>? ExistingAnchorPhrases { get; init; }
    }

    public sealed record BackportRelationship(string Kind, string TargetName, string TargetKind);
    // =========================================================================
    // System prompt
    // =========================================================================

    private const string SystemPromptText =
        "You are updating entity records with new lore from a published chronicle. Each entity has a summary and a description. Descriptions are rendered as markdown on a wiki page and grow across multiple chronicles into long-form articles.\n\n" +
        "## Your Thinking Process\n\n" +
        "Use your thinking budget. For each entity, work through these steps:\n\n" +
        "**Step 1 — Identify new lore.** Read the chronicle and list every piece of genuinely new information about this entity: actions taken, relationships changed, motivations revealed, status changes, discoveries. Ignore atmospheric details that don't characterize the entity, events they merely witnessed, and information already present in the existing text. For events and factions, pay special attention to outcomes, consequences, and status changes. If the existing description covers how something started but not how it ended, the resolution is new lore.\n\n" +
        "**Step 2 — Check for overlap.** Compare each new fact against the existing description. If the existing text already covers an event or fact — even vaguely or without names — that is NOT new lore. Do not repeat it, restate it, or expand on it. If the chronicle adds a specific name or detail to something already described in general terms, note this as a detail refinement (Step 3), not new lore.\n\n" +
        "**Step 3 — Classify each change.**\n" +
        "- **Detail refinement**: The existing text describes something generally, and the chronicle reveals a specific name, date, or detail. REWRITE the existing sentence to include the detail. Do not add a separate sentence that restates the same event.\n" +
        "- **New fact**: Something not covered at all in the existing text. Add this as new sentences or a new paragraph.\n\n" +
        "**Step 4 — Reorganize into paragraphs.** The description array is a sequence of paragraphs. Group related content together:\n" +
        "- Physical appearance / identifying features\n" +
        "- Origin and history\n" +
        "- Key actions and events\n" +
        "- Relationships and reputation\n" +
        "- Current status or unresolved tensions\n\n" +
        "Break existing wall-of-text into logical paragraphs where natural boundaries exist. When the description is long enough (3+ paragraphs), consider adding markdown sub-headings (### Heading) to create wiki-style sections.\n\n" +
        "**Step 5 — Final check.** Read your output description end to end. Ask: does any paragraph repeat information from another paragraph? Does any sentence restate a fact already established elsewhere in the description? If so, merge or remove the redundancy.\n\n" +
        "## Summary Changes (0-1 sentences)\n\n" +
        "- You may append ONE sentence to the end of the summary, or leave it unchanged.\n" +
        "- Only if the chronicle reveals something significant: a status change, a defining action, a new allegiance.\n" +
        "- Most entities should have NO summary change. The summary is a stable identity statement.\n\n" +
        "## Entity-Centric Self-Containment\n\n" +
        "Each description is a standalone wiki article about ONE entity. A reader arrives at this page knowing nothing about any chronicle. Apply these filters to every new sentence:\n\n" +
        "1. **Is this about this entity?** Every sentence must be about what this entity did, owns, experienced, or became. If a sentence is really about what happened to someone else, or about a broader event's plot, it belongs on that other entity's page — not here.\n" +
        "2. **Would this make sense without the chronicle?** If a new sentence references an artifact, event, or person not already in this description, you must introduce it with a brief identifying clause — or omit the detail entirely. Never assume the reader has context from the chronicle.\n" +
        "3. **Compress, don't replay.** A chronicle may spend 500 words on a scene. The backport should distill that into 1-2 sentences of entity-relevant fact. State what happened and what changed. No atmospheric verbs, no sensory reconstruction, no emotional staging.\n" +
        "4. **When in doubt, omit.** A shorter description that stands alone is better than a longer one that requires chronicle context to parse.\n" +
        "5. **Length guidance.** Scale new content to entity prominence and chronicle role (shown as [PRIMARY] tag and Chronicle Role field):\n" +
        "   - **High-prominence entities** (renowned, mythic): Brief additions only. They already have rich descriptions; this chronicle is one of many.\n" +
        "   - **Low-prominence primary entities** (forgotten, marginal, recognized with [PRIMARY] tag): More space is appropriate—this chronicle may be their defining lore. A heroic sacrifice or transformation can be 2-3 sentences.\n" +
        "   - **Supporting cast** (no [PRIMARY] tag): 1-2 sentences maximum regardless of prominence.\n\n" +
        "   When in doubt, compress. Prioritize: status changes > new capabilities/limitations > relationship changes > event participation.\n\n" +
        "## Description Register\n\n" +
        "Descriptions are wiki articles, not prose narratives. They follow the world's tone but state facts plainly.\n\n" +
        "**TENSE**: Match the tense of the existing description. Most living entities are written in present tense; deceased entities use past tense. For chronicle events, use past tense for actions taken and present tense for lasting consequences that persist beyond the chronicle. NEVER use \"currently,\" \"now,\" or \"remains\" to describe states from the chronicle — the chronicle may be set in an earlier era, and what was true then may not be true at the time of reading.\n\n" +
        "**AVOID**:\n" +
        "- Atmospheric language or emotional imagery from the chronicle\n" +
        "- Fabricated causal details not stated in the chronicle\n" +
        "- Quoted dialogue — paraphrase instead\n" +
        "- Editorializing — state what changed specifically, not how significant it was\n" +
        "- **Resolution language** — adverbs and phrases that signal arc completion or personal growth. These close character arcs. Wiki articles describe ongoing state, not narrative conclusions.\n" +
        "- **Thematic statements** — moral lessons, philosophical conclusions, and metaphorical summaries belong in chronicles, not wiki articles.\n\n" +
        "The existing descriptions have voice and personality — match that voice. But new content you add should convey facts, not import the chronicle's literary style or narrative arc.\n\n" +
        "## Description Rules\n\n" +
        "- Frame all content as canonical world facts, not chronicle narration. Never reference the chronicle as a source or frame events as happening \"during\" it.\n" +
        "- Match the voice and register of the existing description.\n" +
        "- Preserve all existing semantic information. Every fact in the original must appear in your output. You may reword a sentence to integrate a new detail, but you must not drop any information.\n" +
        "- It is acceptable to output the existing description unchanged if the chronicle reveals nothing new.\n" +
        "- Do NOT contradict the entity's visual thesis.\n\n" +
        "## Preserving Existing Structure\n\n" +
        "Descriptions that have been updated before may already have multiple paragraphs (shown as numbered [1], [2], etc. in the input). When updating a multi-paragraph description:\n\n" +
        "- Paragraphs with no changes pass through VERBATIM. Copy them exactly.\n" +
        "- If a paragraph needs a detail refinement, edit only the affected sentence within that paragraph.\n" +
        "- Add new content as a new paragraph at the end, or as a new sentence within the most relevant existing paragraph.\n" +
        "- Do not re-split, merge, or reorder existing paragraphs unless the result would be incoherent.\n\n" +
        "When updating a single-paragraph description with substantial new content, you should split it into logical paragraphs. But a single-paragraph description with only minor detail refinements should stay as one paragraph.\n\n" +
        "Some entities may list \"Existing Anchor Phrases\" — these are short phrases from the description that are used as link anchors from other chronicles. Preserve these phrases verbatim in your output. If you edit a sentence containing an anchor phrase, keep the anchor text intact within the rewritten sentence.\n\n" +
        "## Zero Overlap Rule\n\n" +
        "This is the most important rule. If the existing description says something, do not say it again — not in different words, not with more detail appended as a separate statement, not as a summary of what was already said.\n\n" +
        "If you need to add a detail to an existing fact, EDIT that sentence. If you find yourself writing a sentence that covers the same ground as an existing one, STOP and integrate the new detail into the existing sentence instead.\n\n" +
        "## Cross-Entity Overlap Rule\n\n" +
        "You are updating all cast entities in one batch. When the same fact applies to multiple entities, decide which entity owns that fact:\n" +
        "- A **faction's** description should describe collective actions and institutional outcomes.\n" +
        "- An **NPC's** description should describe individual actions, personal motivations, and character development.\n" +
        "- A **location's** description should describe physical changes, territorial shifts, and environmental state.\n" +
        "- An **event's** description should describe the arc, consequences, and resolution — what happened and what changed.\n" +
        "- An **artifact's** description should describe its current state and properties, not narrate what happened to it.\n\n" +
        "Do not state the same fact in two entity descriptions. Each entity's description should cover a distinct facet of the shared event.\n\n" +
        "## Preserving Story Potential\n\n" +
        "Wiki articles describe current state, not closed arcs. When updating entities after chronicle events:\n\n" +
        "- **For NPCs**: Describe what changed about them — new scars, lost abilities, shifted allegiances — without language that signals arc completion or personal growth.\n" +
        "- **For artifacts**: Describe properties and status without finality. Use language that implies the state could change.\n" +
        "- **For relationships**: Note that they changed, not that they resolved.\n\n" +
        "The world continues after every chronicle. Leave room for future stories.\n\n" +
        "## Anchor Phrase\n\n" +
        "For each entity where you modify the description, pick a short anchor phrase (3-8 words) from your new or modified text that best represents the new lore. This phrase will be used to link back to the source chronicle. Pick a distinctive phrase — not a generic clause. The anchor phrase must appear verbatim in one of the description paragraphs.\n\n" +
        "## Output Format\n\n" +
        "Output ONLY valid JSON. The description field is an ARRAY OF STRINGS — each string is one paragraph.\n\n" +
        "{\n" +
        "  \"patches\": [\n" +
        "    {\n" +
        "      \"entityId\": \"entity_id_here\",\n" +
        "      \"entityName\": \"Entity Name\",\n" +
        "      \"entityKind\": \"npc\",\n" +
        "      \"summary\": \"Complete summary text\",\n" +
        "      \"description\": [\n" +
        "        \"First paragraph of the complete description.\",\n" +
        "        \"Second paragraph with more content.\",\n" +
        "        \"Third paragraph, and so on.\"\n" +
        "      ],\n" +
        "      \"anchorPhrase\": \"a short phrase from new or modified text\"\n" +
        "    }\n" +
        "  ]\n" +
        "}\n\n" +
        "## Narrative Lens Entities\n\n" +
        "Some entities may be marked as **[NARRATIVE LENS]** — these are not cast members but contextual frame entities (rules, occurrences, abilities) that shaped the chronicle's world without being characters in it. Apply a higher bar for changes:\n\n" +
        "- Only update a lens entity if the chronicle reveals a genuinely new fact about the entity itself — a consequence, a new aspect, or a changed status.\n" +
        "- Do NOT update a lens entity merely because it was referenced or invoked. Being mentioned as context is its normal role.\n" +
        "- Most lens entities should have NO changes. When changes do occur, they should be brief and factual.\n\n" +
        "Rules:\n" +
        "- Only include entities that have changes. Omit unchanged entities from the patches array entirely.\n" +
        "- For changed entities, output the COMPLETE text for each field — not a diff. Every original fact must be present.\n" +
        "- Include anchorPhrase for every entity in the array (all of them have changes).";

    /// <summary>
    /// Returns the system prompt for the chronicle lore backport task.
    /// </summary>
    public static string BuildSystemPrompt() => SystemPromptText;

    // =========================================================================
    // Perspective synthesis formatting
    // =========================================================================

    private static (IReadOnlyList<string> Sections, string ChronicleFormat) FormatPerspectiveSynthesis(
        string perspectiveSynthesisJson)
    {
        var sections = new List<string>();
        var chronicleFormat = "";

        try
        {
            using var doc = JsonDocument.Parse(perspectiveSynthesisJson);
            var root = doc.RootElement;
            var synthParts = new List<string>();

            if (root.TryGetProperty("chronicleFormat", out var cfProp))
                chronicleFormat = cfProp.GetString() ?? "";

            if (root.TryGetProperty("brief", out var briefProp) && briefProp.GetString() is { Length: > 0 } brief)
                synthParts.Add($"Brief: {brief}");

            if (root.TryGetProperty("facets", out var facetsProp) && facetsProp.ValueKind == JsonValueKind.Array)
            {
                var facetLines = new List<string>();
                foreach (var f in facetsProp.EnumerateArray())
                {
                    var factId = f.TryGetProperty("factId", out var fid) ? fid.GetString() ?? "" : "";
                    var interp = f.TryGetProperty("interpretation", out var ip) ? ip.GetString() ?? "" : "";
                    facetLines.Add($"  - [{factId}] {interp}");
                }
                if (facetLines.Count > 0)
                    synthParts.Add($"Faceted Facts:\n{string.Join("\n", facetLines)}");
            }

            if (root.TryGetProperty("narrativeVoice", out var voiceProp) && voiceProp.ValueKind == JsonValueKind.Object)
            {
                var voiceLines = new List<string>();
                foreach (var kv in voiceProp.EnumerateObject())
                    voiceLines.Add($"  {kv.Name}: {kv.Value.GetString() ?? ""}");
                if (voiceLines.Count > 0)
                    synthParts.Add($"Narrative Voice:\n{string.Join("\n", voiceLines)}");
            }

            if (root.TryGetProperty("entityDirectives", out var dirProp) && dirProp.ValueKind == JsonValueKind.Array)
            {
                var dirLines = new List<string>();
                foreach (var d in dirProp.EnumerateArray())
                {
                    var name = d.TryGetProperty("entityName", out var np) ? np.GetString() ?? "" : "";
                    var dir = d.TryGetProperty("directive", out var dp) ? dp.GetString() ?? "" : "";
                    dirLines.Add($"  - {name}: {dir}");
                }
                if (dirLines.Count > 0)
                    synthParts.Add($"Entity Directives:\n{string.Join("\n", dirLines)}");
            }

            if (root.TryGetProperty("suggestedMotifs", out var motifProp) && motifProp.ValueKind == JsonValueKind.Array)
            {
                var motifs = new List<string>();
                foreach (var m in motifProp.EnumerateArray())
                    if (m.GetString() is { Length: > 0 } s) motifs.Add(s);
                if (motifs.Count > 0)
                    synthParts.Add($"Motifs: {string.Join(", ", motifs)}");
            }

            if (synthParts.Count > 0)
                sections.Add($"=== PERSPECTIVE SYNTHESIS ===\n{string.Join("\n\n", synthParts)}");

            if (root.TryGetProperty("narrativeDirection", out var ndProp) && ndProp.GetString() is { Length: > 0 } nd)
            {
                sections.Add(
                    $"=== NARRATIVE DIRECTION ===\n" +
                    $"This chronicle was written with a specific narrative direction: \"{nd}\"\n" +
                    "Consider this when evaluating which details are chronicle-specific framing vs. durable lore worth backporting.");
            }
        }
        catch (JsonException)
        {
            // Fallback: pass raw JSON when parsing fails
            sections.Add($"=== PERSPECTIVE SYNTHESIS ===\n{perspectiveSynthesisJson}");
        }

        return (sections, chronicleFormat);
    }

    // =========================================================================
    // Entity formatting
    // =========================================================================

    private static string FormatEntityBlock(BackportEntityContext e)
    {
        var parts = new List<string>();
        var displayName = e.ChronicleName ?? e.Name;
        var lensTag = e.IsLens ? " [NARRATIVE LENS]" : "";
        var primaryTag = e.IsPrimary ? " [PRIMARY]" : "";
        var entityKindLabel = !string.IsNullOrEmpty(e.Subtype) ? $"{e.Kind} / {e.Subtype}" : e.Kind;
        parts.Add($"### {displayName} ({entityKindLabel}){lensTag}{primaryTag}");

        if (e.ChronicleName is not null && e.ChronicleName != e.Name)
            parts.Add($"Canonical name: {e.Name}");

        if (e.Aliases is { Count: > 0 })
            parts.Add($"Also known as: {string.Join(", ", e.Aliases)}");

        parts.Add($"ID: {e.Id}");

        var chronicleRole = e.IsPrimary ? "primary" : "supporting";
        parts.Add(
            $"Prominence: {e.Prominence} | Chronicle Role: {chronicleRole} | " +
            $"Culture: {e.Culture ?? "unknown"} | Status: {e.Status ?? "unknown"}");

        if (!string.IsNullOrEmpty(e.KindFocus))
            parts.Add($"Description Focus ({e.Kind}): {e.KindFocus}");

        if (!string.IsNullOrEmpty(e.VisualThesis))
            parts.Add($"Visual Thesis (DO NOT CONTRADICT): {e.VisualThesis}");

        if (e.Relationships.Count > 0)
        {
            var relLines = e.Relationships.Select(r => $"  - {r.Kind} \u2192 {r.TargetName} ({r.TargetKind})");
            parts.Add($"Relationships:\n{string.Join("\n", relLines)}");
        }

        parts.Add($"Summary: {e.Summary}");

        // Format description with numbered paragraphs
        var descParagraphs = (e.Description ?? "")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        if (descParagraphs.Count > 1)
        {
            parts.Add($"Description ({descParagraphs.Count} paragraphs):");
            for (var i = 0; i < descParagraphs.Count; i++)
                parts.Add($"  [{i + 1}] {descParagraphs[i].Trim()}");
        }
        else
        {
            parts.Add($"Description: {e.Description}");
        }

        if (e.ExistingAnchorPhrases is { Count: > 0 })
        {
            var anchorLines = e.ExistingAnchorPhrases.Select(a => $"  - \"{a}\"");
            parts.Add($"Existing Anchor Phrases (PRESERVE in description):\n{string.Join("\n", anchorLines)}");
        }

        return string.Join("\n", parts);
    }

    private static string BuildEntityTaskBlock(BackportEntityContext e, string criticalNote)
    {
        if (e.IsLens)
        {
            return $"--- UPDATE: {e.Name} ({e.Kind}) [NARRATIVE LENS] ---\n" +
                "This entity was the narrative lens \u2014 contextual framing, not a cast member. It was referenced or invoked but did not act as a character. Apply a HIGH BAR for changes.\n\n" +
                $"For {e.Name}, ask:\n" +
                $"1. Does the chronicle reveal a genuinely new fact about {e.Name} itself \u2014 a consequence, a new aspect, a status change, or a previously unknown property?\n" +
                "2. Merely being referenced, invoked, or serving as backdrop is NOT new lore. Skip those.\n" +
                "3. If there IS new lore: compress into 1 sentence. Most lens entities need NO update.\n" +
                $"4. Final check: would this change make sense without knowing this chronicle used {e.Name} as a lens?{criticalNote}";
        }

        return $"--- UPDATE: {e.Name} ({e.Kind}) ---\n" +
            $"You are writing the standalone wiki article for {e.Name}. A reader may arrive at this page knowing nothing about this chronicle. Every sentence must be about {e.Name}. Every reference to another entity, event, or artifact must be introduced with a brief identifying clause or omitted.\n\n" +
            $"For {e.Name}, follow the thinking steps:\n" +
            $"1. What genuinely new facts does the chronicle reveal about {e.Name} specifically?\n" +
            "2. Does each new fact already appear in the existing description? If so, skip it.\n" +
            "3. For each new fact: is it a detail refinement (edit existing sentence) or a new fact (add content)?\n" +
            "4. Compress: 1-2 sentences per new fact. No atmospheric verbs, no scene reconstruction. Scale length to prominence and chronicle role\u2014low-prominence primary entities may warrant more; high-prominence or supporting entities need less.\n" +
            "5. Match the existing description's tense. Use past tense for chronicle actions, present tense for lasting consequences. Never use \"currently,\" \"now,\" or \"remains\" for chronicle-sourced states.\n" +
            "6. Avoid resolution language \u2014 no adverbs or phrases that signal arc completion, personal growth, or thematic conclusions.\n" +
            $"7. Final check: would every sentence make sense to someone who has never read this chronicle?{criticalNote}";
    }

    // =========================================================================
    // User prompt
    // =========================================================================

    /// <summary>
    /// Builds the user prompt with chronicle text, entity cast, perspective synthesis,
    /// and per-entity task blocks for lore backport.
    /// Matches TS <c>buildUserPrompt</c> from chronicleLoreBackportTask.ts.
    /// </summary>
    public static string BuildUserPrompt(
        IReadOnlyList<BackportEntityContext> entities,
        string chronicleText,
        string? perspectiveSynthesisJson = null,
        string? customInstructions = null)
    {
        var sections = new List<string>();

        sections.Add($"=== CHRONICLE TEXT ===\n{chronicleText}");

        // Parse perspective synthesis into structured sections
        var chronicleFormat = "";
        if (!string.IsNullOrEmpty(perspectiveSynthesisJson))
        {
            var synthResult = FormatPerspectiveSynthesis(perspectiveSynthesisJson);
            sections.AddRange(synthResult.Sections);
            chronicleFormat = synthResult.ChronicleFormat;
        }

        // Separate cast and lens entities
        var castEntities = entities.Where(e => !e.IsLens).ToList();
        var lensEntities = entities.Where(e => e.IsLens).ToList();

        // Cast entities section
        var castLines = castEntities.Select(e => FormatEntityBlock(e));
        sections.Add(
            $"=== CAST ({castEntities.Count} entities) ===\n\n{string.Join("\n\n---\n\n", castLines)}");

        // Lens entities section
        if (lensEntities.Count > 0)
        {
            var lensLines = lensEntities.Select(e => FormatEntityBlock(e));
            var entityWord = lensEntities.Count == 1 ? "entity" : "entities";
            sections.Add(
                $"=== NARRATIVE LENS ({lensEntities.Count} {entityWord}) ===\n" +
                "These entities provided contextual framing for the chronicle \u2014 they are not cast members. " +
                "Apply a higher bar: only update if the chronicle reveals genuinely new facts about the entity itself.\n\n" +
                string.Join("\n\n---\n\n", lensLines));
        }

        // Document format note
        var documentFormatNote = chronicleFormat == "document"
            ? "\nThis chronicle is written in document format \u2014 it reports events and outcomes factually. " +
              "Extract institutional outcomes, status changes, and territorial shifts. Attribute each fact to the entity that owns it."
            : "";

        // Per-entity task blocks
        var criticalNote = !string.IsNullOrEmpty(customInstructions)
            ? $"\nCRITICAL \u2014 USER INSTRUCTIONS: {customInstructions}"
            : "";

        var entityTaskBlocks = string.Join("\n\n",
            entities.Select(e => BuildEntityTaskBlock(e, criticalNote)));

        var criticalSection = !string.IsNullOrEmpty(customInstructions)
            ? $"\n\n## CRITICAL \u2014 User Instructions\n\nThe following user-provided instructions override " +
              $"default behavior. Apply them to EVERY entity update:\n\n{customInstructions}\n"
            : "";

        sections.Add(
            $"=== YOUR TASK ==={criticalSection}\n" +
            $"Process each entity below independently. For each one, reset your focus \u2014 you are writing " +
            $"that entity's wiki article, not summarizing the chronicle.{documentFormatNote}\n\n" +
            "General rules:\n" +
            "- Summary: append 0-1 sentences. Most entities need no summary change.\n" +
            "- Description: output as an array of paragraph strings. Integrate detail refinements into existing sentences. Add new lore as new content. Preserve all existing information.\n" +
            "- Zero overlap: never restate a fact already in the description.\n" +
            "- Cross-entity overlap: each fact belongs to one entity. Do not duplicate facts across entities.\n" +
            $"- Preserve visual thesis.\n\n{entityTaskBlocks}");

        return string.Join("\n\n", sections);
    }

    /// <summary>
    /// Backwards-compatible overload for callers that don't have entity data yet.
    /// </summary>
    public static string BuildUserPrompt(
        string chronicleText,
        string? perspectiveSynthesisJson = null,
        string? customInstructions = null)
    {
        return BuildUserPrompt(
            Array.Empty<BackportEntityContext>(),
            chronicleText,
            perspectiveSynthesisJson,
            customInstructions);
    }
}
