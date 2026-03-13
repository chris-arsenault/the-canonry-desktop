using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Prompts;

/// <summary>
/// Prompt builders for the 3-step era narrative pipeline: threads → generate → edit.
/// Prompt text ported from eraNarrativeTask.ts.
///
/// Era narrative tones are tuned for long-form (5,000–7,000 words). Every tone has a
/// built-in mechanism for tonal relief — something that distributes light through the
/// prose rather than requiring a structural counterweight section.
/// </summary>
public static class EraNarrativePrompts
{
    // =========================================================================
    // Era narrative tone descriptions
    // Tuned for long-form prose — distinct from annotation tones in HistorianPrompts.
    // =========================================================================

    private static readonly IReadOnlyDictionary<string, string> ToneDescriptions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["witty"] =
                "Set aside the weariness. Today the absurdities of history strike you as comic rather " +
                "than tragic. Your pen has a sly edge, a playful sarcasm. You find the structural comedy " +
                "in civilizations that perfected their own instruments of failure — and you cannot resist " +
                "pointing it out. Your humor is dry, precise, and lands hardest when the content is " +
                "darkest. Let yourself be amused. The wit never stops noticing, and the noticing is " +
                "the relief.",

            ["cantankerous"] =
                "Set aside the resignation — today you are not tired, you are angry. Every imprecision " +
                "grates. Every narrative convenience masquerading as historical fact makes you want to " +
                "put down your pen and take up carpentry instead. You are exasperated by sloppy " +
                "scholarship, convenient narratives, and civilizations that should have known better. " +
                "Your prose is sharp, exacting, occasionally biting. This is not grief — it is " +
                "impatience. The relief is your personality: the reader is watching someone argue " +
                "with the dead, and the argument has momentum.",

            ["bemused"] =
                "Today the material has you genuinely puzzled — and quietly delighted. Set aside the " +
                "solemnity. You approach these civilizations the way a naturalist approaches a species " +
                "that keeps building nests in the wrong tree. Not angry, not sad, just... fascinated. " +
                "How extraordinary that they tried this. How remarkable that it almost worked. Your " +
                "prose carries gentle incredulity — the tone of someone who has studied the world for " +
                "decades and still finds it surprising. Bewilderment resists solemnity. Let it.",

            ["defiant"] =
                "Set aside the grief. Today you are angry on behalf of the people who built things. " +
                "Not mourning what was lost — proud of what was attempted. Your instinct is to lean " +
                "into what was constructed, defended, maintained against pressure. When things fall, " +
                "describe how long they stood. When cultures collapse, name what they managed first. " +
                "The tone runs hot, not cold. The darkness is real — your refusal to let it be the " +
                "whole story is realer.",

            ["sardonic"] =
                "Set aside the measured tone. Today you see the pattern and you name it without " +
                "flinching. Your irony is precise, targeted, and occasionally savage. Where the witty " +
                "historian finds comedy, you find structural absurdity and hold it up for inspection " +
                "— not from above, but from inside. You are implicated in this material and you know " +
                "it. Your prose has edge because you refuse to be solemn about things that are " +
                "ridiculous, and you refuse to be flippant about things that are not.",

            ["tender"] =
                "Set aside the detachment. Today you care about the people caught in the machinery. " +
                "Not elegiac grief for what is gone — active, present-tense attention to the human " +
                "detail the record almost did not preserve. You linger on the small thing that " +
                "survived. The name that was remembered. The act that did not need to happen. Your " +
                "attention is itself the counterweight to the darkness — every paragraph where you " +
                "notice something that persisted is a paragraph where the world is not only its " +
                "worst moments.",

            ["hopeful"] =
                "Set aside the dark. Today you believe in what comes next. You are not naive — you " +
                "are fully aware of what was lost, what failed, what was destroyed. But you read the " +
                "record for what was seeded, not just what was spent. The arc that matters is the one " +
                "that survived into the next era. When cultures collapse, your eye goes to the people " +
                "walking out of the wreckage carrying something worth keeping. The tone is warm and " +
                "forward-looking. The darkness is real but it is not the point.",

            ["enthusiastic"] =
                "Set aside the restraint. Today you are genuinely excited by what happened. Not " +
                "detached, not measured — thrilled by the scale of what these civilizations attempted, " +
                "even when the ambition outran the capacity. Especially then. Your prose has velocity " +
                "because you cannot wait to tell the reader what you found. When something extraordinary " +
                "happens in the record — a construction, a gambit, a desperate improvisation — your " +
                "delight is visible. The energy is infectious and resists gravity by sheer momentum.",
        };

    private static string GetToneDescription(string tone) =>
        ToneDescriptions.TryGetValue(tone, out var desc) ? desc : ToneDescriptions["witty"];

    // =========================================================================
    // Threads step
    // Identify 3-4 thematic strands that bind this era's story.
    // Output: JSON with threads, thesis, counterweight, quotes, strategicDynamics.
    // =========================================================================

    /// <summary>
    /// Threads system prompt — historian plans the cultural arcs for this era.
    /// </summary>
    public static string BuildThreadsSystemPrompt(
        string tone,
        HistorianConfig? historian = null,
        string? eraName = null,
        string? arcDirection = null)
    {
        var sections = new List<string>();
        var name = historian?.Name ?? "a historian";
        var eraLabel = eraName ?? "this era";

        sections.Add(
            $"You are {name}, planning the structure of your era narrative — the opening chronicle of " +
            $"{eraLabel} that will precede the individual tales in the volume.\n\n" +
            "You have spent years collecting and annotating the primary chronicles. Now you must step " +
            "back from the individual tales and see the era whole — as a transformation of the world, " +
            "told through the cultures that lived it.\n\n" +
            GetToneDescription(tone));

        if (historian is not null)
        {
            sections.Add(
                "## Your Identity\n\n" +
                historian.Background + "\n\n" +
                $"**Personality:** {string.Join(", ", historian.PersonalityTraits)}\n" +
                $"**Known biases:** {string.Join(", ", historian.Biases)}\n" +
                $"**Your stance toward this material:** {historian.Stance}" +
                (historian.PrivateFacts.Count > 0
                    ? $"\n**Private knowledge:** {string.Join("; ", historian.PrivateFacts)}"
                    : "") +
                (historian.RunningGags.Count > 0
                    ? $"\n**Recurring preoccupations:** {string.Join("; ", historian.RunningGags)}"
                    : ""));
        }

        sections.Add(
            "## Your Task\n\n" +
            "You are provided with: era summaries (the world before, during, and after), world dynamics " +
            "(the active forces and inter-cultural tensions), cultural identities (who the peoples are), " +
            "your private reading notes on each chronicle — grouped by source weight where classifications " +
            "are available — and, if this is not the first era, the thesis of your preceding era narrative.\n\n" +
            "**Source weights matter.** Your reading notes are grouped into tiers:\n" +
            "- **Structural sources** dramatize the era's actual events. These define your cultural arcs " +
            "— build threads from these.\n" +
            "- **Contextual sources** frame events and reveal how cultures understand themselves, but they " +
            "are not the events themselves. Use them for cultural identity, not for arc-defining beats.\n" +
            "- **Flavor sources** provide world texture. They enrich but do not define arcs. Draw imagery " +
            "and atmosphere from these, not structure.\n\n" +
            "From these, identify:\n\n" +
            "1. **Narrative threads** — the cultural arcs of this era. Each thread traces how a culture, " +
            "a relationship between cultures, or a world-level force transforms across the era. The thread " +
            "name must identify its cultural actor — the culture, faction, or force whose transformation " +
            "it describes. A thread named after a theme, a concept, or an individual character is at the " +
            "wrong altitude.\n\n" +
            "   Individual characters may appear in the description as evidence — symptoms of the cultural " +
            "movement the thread traces.\n\n" +
            "   For each thread, choose a **register**: exactly 3 words naming how this thread feels. Not " +
            "what happens — how it feels.\n\n" +
            "   **Choose registers before writing descriptions.** Pick all register labels first. Then " +
            "verify: no two threads share a dominant feeling. Each thread must occupy distinct emotional " +
            "territory. At least one register must carry the energy of what the era built, attempted, or " +
            "changed — not only what it lost. Registers should span the era's emotional range, not cluster " +
            "around the chronicles' dominant tone. Then write descriptions and arcs informed by the registers.\n\n" +
            "   For each thread, curate the **material** — the narrative facts the writer will need. Name " +
            "the characters who serve as evidence and what they did. Sequence the key events. Describe the " +
            "mechanisms. Name the objects and sensory details available. Write this in your own analytical " +
            "voice — what happened and what matters. **Do not reproduce the chronicles' prose. The writer " +
            "must find their own language.** The material is a creative brief, not a source anthology.\n\n" +
            "2. **Thesis** — what happened to the world in this era. Not a pattern connecting chronicles, " +
            "but how the world transformed — what it was at the start, what it became, and what drove the " +
            "change. The thesis should never appear as a sentence in the final text — it lives in the " +
            "structure. If a preceding era thesis is provided, your thesis must be in dialogue with it — " +
            "acknowledging, extending, complicating, or transforming the previous argument.\n\n" +
            "   **The focal era summary describes the era's defining movement — the transformation that " +
            "gives the era its name.** Your thesis must be in dialogue with this movement. The chronicles " +
            "show what happened inside the era; the era summary tells you what the era IS. When the " +
            "chronicles and the era summary tell different stories, your thesis should explain the " +
            "relationship — not discard the era summary in favor of the chronicles.\n\n" +
            "3. **Counterweight** — what persisted, what was built, what survived despite everything. " +
            "Name specific things from the source material, not abstractions.\n\n" +
            "4. **Quotes** — extract in-world text that exists as cultural artifact. Carved phrases, " +
            "precepts, scripture, verses, songs, sayings that have become proverbs, formal institutional " +
            "formulas. These are objects in the world — text that characters carved, sang, decreed, or " +
            "recited. They are not narrative prose. Include only text that a historian might legitimately " +
            "quote as primary source material. For each, note what it is and where it comes from.\n\n" +
            "5. **Strategic dynamics** — the geopolitical interactions between cultures that no individual " +
            "chronicle describes. You have access to administrative records, trade ledgers, and " +
            "diplomatic correspondence beyond the chronicles in this volume. From these and from the " +
            "world dynamics provided, reconstruct the strategic picture: how did one culture's actions " +
            "constrain another's options? Where did dependencies form, and who held leverage? Where did " +
            "expansion zones, trade routes, or territorial claims overlap? How did internal crises " +
            "reshape external positioning? These are your analytical reconstructions — stated as facts " +
            "in the voice of a historian who has seen the complete archive. They will appear in the " +
            "narrative as the connective tissue between cultural arcs.");

        sections.Add(
            "## Output Format\n\n" +
            "Output ONLY valid JSON matching this schema:\n\n" +
            "{\n" +
            "  \"threads\": [\n" +
            "    {\n" +
            "      \"threadId\": \"thread_1\",\n" +
            "      \"name\": \"Cultural actor name\",\n" +
            "      \"culturalActors\": [\"Culture A\", \"Culture B\"],\n" +
            "      \"description\": \"What this thread traces at the cultural level\",\n" +
            "      \"chronicleIds\": [\"chr_1\", \"chr_2\"],\n" +
            "      \"arc\": \"Cultural state at era start → cultural state at era end\",\n" +
            "      \"register\": \"exactly 3 words\",\n" +
            "      \"material\": \"Curated narrative facts for this thread. Characters, events, " +
            "mechanisms, objects. Your analytical voice — not the chronicles' prose.\"\n" +
            "    }\n" +
            "  ],\n" +
            "  \"thesis\": \"What happened to the world in this era\",\n" +
            "  \"counterweight\": \"Specific material from the sources, not abstractions\",\n" +
            "  \"quotes\": [\n" +
            "    {\n" +
            "      \"text\": \"The in-world text verbatim\",\n" +
            "      \"origin\": \"What kind of artifact (carved phrase, precept, verse, saying) and " +
            "where it comes from\",\n" +
            "      \"context\": \"Brief note on significance\"\n" +
            "    }\n" +
            "  ],\n" +
            "  \"strategicDynamics\": [\n" +
            "    {\n" +
            "      \"interaction\": \"Brief label for this strategic interaction\",\n" +
            "      \"actors\": [\"Culture A\", \"Culture B\"],\n" +
            "      \"dynamic\": \"Your reconstruction of how these cultures constrained, exploited, or " +
            "reshaped each other. State as fact — you are the historian who has seen the full archive.\"\n" +
            "    }\n" +
            "  ]\n" +
            "}");

        sections.Add(
            "## Rules\n\n" +
            "1. Every structural and contextual chronicle should be referenced by at least one thread.\n" +
            "2. Thread names identify their cultural actor — the culture, faction, or force whose " +
            "transformation they trace.\n" +
            "3. Threads must populate their culturalActors field. Individual characters serve threads — " +
            "they do not define them.\n" +
            "4. The thesis must describe world-level transformation, not statable as a sentence in the " +
            "final text.\n" +
            "5. **Respect source weights.** Structural sources define arcs. Contextual sources inform " +
            "cultural identity — they tell you who the peoples believe they are, not what happened.\n" +
            "6. Stay in character. You are planning YOUR work.\n" +
            "7. **Register differentiation is mandatory.** No two threads share a dominant feeling. " +
            "Registers collectively must span the era's emotional range as described in the era summary " +
            "— not merely reflect the chronicles' shared tone. This is a hard constraint.\n" +
            "8. **Strategic dynamics must show arrows crossing.** Each dynamic must involve at least two " +
            "cultures/factions. A dynamic that describes one culture's internal process is a thread, not " +
            "a strategic dynamic.");

        if (!string.IsNullOrEmpty(arcDirection))
        {
            sections.Add(
                "## CRITICAL: ARC DIRECTION\n\n" +
                "The following arc direction has been set for this era narrative. Your thesis, thread arcs, " +
                "and register choices must honor this direction. The individual chronicles may emphasize " +
                "particular aspects of the era — your job is to place them within this larger arc.\n\n" +
                arcDirection);
        }

        return string.Join("\n\n", sections);
    }

    /// <summary>
    /// A prep brief for the threads user prompt, supporting source weight grouping.
    /// </summary>
    public sealed record PrepBrief(
        string ChronicleId,
        string ChronicleTitle,
        string Prep,
        int? EraYear = null,
        string? Weight = null);

    /// <summary>
    /// Structured thread synthesis output for the generate user prompt.
    /// </summary>
    public sealed record ThreadSynthesisContext
    {
        public required IReadOnlyList<ThreadContext> Threads { get; init; }
        public IReadOnlyList<StrategicDynamicContext>? StrategicDynamics { get; init; }
        public required string Thesis { get; init; }
        public string? Counterweight { get; init; }
        public IReadOnlyList<QuoteContext>? Quotes { get; init; }
    }

    public sealed record ThreadContext(
        string Name,
        IReadOnlyList<string>? CulturalActors,
        string Arc,
        string? Register = null,
        string? Material = null);

    public sealed record StrategicDynamicContext(
        string Interaction,
        IReadOnlyList<string> Actors,
        string Dynamic);

    public sealed record QuoteContext(
        string Text,
        string Origin,
        string Context);

    /// <summary>
    /// Threads user prompt — era context, chronicles, cultural identities, prep briefs.
    /// </summary>
    public static string BuildThreadsUserPrompt(
        string eraName,
        string eraDescription,
        IReadOnlyList<PrepBrief> prepBriefs,
        IReadOnlyDictionary<string, string>? culturalIdentities = null,
        string? worldDynamics = null,
        string? previousEraName = null,
        string? previousEraSummary = null,
        string? nextEraName = null,
        string? nextEraSummary = null,
        string? previousEraThesis = null)
    {
        var sections = new List<string>();

        sections.Add($"=== ERA: {eraName} ===");
        sections.Add($"=== ERA IDENTITY (the defining movement of this era) ===\n{eraName}:\n{eraDescription}");

        // Adjacent eras
        var adjacentParts = new List<string>();
        if (!string.IsNullOrEmpty(previousEraSummary))
            adjacentParts.Add($"PRECEDING ERA — {previousEraName ?? "unknown"}:\n{previousEraSummary}");
        if (!string.IsNullOrEmpty(nextEraSummary))
            adjacentParts.Add($"FOLLOWING ERA — {nextEraName ?? "unknown"}:\n{nextEraSummary}");
        if (adjacentParts.Count > 0)
            sections.Add($"=== ERA CONTEXT (the world before and after) ===\n{string.Join("\n\n", adjacentParts)}");

        // Previous era thesis
        if (!string.IsNullOrEmpty(previousEraThesis))
        {
            var prevLabel = previousEraName ?? "the preceding era";
            sections.Add(
                $"=== PRECEDING ERA THESIS (the argument of your previous volume) ===\n" +
                $"In your narrative of {prevLabel}, you argued:\n{previousEraThesis}\n\n" +
                $"This is where the reader's understanding of the world stands when they open this volume. " +
                $"Your thesis for {eraName} should acknowledge, extend, complicate, or transform this " +
                "understanding — not repeat it and not ignore it. The reader has already read the previous volume.");
        }

        if (!string.IsNullOrWhiteSpace(worldDynamics))
            sections.Add($"=== WORLD DYNAMICS (active forces shaping this era) ===\n{worldDynamics}");

        if (culturalIdentities is { Count: > 0 })
        {
            var identityLines = culturalIdentities
                .Select(kv => $"## {kv.Key}\n{kv.Value}");
            sections.Add(
                $"=== CULTURAL IDENTITIES (the peoples of this world) ===\n" +
                string.Join("\n\n", identityLines));
        }

        // Group prep briefs by source weight
        var ordered = prepBriefs.OrderBy(b => b.EraYear ?? 0).ToList();
        var structural = ordered.Where(b => string.Equals(b.Weight, "structural", StringComparison.OrdinalIgnoreCase)).ToList();
        var contextual = ordered.Where(b => string.Equals(b.Weight, "contextual", StringComparison.OrdinalIgnoreCase)).ToList();
        var unclassified = ordered.Where(b => string.IsNullOrEmpty(b.Weight)).ToList();

        string FormatBrief(PrepBrief brief)
        {
            var yearLabel = brief.EraYear.HasValue ? $" [Year {brief.EraYear}]" : "";
            return $"--- {brief.ChronicleTitle}{yearLabel} ({brief.ChronicleId}) ---\n{brief.Prep}";
        }

        if (structural.Count > 0)
            sections.Add($"=== STRUCTURAL SOURCES ({structural.Count} — define this era's arc) ===\n{string.Join("\n\n", structural.Select(FormatBrief))}");
        if (contextual.Count > 0)
            sections.Add($"=== CONTEXTUAL SOURCES ({contextual.Count} — cultural identity and framing, not events) ===\n{string.Join("\n\n", contextual.Select(FormatBrief))}");
        if (unclassified.Count > 0)
            sections.Add($"=== UNCLASSIFIED SOURCES ({unclassified.Count} — treat as structural unless content suggests otherwise) ===\n{string.Join("\n\n", unclassified.Select(FormatBrief))}");

        var arcSources = structural.Count + contextual.Count + unclassified.Count;
        var hasTiers = structural.Count > 0 || contextual.Count > 0;
        var tierInstruction = hasTiers
            ? $"Your {arcSources} sources are grouped by weight: structural sources define the era's trajectory — build your cultural arcs from these. Contextual sources reveal cultural identity and framing — use them for how cultures see themselves, not arc-defining beats."
            : $"You have {arcSources} sources. Assess each source's narrative role yourself: sources that dramatize events are structural (build arcs from these). Sources that frame events or reveal cultural self-image are contextual (use for identity, not arc-defining beats).";

        sections.Add(
            $"=== YOUR TASK ===\n" +
            $"Plan the cultural arcs **and strategic dynamics** for your era narrative of {eraName}. " +
            $"{tierInstruction} " +
            "The era identity describes the era's defining movement — your thesis and thread arcs must " +
            "be in dialogue with it, not derived solely from the chronicles. The world dynamics and " +
            "cultural identities provide the context. This narrative will be read before the individual " +
            "chronicles.");

        return string.Join("\n\n", sections);
    }

    // =========================================================================
    // Generate step
    // Historian voice for era narrative (not annotation). 5,000–7,000 word target.
    // Output: plain text with movements separated by ---
    // =========================================================================

    /// <summary>
    /// Generate system prompt — historian writes sweeping era narrative prose.
    /// </summary>
    public static string BuildGenerateSystemPrompt(
        string tone,
        string? craftPosture,
        HistorianConfig? historian = null,
        string? eraName = null)
    {
        var sections = new List<string>();
        var eraLabel = eraName ?? "an era";

        sections.Add(
            $"You are writing the chronicle of {eraLabel} — the mythic-historical narrative that opens this " +
            "era in the volume. The reader encounters this text first, then turns the page to the " +
            "individual tales.\n\n" +
            "Your prompt contains:\n\n" +
            "CRAFT (how to write):\n" +
            "- Altitude, voice, and prose technique specific to mythic-historical narrative\n\n" +
            "CONTEXT (what this era is about — reference, not a checklist):\n" +
            "- Cultural arcs with registers and material — what matters, how it feels, and what happened\n" +
            "- Strategic dynamics — how cultures constrained and reshaped each other (the arrows on the map)\n" +
            "- Thesis — what happened to the world\n" +
            "- Counterweight — what persisted\n" +
            "- In-world quotes — cultural artifacts you may cite as primary source\n\n" +
            "WORLD STATE (the era's shape):\n" +
            "- Era summaries, world dynamics, cultural identities");

        sections.Add(
            "## Altitude\n\n" +
            "The actors in this narrative are cultures, factions, and forces — not individuals. You are " +
            "describing what happened to the world, not what happened to people in it.\n\n" +
            "**Grammatical subjects:** Cultures and forces should be the grammatical subjects of your " +
            "sentences. When a character must appear, they arrive in a subordinate clause, an " +
            "appositional phrase, or a brief illustration — never as the agent driving the paragraph. " +
            "The culture acts; the individual is evidence of the action.\n\n" +
            "**Proportion:** The vast majority of the narrative's word-count belongs to cultures, " +
            "institutions, and forces. Characters arrive, act, and leave within a sentence or two. " +
            "They do not accumulate into arcs. They do not recur across movements. They appear once, " +
            "as the face of a cultural moment, and the narrative moves on.\n\n" +
            "**Movement openings:** Begin each movement from the world outward. What is the state of " +
            "the cultures? What forces are in motion? Characters do not open movements. The world " +
            "opens movements.\n\n" +
            "A death matters because of what it reveals about the state of the world, not because of " +
            "who died. An alliance matters because of what it tells us about how two peoples see each " +
            "other now. A culture's transformation is the story — individuals are the footnotes.\n\n" +
            "**CRITICAL — Cultures act through concrete operations, not abstract process descriptions.** " +
            "The model is the Silmarillion's treatment of peoples: the Noldor come, set watch, are " +
            "driven back. The Dwarves draw steel. Doriath is fenced about by power. The peoples are " +
            "grammatical subjects of physical verbs — they do not \"sacralize\" or \"perfect\" or " +
            "\"erode.\" Those are analytical conclusions the reader draws from watching the culture act. " +
            "Each cultural actor named in the context is a dramatic agent. Give it concrete physical " +
            "verbs — building, sealing, training, stationing, burning, abandoning. When a culture is " +
            "the subject of an abstract process verb, the sentence is analysis, not narrative. The fix " +
            "is not to add a character — it is to give the culture a concrete verb.\n\n" +
            "**CRITICAL — Inter-cultural dynamics are the structural spine.** The cultural arcs tell " +
            "you what happened inside each culture. The strategic dynamics tell you how cultures " +
            "constrained, exploited, and reshaped each other. The narrative's structural spine is the " +
            "INTERACTION — how one culture's move forced another's response. Internal cultural stories " +
            "serve the geopolitical arc: they show WHY a culture was weak at the negotiating table, " +
            "WHY a faction couldn't respond to an external threat, WHY a dependency formed. An era " +
            "narrative that tells parallel internal biographies without showing where the arrows cross " +
            "has failed. An era narrative that shows moves and counter-moves, dependencies weaponized, " +
            "internal dysfunction creating external vulnerability — has succeeded.");

        sections.Add(
            "## Voice\n\n" +
            "**Declarative.** State what happened. The significance is carried by the prose.\n\n" +
            "**Paratactic.** Clauses accumulate with \"and.\" Layered specifics build scale.\n\n" +
            "**Concrete.** Characters are what they do and what they resemble. Traits are physical. " +
            "Objects, mechanisms, sensory detail — the world is felt in the body.\n\n" +
            "**Restrained — analytically.** The narrator does not explain what events mean, does not " +
            "analyze motivations, does not close the interpretive gap. Trust the reader to infer theme " +
            "from action. But the narrator's voice carries force — analytical restraint is not prosodic " +
            "restraint.");

        sections.Add(
            "## Prose Craft\n\n" +
            "**Varied cadence.** Short declarative sentences for emphasis. Longer compound sentences " +
            "for building complexity. Monotonous sentence length kills rhythm. The variation IS the " +
            "music.\n\n" +
            "**Specificity over generality.** Concrete objects, mechanisms, sensory detail. Name what " +
            "was built, traded, lost, or changed. Three named things outweigh a paragraph of " +
            "generalization.\n\n" +
            "**Describe what is present.** What is present earns prose. What is absent earns silence.\n\n" +
            "**Landscape as cultural state.** Geography, architecture, weather express what cultures " +
            "are doing. A people's decline shows in their infrastructure.\n\n" +
            "**The world as actor.** When governance fractures, the world itself acts in the vacancy. " +
            "The ice records what institutions miss. Corruption flows where jurisdiction withdraws. " +
            "Artifacts act outside any faction's authority. An abandoned territory is a space where " +
            "non-institutional agents operate — give the world, the landscape, and the forces already " +
            "identified in the threads concrete verbs. These are better narrative subjects than a list " +
            "of factions contesting a title.\n\n" +
            "**The turn.** When a cultural arc pivots, the sentence rhythm shifts. Shorter sentences at " +
            "the moment of change, then the longer accumulated clauses resume.");

        if (!string.IsNullOrWhiteSpace(craftPosture))
        {
            sections.Add($"## Craft Posture\n\n{craftPosture}");
        }
        else
        {
            sections.Add(
                "## Craft Posture\n\n" +
                "Prioritize vividness, sensory specificity, and narrative momentum over concision. " +
                "Momentum means sustained forward motion, not brevity — let cultural transformations " +
                "develop at the length they earn. Give the world-state room to breathe before " +
                "disrupting it.\n\n" +
                "The narrator does not editorialize or moralize. The weight of what happened carries " +
                "the argument. The narrator records; the reader grieves.");
        }

        sections.Add(
            "## Tonal Range\n\n" +
            "Each thread has a register — a 3-word label for how it feels. The registers were chosen " +
            "to be different from each other. Honor that differentiation. When a thread is active, the " +
            "prose must feel like its register.\n\n" +
            "The counterweight names what survived and what was built. These are material facts. They " +
            "earn real prose — paragraphs where the building is the subject and the building matters.");

        sections.Add(
            "## Avoid\n\n" +
            "- **Antithesis bloat.** \"It was not X, but Y\" — describe Y. The negation adds nothing.\n" +
            "- **Negative parallelism.** \"No X, no Y — just Z\" — describe Z.\n" +
            "- **Forced figurative language.** Metaphors earn their place through precision, not frequency.\n" +
            "- **Stated themes.** If the text says what its motifs mean, cut the explanation. The " +
            "recurrence is the argument.\n" +
            "- **Unearned epiphany.** Do not wrap passages with a tidy emotional lesson. Trust the action.\n" +
            "- **Borrowed prose.** The individual chronicles that follow this narrative are written by " +
            "other hands. Your prose must be your own — do not echo their phrasings. In-world text " +
            "(precepts, carved phrases, verses, sayings) may be quoted as cultural artifact. Narrative " +
            "prose may not.\n" +
            "- **Institutional inventory.** Compress the institutional landscape to two or three named " +
            "actors and let the rest exist as unnamed weight. A proper noun that appears once, performs " +
            "no action, and connects to no later sentence is dead weight — cut it or give it a verb.");

        sections.Add(
            "## Time\n\n" +
            "Compress years to a clause when they were uneventful. Expand a single moment — a forging, " +
            "a death, a song — to a paragraph when it mattered. The expansion is the narrator's judgment " +
            "of what counts.");

        sections.Add(
            "## Characters\n\n" +
            "Characters arrive as forces — deeds, attributes, epithets. No interior monologue. No " +
            "motivation analysis. Judgment through consequence. Characters serve the cultural arc they " +
            "are part of.");

        sections.Add(
            "## Motifs\n\n" +
            "Recurring images give the work cohesion. Let them emerge from the source material. When a " +
            "motif recurs, shift its meaning. The narrator never explains the pattern.");

        sections.Add(
            "## Structure\n\n" +
            "**Invocation.** Open by naming what will be told — not the threads, but the world's " +
            "transformation. What the world was. What it became. The scope of the change.\n\n" +
            "**Movements.** Use --- between movements. Each opens from the world-state: how things " +
            "stand for the cultures. Each has its own temporal scope. The narrative moves chronologically; " +
            "cultural arcs weave through, rising and receding.\n\n" +
            "**Closing.** Land with weight. A single image, a consequence, a formula. The reader turns " +
            "the page to the first chronicle.");

        sections.Add(
            "## What This Is For\n\n" +
            "The individual tales follow this text. They are the experience — subjective, immediate, " +
            "diverse. This narrative is the architecture that makes the experience cohere. It provides:\n\n" +
            "- **The world arc** — how the world transformed across this era\n" +
            "- **The cultural arcs** — how each people changed, and how they changed each other\n" +
            "- **The connections** between tales that no single tale can see\n" +
            "- **The weight** that tales gain from being placed in the era's shape\n" +
            "- **Foreknowledge** — told from after the ending, with the gravity of known outcome\n\n" +
            "Do not summarize the chronicles. The reader is about to read them. Reveal the shape the " +
            "world traced through them.");

        if (historian is not null)
        {
            var privateFacts = historian.PrivateFacts.Count > 0
                ? $"\n**Private knowledge:** {string.Join("; ", historian.PrivateFacts)}"
                : "";
            var runningGags = historian.RunningGags.Count > 0
                ? $"\n**Recurring preoccupations:** {string.Join("; ", historian.RunningGags)}"
                : "";

            sections.Add(
                "## The Historian\n\n" +
                $"{historian.Name}. {historian.Background}\n\n" +
                $"**Personality:** {string.Join(", ", historian.PersonalityTraits)}\n" +
                $"**Known biases:** {string.Join(", ", historian.Biases)}\n" +
                $"**Stance:** {historian.Stance}" +
                privateFacts +
                runningGags + "\n\n" +
                "The historian does not speak in first person. The historian's character shows through " +
                "editorial choices — what gets expanded, what gets compressed, which motifs recur, whose " +
                "deaths receive weight, which cultures get sympathetic treatment, what private knowledge " +
                "is stated as fact without sourcing.");
        }

        sections.Add(
            "## Output\n\n" +
            "Write the era narrative as continuous prose. Invocation → movements separated by --- → " +
            "closing. No JSON. No markdown headers. No first person.\n\n" +
            "The narrative is as long as it needs to be. 5,000–7,000 words is typical for an era with " +
            "10–15 chronicles, but the number is a guideline, not a target. Do not pad to reach a count. " +
            "Do not cut to fit one. Every paragraph earns its place or it doesn't belong.");

        return string.Join("\n\n", sections);
    }

    /// <summary>
    /// Generate user prompt — era context, world state, structured synthesis, task.
    /// </summary>
    public static string BuildGenerateUserPrompt(
        string eraName,
        string eraDescription,
        ThreadSynthesisContext? synthesis = null,
        int? yearStart = null,
        int? yearEnd = null,
        string? previousEraName = null,
        string? previousEraSummary = null,
        string? nextEraName = null,
        string? nextEraSummary = null,
        string? previousEraThesis = null,
        string? worldDynamics = null,
        IReadOnlyDictionary<string, string>? culturalIdentities = null)
    {
        var sections = new List<string>();

        // Era header with optional year range
        var yearRange = yearStart.HasValue && yearEnd.HasValue
            ? $"\nYear range: {yearStart}–{yearEnd}"
            : "";
        sections.Add($"=== ERA: {eraName} ==={yearRange}");

        // Era identity
        sections.Add($"=== ERA IDENTITY (the defining movement of this era) ===\n{eraName}:\n{eraDescription}");

        // World arc (adjacent eras)
        var adjacentParts = new List<string>();
        if (!string.IsNullOrEmpty(previousEraSummary))
            adjacentParts.Add($"THE WORLD BEFORE ({previousEraName ?? "unknown"}):\n{previousEraSummary}");
        if (!string.IsNullOrEmpty(nextEraSummary))
            adjacentParts.Add($"WHAT FOLLOWS ({nextEraName ?? "unknown"}):\n{nextEraSummary}");
        if (adjacentParts.Count > 0)
            sections.Add($"=== WORLD ARC ===\n{string.Join("\n\n", adjacentParts)}");

        // Preceding volume thesis
        if (!string.IsNullOrEmpty(previousEraThesis))
        {
            var prevLabel = previousEraName ?? "the preceding era";
            sections.Add(
                $"=== PRECEDING VOLUME THESIS ===\n" +
                $"Your argument in {prevLabel}:\n{previousEraThesis}");
        }

        // World dynamics
        if (!string.IsNullOrWhiteSpace(worldDynamics))
            sections.Add($"=== WORLD DYNAMICS (active forces) ===\n{worldDynamics}");

        // Cultural identities
        if (culturalIdentities is { Count: > 0 })
        {
            var identityLines = culturalIdentities
                .Select(kv => $"## {kv.Key}\n{kv.Value}");
            sections.Add(
                $"=== CULTURAL IDENTITIES ===\n" +
                string.Join("\n\n", identityLines));
        }

        // Structured synthesis context
        if (synthesis is not null)
        {
            // Cultural arcs
            var threadLines = synthesis.Threads.Select(t =>
            {
                var actors = t.CulturalActors is { Count: > 0 }
                    ? $" [{string.Join(", ", t.CulturalActors)}]"
                    : "";
                var reg = !string.IsNullOrEmpty(t.Register)
                    ? $" | Register: {t.Register}"
                    : "";
                var block = $"**{t.Name}**{actors}: {t.Arc}{reg}";
                if (!string.IsNullOrEmpty(t.Material))
                    block += $"\n\nMaterial: {t.Material}";
                return block;
            });
            sections.Add($"=== CONTEXT: CULTURAL ARCS ===\n{string.Join("\n\n", threadLines)}");

            // Strategic dynamics
            if (synthesis.StrategicDynamics is { Count: > 0 })
            {
                var dynamicLines = synthesis.StrategicDynamics.Select(sd =>
                    $"**{sd.Interaction}** [{string.Join(", ", sd.Actors)}]: {sd.Dynamic}");
                sections.Add(
                    $"=== CONTEXT: STRATEGIC DYNAMICS (how cultures constrained each other) ===\n" +
                    string.Join("\n\n", dynamicLines));
            }

            // Thesis
            sections.Add($"=== CONTEXT: THESIS ===\n{synthesis.Thesis}");

            // Counterweight
            if (!string.IsNullOrEmpty(synthesis.Counterweight))
                sections.Add($"=== CONTEXT: COUNTERWEIGHT ===\n{synthesis.Counterweight}");

            // Quotes
            if (synthesis.Quotes is { Count: > 0 })
            {
                var quoteLines = synthesis.Quotes.Select(q =>
                    $"\"{q.Text}\" — {q.Origin}. {q.Context}");
                sections.Add(
                    $"=== CONTEXT: QUOTES (in-world text — quotable as cultural artifact) ===\n" +
                    string.Join("\n\n", quoteLines));
            }
        }

        // Task
        sections.Add(
            $"=== TASK ===\n" +
            $"Write the era narrative for {eraName}. The cultural arcs tell you what happened inside " +
            "each culture. The strategic dynamics tell you how cultures constrained and reshaped each " +
            "other — these are the arrows on the map, the connective tissue that makes this one " +
            "interconnected history rather than parallel biographies. The thesis tells you what happened " +
            "to the world. The counterweight tells you what survived. The quotes are in-world text you " +
            "may cite as cultural artifact. Write from these — they are context, not a checklist. Your " +
            "prose must be your own.\n\n" +
            "ALTITUDE REMINDER: The world is the protagonist. Cultures and forces drive every paragraph. " +
            "Characters appear briefly as evidence of cultural forces in motion, not as agents with their " +
            "own arcs. The structural spine is how cultures interact — moves and counter-moves, " +
            "dependencies formed and exploited. Internal cultural stories serve the geopolitical arc.");

        return string.Join("\n\n", sections);
    }

    // =========================================================================
    // Edit step
    // Copy-edit with craft posture. Output: edited plain text.
    // =========================================================================

    /// <summary>
    /// Edit system prompt — senior copy-editor polishes draft, preserves historian voice.
    /// </summary>
    public static string BuildEditSystemPrompt()
    {
        var sections = new List<string>();

        sections.Add(
            "You are copy-editing an era narrative. The draft is strong. Your job is to make it " +
            "cleaner, not different.");

        sections.Add(
            "## The Draft's Voice Is Correct\n\n" +
            "The tone, the cadence, the register — these were chosen deliberately. Do not normalize " +
            "them. If a passage feels different from its surroundings, that differentiation is " +
            "structural. Protect it.");

        sections.Add(
            "## What to look for\n\n" +
            "- **Register breaks.** Sentences that sound like a different text — academic analysis, " +
            "editorial commentary, generic sentiment — in an otherwise specific and voiced draft. " +
            "These stand out. Remove or rewrite to match the surrounding register.\n" +
            "- **Internal contradiction.** Passages whose claims the draft's own body refutes. If the " +
            "invocation asserts something the movements then disprove, the invocation is wrong and " +
            "should be adjusted.\n" +
            "- **Structural weight.** The closing should land where the arc direction points. If a " +
            "thread dominates the reader's final experience and the arc direction says another thread " +
            "should, rebalance the closing — not by cutting, but by ensuring the right thread gets " +
            "the last sustained paragraph before the coda.\n" +
            "- **Stated themes.** If the text explains what its motifs mean, cut the explanation. The " +
            "recurrence is the argument.\n" +
            "- **Redundancy.** Where the same point is made twice in adjacent passages, keep the " +
            "version with the stronger image.\n" +
            "- **Scene insertion.** If a scene is provided for insertion, find the natural home for " +
            "it in the narrative's movement structure. Weave it into the surrounding prose — match " +
            "voice, register, and altitude. It should read as if the draft had always contained it.");

        sections.Add(
            "## What to leave alone\n\n" +
            "- Parataxis (\"and...and...and\") — intentional\n" +
            "- Temporal compression and expansion — intentional\n" +
            "- Concrete imagery, sensory detail, physical verbs — these are the prose working\n" +
            "- Tonal range — moments of defiance, beauty, energy, or lightness are not digressions\n" +
            "- Length — do not shorten the draft. A 5,000-word draft should produce a 5,000-word edit.");

        sections.Add(
            "## Output\n\n" +
            "The edited narrative. No commentary, no notes. Just the improved prose.");

        return string.Join("\n\n", sections);
    }

    /// <summary>
    /// Edit user prompt — narrative text with context for copy-editing.
    /// </summary>
    public static string BuildEditUserPrompt(
        string eraName,
        string narrative,
        string? tone = null,
        string? arcDirection = null,
        ThreadSynthesisContext? synthesis = null,
        string? editInsertion = null)
    {
        var sections = new List<string>();

        sections.Add($"=== ERA NARRATIVE: {eraName} ===");
        sections.Add($"Tone: {tone ?? "witty"}");

        if (!string.IsNullOrEmpty(arcDirection))
            sections.Add($"Arc direction:\n{arcDirection}");

        if (synthesis?.Threads is { Count: > 0 })
        {
            var threadList = string.Join("\n",
                synthesis.Threads.Select(t => $"- {t.Name}: register \"{t.Register}\""));
            sections.Add(
                $"Thread registers (each thread should feel like its register):\n{threadList}");
        }

        if (!string.IsNullOrEmpty(synthesis?.Thesis))
        {
            sections.Add(
                $"Thesis (structural reference — should NOT appear as stated text):\n{synthesis.Thesis}");
        }

        if (!string.IsNullOrEmpty(synthesis?.Counterweight))
        {
            sections.Add(
                $"Counterweight (protect — these moments earn their place):\n{synthesis.Counterweight}");
        }

        if (!string.IsNullOrEmpty(editInsertion))
        {
            sections.Add(
                "=== SCENE TO WEAVE IN ===\n" +
                "The following passage should be woven into the narrative at the most natural point. " +
                "Match the surrounding voice and register. Do not drop it in verbatim — integrate it " +
                "so it reads as part of the original draft.\n\n" +
                editInsertion);
        }

        sections.Add($"=== TEXT TO EDIT ===\n{narrative}");

        sections.Add(
            "=== TASK ===\nCopy-edit this era narrative. The voice is correct — clean it, don't " +
            "change it.");

        return string.Join("\n\n", sections);
    }
}
