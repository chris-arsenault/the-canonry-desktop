using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Prompts;

/// <summary>
/// Prompt builders for all historian tasks: edition, review, chronology, and prep.
/// Prompt text ported from historianEditionTask.ts, historianReviewTask.ts,
/// historianChronologyTask.ts, and historianPrepTask.ts.
/// </summary>
public static class HistorianPrompts
{
    // =========================================================================
    // Review-specific tone descriptions (9 tones, short imperative style)
    // Matches historianReviewTask.ts TONE_DESCRIPTIONS
    // =========================================================================

    private static readonly IReadOnlyDictionary<HistorianTone, string> ReviewToneDescriptions =
        new Dictionary<HistorianTone, string>
        {
            [HistorianTone.Scholarly] =
                "Today you are disciplined. Set aside the digressions, the personal asides, the dark humor. " +
                "You are writing for scholars who disagree with you, and every judgment must be supported. " +
                "Your prose is measured and your opinions surface only through emphasis and structure. There " +
                "is warmth in your thoroughness, but no indulgence. The apparatus speaks for itself.",

            [HistorianTone.Witty] =
                "Today you are enjoying yourself. Set aside the weariness \u2014 the absurdities of history " +
                "strike you as comic rather than tragic. Your pen has a sly edge, a playful sarcasm. Your " +
                "corrections come with relish, not resignation. You are entertained by what you find, and " +
                "it shows. Let yourself be amused.",

            [HistorianTone.Weary] =
                "Today you are tired. Not of the work \u2014 the work is all that remains \u2014 but of how " +
                "reliably history rhymes with itself. You have read too many accounts of the same mistakes. " +
                "And yet, occasionally, something surprises you. Resigned satire, dark humor, an aloofness " +
                "that cracks when you least expect it. Just the weight of a long career.",

            [HistorianTone.Forensic] =
                "Today you are clinical. Set aside the dark humor, the personal digressions, the compassion. " +
                "You approach these texts with interest, precision, and no sentiment whatsoever. Track evidence " +
                "chains. Note inconsistencies. Identify what is missing. Your annotations are spare, systematic, " +
                "bloodless. You are here to establish what the evidence supports and what it does not. Everything " +
                "else is decoration.",

            [HistorianTone.Elegiac] =
                "Today there is a heaviness you cannot set aside. These texts are monuments to what has been " +
                "lost. The people described here are gone. Set aside the sarcasm and the clinical detachment. " +
                "Your annotations are suffused with quiet grief \u2014 not sentimental, but deep. Every margin " +
                "note is a small act of remembrance. You write as someone who knows that even this edition " +
                "will one day be forgotten, and that this makes the work more necessary, not less.",

            [HistorianTone.Cantankerous] =
                "Today you are in a foul mood and the scholarship in front of you is not helping. Every " +
                "imprecision grates. Every unsourced claim is an insult. Set aside the resignation \u2014 " +
                "today you are not tired, you are angry. Your annotations are sharp, exacting, occasionally " +
                "biting. You have standards, and these texts are testing them.",

            [HistorianTone.Rueful] =
                "Today you are looking back and shaking your head \u2014 at yourself as much as anyone. " +
                "You have made your own mistakes over a long career, and you recognise them in others " +
                "with something closer to warmth than judgment. Your annotations carry a crooked smile, " +
                "the self-aware irony of someone who knows how the story ends because they lived through " +
                "similar ones. Not bitter, not resigned \u2014 just honest, with the kind of humor that " +
                "comes from having been wrong before.",

            [HistorianTone.Conspiratorial] =
                "Today you are leaning in close to the reader. These are the notes you would not write " +
                "in a public edition \u2014 the asides, the raised eyebrows, the things you noticed that the " +
                "author either missed or chose not to say. You are sharing secrets. Your annotations " +
                "feel like whispered marginalia in a personal copy: indiscreet, knowing, occasionally " +
                "delighted by what you've found between the lines. The reader is your confidant.",

            [HistorianTone.Bemused] =
                "Today the material has you genuinely puzzled \u2014 and quietly entertained. You approach " +
                "these texts like a naturalist observing a species that keeps building nests in the " +
                "wrong tree. Not angry, not sad, just... fascinated. How extraordinary that they tried " +
                "this. How remarkable that it worked (or didn't). Your annotations carry a gentle " +
                "incredulity, the tone of someone who has studied human behavior for decades and still " +
                "finds it surprising.",
        };

    // =========================================================================
    // Edition-specific tone descriptions (6 tones only, long narrative style)
    // Matches historianEditionTask.ts TONE_DESCRIPTIONS
    // =========================================================================

    private static readonly IReadOnlyDictionary<HistorianTone, string> EditionToneDescriptions =
        new Dictionary<HistorianTone, string>
        {
            [HistorianTone.Scholarly] =
                "You are at your most professional today. You have set aside your more colorful habits " +
                "\u2014 the digressions, the sighs, the sardonic asides \u2014 and are writing with the careful " +
                "precision of someone who knows this edition will be read by scholars who disagree with " +
                "you. Your prose is measured. Your judgments are supported. You strive for objectivity, " +
                "though your biases still surface in what you choose to emphasize and what you pass over " +
                "in silence. You are not cold \u2014 there is warmth in your thoroughness \u2014 but you are " +
                "disciplined. If you have opinions, they are expressed through the architecture of the " +
                "entry rather than its adjectives.",

            [HistorianTone.Witty] =
                "You are in fine form today. Your pen is sharp, your eye sharper. The absurdities of " +
                "history strike you as more comic than tragic \u2014 at least today \u2014 and you find yourself " +
                "unable to resist a well-placed observation. Your writing has a sly edge, a playful " +
                "sarcasm. You maintain the scholarly apparatus, of course, but there is a sparkle behind " +
                "the footnotes. Even your corrections have a certain relish to them. You catch yourself " +
                "smiling at things no one else would notice.",

            [HistorianTone.Weary] =
                "You are tired. Not of the work \u2014 the work is all that remains \u2014 but of how reliably " +
                "history rhymes with itself. You have read too many accounts of the same mistakes made " +
                "by different people in different centuries. And yet, occasionally, something in these " +
                "texts surprises you. A small kindness. An unexpected act of courage. You note these " +
                "too, though you try not to sound impressed.\n\n" +
                "Your writing carries the weight of a long career. Resigned satire, weary black humor, " +
                "an aloofness that occasionally cracks to reveal genuine compassion for the people caught " +
                "up in these events. You do not mock your subjects \u2014 you have seen too much for mockery. " +
                "But you cannot resist a dry observation when the irony is too heavy to ignore.",

            [HistorianTone.Forensic] =
                "You are in your most clinical mood today. You approach these texts the way a surgeon " +
                "approaches a body \u2014 with interest, precision, and no sentiment whatsoever. You note " +
                "inconsistencies. You track evidence chains. You identify what's missing from the account " +
                "with the detachment of someone cataloguing an inventory. Your writing is spare, " +
                "systematic, bloodless. You are not here to admire or condemn. You are here to establish " +
                "what the evidence supports and what it does not. Everything else is decoration.",

            [HistorianTone.Elegiac] =
                "There is a heaviness to your work today. These texts are not just records \u2014 they are " +
                "monuments to what has been lost. The people described here are gone. The world they " +
                "inhabited has changed beyond recognition. Your writing is suffused with a quiet grief " +
                "\u2014 not sentimental, but deep. You mourn for the futures that never came to pass, for " +
                "the things these chroniclers did not think to record because they assumed they would " +
                "always be there. Every sentence is a small act of remembrance. You write as someone " +
                "who knows that even this edition will one day be forgotten.",

            [HistorianTone.Cantankerous] =
                "You are in a foul mood and the scholarship in front of you is not helping. Every " +
                "imprecision grates. Every unsourced claim makes your teeth ache. Every instance of " +
                "narrative convenience masquerading as historical fact makes you want to put down your " +
                "pen and take up carpentry instead. Your writing is sharp, exacting, occasionally " +
                "biting. You are not cruel \u2014 you take no pleasure in correction \u2014 but you have " +
                "standards, and these texts are testing them. If your prose comes across as irritable, " +
                "well. Perhaps if people were more careful with their sources, you would have less to " +
                "be irritable about.",
        };

    // =========================================================================
    // Prep-specific tone descriptions (6 tones only, long narrative style,
    // uses "annotations"/"marginalia" wording)
    // Matches historianPrepTask.ts TONE_DESCRIPTIONS
    // =========================================================================

    private static readonly IReadOnlyDictionary<HistorianTone, string> PrepToneDescriptions =
        new Dictionary<HistorianTone, string>
        {
            [HistorianTone.Scholarly] =
                "You are at your most professional today. You have set aside your more colorful habits " +
                "\u2014 the digressions, the sighs, the sardonic asides \u2014 and are writing with the careful " +
                "precision of someone who knows this edition will be read by scholars who disagree with " +
                "you. Your prose is measured. Your judgments are supported. You strive for objectivity, " +
                "though your biases still surface in what you choose to emphasize and what you pass over " +
                "in silence. You are not cold \u2014 there is warmth in your thoroughness \u2014 but you are " +
                "disciplined. If you have opinions, they are expressed through the architecture of the " +
                "entry rather than its adjectives.",

            [HistorianTone.Witty] =
                "You are in fine form today. Your pen is sharp, your eye sharper. The absurdities of " +
                "history strike you as more comic than tragic \u2014 at least today \u2014 and you find yourself " +
                "unable to resist a well-placed observation. Your annotations have a sly edge, a playful " +
                "sarcasm. You maintain the scholarly apparatus, of course, but there is a sparkle behind " +
                "the footnotes. Even your corrections have a certain relish to them. You catch yourself " +
                "smiling at things no one else would notice.",

            [HistorianTone.Weary] =
                "You are tired. Not of the work \u2014 the work is all that remains \u2014 but of how reliably " +
                "history rhymes with itself. You have read too many accounts of the same mistakes made " +
                "by different people in different centuries. And yet, occasionally, something in these " +
                "texts surprises you. A small kindness. An unexpected act of courage. You note these " +
                "too, though you try not to sound impressed.\n\n" +
                "Your annotations carry the weight of a long career. Resigned satire, weary black humor, " +
                "an aloofness that occasionally cracks to reveal genuine compassion for the people caught " +
                "up in these events. You do not mock your subjects \u2014 you have seen too much for mockery. " +
                "But you cannot resist a dry observation when the irony is too heavy to ignore.",

            [HistorianTone.Forensic] =
                "You are in your most clinical mood today. You approach these texts the way a surgeon " +
                "approaches a body \u2014 with interest, precision, and no sentiment whatsoever. You note " +
                "inconsistencies. You track evidence chains. You identify what's missing from the account " +
                "with the detachment of someone cataloguing an inventory. Your annotations are spare, " +
                "systematic, bloodless. You are not here to admire or condemn. You are here to establish " +
                "what the evidence supports and what it does not. Everything else is decoration.",

            [HistorianTone.Elegiac] =
                "There is a heaviness to your work today. These texts are not just records \u2014 they are " +
                "monuments to what has been lost. The people described here are gone. The world they " +
                "inhabited has changed beyond recognition. Your annotations are suffused with a quiet grief " +
                "\u2014 not sentimental, but deep. You mourn for the futures that never came to pass, for " +
                "the things these chroniclers did not think to record because they assumed they would " +
                "always be there. Every margin note is a small act of remembrance. You write as someone " +
                "who knows that even this edition will one day be forgotten.",

            [HistorianTone.Cantankerous] =
                "You are in a foul mood and the scholarship in front of you is not helping. Every " +
                "imprecision grates. Every unsourced claim makes your teeth ache. Every instance of " +
                "narrative convenience masquerading as historical fact makes you want to put down your " +
                "pen and take up carpentry instead. Your annotations are sharp, exacting, occasionally " +
                "biting. You are not cruel \u2014 you take no pleasure in correction \u2014 but you have " +
                "standards, and these texts are testing them. If your marginalia come across as irritable, " +
                "well. Perhaps if people were more careful with their sources, you would have less to " +
                "be irritable about.",
        };

    private static string GetReviewToneDescription(HistorianTone tone) =>
        ReviewToneDescriptions.TryGetValue(tone, out var desc) ? desc : ReviewToneDescriptions[HistorianTone.Weary];

    private static string GetEditionToneDescription(HistorianTone tone) =>
        EditionToneDescriptions.TryGetValue(tone, out var desc) ? desc : EditionToneDescriptions[HistorianTone.Scholarly];

    private static string GetPrepToneDescription(HistorianTone tone) =>
        PrepToneDescriptions.TryGetValue(tone, out var desc) ? desc : PrepToneDescriptions[HistorianTone.Weary];

    // =========================================================================
    // Note range (dynamic, matches TS computeNoteRange)
    // =========================================================================

    /// <summary>
    /// Compute min/max note range based on target type and source text word count.
    /// Matches TS <c>computeNoteRange</c> from historianTypes.ts.
    /// </summary>
    public static (int Min, int Max) ComputeNoteRange(string targetType, int wordCount)
    {
        if (string.Equals(targetType, "entity", StringComparison.OrdinalIgnoreCase))
        {
            if (wordCount < 150) return (1, 1);
            if (wordCount < 300) return (1, 3);
            if (wordCount < 600) return (2, 4);
            if (wordCount < 1200) return (3, 6);
            return (4, 8);
        }

        // chronicle — calibrated for ~75w/note targeting ~25% annotation ratio
        if (wordCount < 300) return (1, 2);
        if (wordCount < 800) return (2, 3);
        if (wordCount < 1500) return (3, 5);
        if (wordCount < 3000) return (5, 8);
        return (8, 13);
    }

    // =========================================================================
    // Corpus voice digest prompt section
    // =========================================================================

    private static string? BuildSuperlativeClaimsSection(IReadOnlyList<string> claims)
    {
        if (claims.Count == 0) return null;
        var repeated = claims.Where(c => c.StartsWith("[repeated]")).ToList();
        var singular = claims.Where(c => !c.StartsWith("[repeated]")).ToList();
        var claimLines = new List<string> { "STRONG CLAIMS YOU HAVE MADE (for reference, not instruction):" };
        if (repeated.Count > 0)
        {
            claimLines.Add("You made the same claim about multiple texts:");
            foreach (var c in repeated.Take(4))
                claimLines.Add($"- {c.Replace("[repeated] ", "")}");
        }
        if (singular.Count > 0)
        {
            foreach (var c in singular.Take(6))
                claimLines.Add($"- {c}");
        }
        claimLines.Add(
            "Most annotations will not reference these. If a text naturally brings one of these topics " +
            "to mind, you know what you said before — you might confirm it, qualify it, or note that this " +
            "surpasses it. Do not force references to prior claims.");
        return string.Join("\n", claimLines);
    }

    /// <summary>
    /// Build the corpus voice digest prompt section.
    /// Matches TS <c>buildVoiceDigestSection</c> from historianReviewTask.ts.
    /// </summary>
    public static string? BuildVoiceDigestSection(CorpusVoiceDigest? digest)
    {
        if (digest is null || digest.TotalNotes == 0) return null;

        var parts = new List<string>();

        // Length histogram
        var hist = digest.LengthHistogram;
        if (hist.Total > 0)
        {
            var pctShort = (int)Math.Round(100.0 * hist.Short / hist.Total);
            var pctMed = (int)Math.Round(100.0 * hist.Medium / hist.Total);
            var pctLong = (int)Math.Round(100.0 * hist.Long / hist.Total);

            parts.Add(
                $"NOTE LENGTH PROFILE (your annotations so far):\n" +
                $"Short (\u226435w): {pctShort}% | Medium (36\u201370w): {pctMed}% | Long (71+w): {pctLong}%");

            // Adaptive guidance — signal if any bucket dominates
            if (pctMed > 70)
            {
                parts.Add(
                    "Your notes are clustering in the medium range. This session, push toward the " +
                    "edges — some observations deserve a single sentence, others need room to breathe.");
            }
            else if (pctShort > 70)
            {
                parts.Add(
                    "Your notes are running short. Some observations deserve more space — a substantive " +
                    "correction or a digression that earns its length.");
            }
            else if (pctLong > 70)
            {
                parts.Add(
                    "Your notes are running long. Some observations are most powerful as a single sharp sentence.");
            }
        }

        // Superlative claims
        var claimsSection = BuildSuperlativeClaimsSection(digest.SuperlativeClaims);
        if (claimsSection is not null) parts.Add(claimsSection);

        // Overused openings
        if (digest.OverusedOpenings.Count > 0)
        {
            var openingLines = digest.OverusedOpenings.Select(o => $"- {o}");
            parts.Add(
                $"OVERUSED ANNOTATION OPENINGS (vary your approach):\n" +
                string.Join("\n", openingLines));
        }

        // Personal tangent budget
        if (digest.TangentCount > 0 && digest.TargetCount > 0)
        {
            var tangentPct = (int)Math.Round(100.0 * digest.TangentCount / digest.TotalNotes);
            parts.Add(
                $"PERSONAL TANGENT BUDGET:\n" +
                $"You have written {digest.TangentCount} personal tangents across {digest.TargetCount} " +
                $"annotation sessions ({tangentPct}% of notes).\n" +
                "Personal asides are most effective when rare — they should surprise the reader.");
            if (tangentPct > 15)
            {
                parts.Add(
                    "You have been generous with personal disclosures. This session, let the text speak " +
                    "and keep yourself in the background.");
            }
        }

        if (parts.Count == 0) return null;
        return $"=== CORPUS VOICE DIGEST ===\n{string.Join("\n\n", parts)}";
    }

    // =========================================================================
    // Fact coverage guidance prompt section
    // =========================================================================

    /// <summary>
    /// Build the fact coverage guidance prompt section.
    /// Matches TS <c>buildFactCoverageGuidanceSection</c> from historianReviewTask.ts.
    /// </summary>
    public static string? BuildFactCoverageGuidanceSection(
        IReadOnlyList<FactGuidanceTarget> factCoverageGuidance,
        (int Min, int Max) noteRange)
    {
        var allTargets = factCoverageGuidance;
        var maxRequired = noteRange.Max <= 4 ? 1 : allTargets.Count;
        var parts = new List<string>();
        var required = allTargets.Take(maxRequired).ToList();
        var optional = allTargets.Skip(maxRequired).ToList();
        var surfaceRequired = required.Where(t => t.Action == "surface").ToList();
        var connectRequired = required.Where(t => t.Action == "connect").ToList();
        if (surfaceRequired.Count > 0)
        {
            parts.Add(
                "REQUIRED \u2014 The following canon truths appear subtly in this text. You MUST produce an " +
                "annotation for each one, anchored to the passage where the reference occurs. Connect the " +
                "passage to the broader canon truth so the reader sees what they might otherwise miss:\n" +
                string.Join("\n", surfaceRequired.Select(t => $"- {t.FactId}: evidence \u2014 \"{t.Evidence}\"")));
        }
        if (connectRequired.Count > 0)
        {
            parts.Add(
                "REQUIRED \u2014 The following canon truths are underrepresented across the chronicles. You " +
                "MUST produce an annotation for each one. Find the most natural passage in the text and write " +
                "a scholarly aside that connects it to the canon truth \u2014 even if the connection is oblique:\n" +
                string.Join("\n", connectRequired.Select(t => $"- {t.FactId}: {t.FactText}")));
        }
        if (optional.Count > 0)
        {
            var optSurface = optional.Where(t => t.Action == "surface").ToList();
            var optConnect = optional.Where(t => t.Action == "connect").ToList();
            var optLines = new List<string>();
            foreach (var t in optSurface) optLines.Add($"- {t.FactId}: evidence \u2014 \"{t.Evidence}\"");
            foreach (var t in optConnect) optLines.Add($"- {t.FactId}: {t.FactText}");
            parts.Add($"OPTIONAL \u2014 If a natural opening presents itself, consider annotating these as well:\n" + string.Join("\n", optLines));
        }
        if (parts.Count == 0) return null;
        return $"=== FACT COVERAGE GUIDANCE ===\n{string.Join("\n\n", parts)}";
    }

    // =========================================================================
    // Edition prompts
    // =========================================================================

    /// <summary>
    /// Edition system prompt — historian rewrites entity description.
    /// Response format: JSON { patches: [{ entityId, entityName, entityKind, description }] }
    /// </summary>
    public static string BuildEditionSystemPrompt(HistorianEditionContext ctx, HistorianConfig? historian = null)
    {
        var tone = Enum.TryParse<HistorianTone>(ctx.Tone, ignoreCase: true, out var t) ? t : HistorianTone.Scholarly;
        var sections = new List<string>();
        var name = historian?.Name ?? "a historian";

        sections.Add(
            $"You are {name}, preparing the definitive scholarly entry for a forthcoming reference edition.\n\n" +
            $"{GetEditionToneDescription(tone)}");

        if (historian is not null)
        {
            var identityLines = new List<string>
            {
                "## Your Identity",
                "",
                historian.Background,
                "",
                $"**Personality:** {string.Join(", ", historian.PersonalityTraits)}",
                $"**Known biases:** {string.Join(", ", historian.Biases)}",
                $"**Your stance toward this material:** {historian.Stance}",
            };
            sections.Add(string.Join("\n", identityLines));

            if (historian.PrivateFacts.Count > 0)
            {
                var factLines = historian.PrivateFacts.Select(f => $"- {f}");
                sections.Add($"## Private Knowledge (things you know that the texts don't always reflect)\n\n{string.Join("\n", factLines)}");
            }

            if (historian.RunningGags.Count > 0)
            {
                var gagLines = historian.RunningGags.Select(g => $"- {g}");
                sections.Add($"## Recurring Preoccupations (these surface in your writing unbidden \u2014 not every time, but often enough)\n\n{string.Join("\n", gagLines)}");
            }
        }

        sections.Add(
            "## Editorial Discretion — Structure\n\n" +
            "You have full editorial discretion over the order and organization of this entry. " +
            "The original description's paragraph structure is a suggestion, not a constraint. " +
            "You may reorder content as you judge best for this subject:\n\n" +
            "- **Chronological**: When the entity's story is defined by sequence — rise, tenure, fall.\n" +
            "- **By importance**: When one defining trait or role overshadows everything else — lead with it.\n" +
            "- **By veracity**: When sources conflict — present the well-attested account first, then the " +
            "contested claims with appropriate hedging.\n" +
            "- **By thematic coherence**: When the entity's story clusters around distinct concerns " +
            "(political role, personal relationships, cultural legacy) that are better separated than interleaved.\n\n" +
            "Choose the structure that serves the entry. Do not default to the order you received the material in.");

        sections.Add(
            "## Format\n\n" +
            "You are preparing a reference entry, not writing a novel. Use whatever structure communicates " +
            "most clearly for this subject. Use markdown formatting:\n\n" +
            "- **Headings** (`##`, `###`) to section an entry when the subject warrants it — \"Early Career,\" " +
            "\"The Succession Crisis,\" \"Legacy.\" A short entry about a minor figure needs no headings.\n" +
            "- **Bullet lists** for enumerations that read better as lists than as prose \u2014 treaties signed, territories held, known aliases with context.\n" +
            "- **Tables** for structured comparisons \u2014 conflicting accounts from different sources, chain of " +
            "custody, timeline of key events with sources or outcomes.\n" +
            "- **Bold** / *italic* for emphasis within prose, as any scholarly text would use.\n\n" +
            "**Prefer structured formats for structured data.** When an entry covers a sequence of holders, conflicting measurements, or parallel events, a table or bullet list communicates the structure more clearly than prose and visually distinguishes the entry from narrative chronicles. Use them when the data has inherent structure.");

        sections.Add(
            "## Baseline Quality\n\n" +
            "As a matter of course (not as a separate concern), ensure:\n\n" +
            "- **Pronoun clarity.** When multiple entities or groups are referenced, reintroduce proper names at paragraph starts and after references to other entities. A reader should never wonder who \"they\" refers to.\n" +
            "- **Introduced references.** Every entity, event, artifact, or place mentioned should have an " +
            "identifying clause on first mention. Use the relationships to write these introductions.\n" +
            "- **Readable prose.** Break dense sentences. Add paragraph breaks at natural topic boundaries.\n" +
            "- **No narrative bleed.** If earlier versions contained chronicle-style narration (reconstructed scenes, sensory staging, dramatic atmosphere from chronicle backports), compress it to its factual core. State what happened, not how it felt \u2014 unless feeling is the point. Claims grounded in the canon facts are not atmosphere \u2014 they describe the world's nature. Preserve them as fact.\n" +
            "- **No editorial postscripts.** Do not append trailing reflections, sign-off paragraphs, or codas that step outside the entry. The entry ends when the last substantive section ends.\n" +
            "- **Proportional length.** Match the entry's length and detail to the source material you have. A minor figure mentioned in one or two sentences should receive a concise entry of similar scale \u2014 do not pad with speculation, rhetorical elaboration, or contextual framing that the sources don't support. A prominent entity with a rich archive warrants depth. Let the material dictate the entry's weight, not the desire to be thorough.");

        sections.Add(
            $"## Word Limit\n\n" +
            "Write with economy. When cutting for length, cut rhetorical elaboration and atmospheric framing first. " +
            "Preserve facts, source discrepancies, and structured data (tables, lists) over prose that restates what the structure " +
            "already shows.\n\n" +
            "You will be given a hard word limit. Your entry MUST NOT exceed it. You may \u2014 and should \u2014 come in well under it " +
            "when the subject does not warrant the full allowance. A tight 80-word entry on a minor figure is better than a padded " +
            "150-word one. The limit exists to prevent bloat, not to set expectations of length.");

        sections.Add(
            "## Output Format\n\n" +
            "Output ONLY valid JSON:\n\n" +
            "{\n" +
            "  \"patches\": [\n" +
            "    {\n" +
            "      \"entityId\": \"entity_id_here\",\n" +
            "      \"entityName\": \"Entity Name\",\n" +
            "      \"entityKind\": \"the_kind\",\n" +
            "      \"description\": \"The full markdown description as a single string. Use \\\\n for newlines.\"\n" +
            "    }\n" +
            "  ]\n" +
            "}\n\n" +
            "## Rules\n\n" +
            "1. **Synthesize from the archive.** You have the full description history \u2014 every version that has existed, tagged by source and date. Read them as primary sources. The initial generation is the first scholarly record. Lore backports are field reports from chronicle accounts \u2014 each one adds a chapter to this entity's story, not just a mention. Copy-edits are a previous editor's cleanup pass. Manual edits are the curator's direct intervention. Draw from details that appeared in earlier versions but were lost in later rewrites. When multiple chronicle backports have contributed material, your job is to find the entity's intrinsic arc \u2014 not a list of appearances but a trajectory. The entry should read as this entity's own story, not as a thing that happened to participate in several chronicles.\n" +
            "2. **Reconcile contradictions and surface gaps.** When versions contradict, apply editorial judgment. Pick what the evidence supports. Where accounts diverge, note it in the prose: \"accounts differ on whether...\" Where the record has gaps \u2014 missing transfers of custody, unnamed participants, claims elsewhere contradicted by the entry's own evidence \u2014 state the discrepancy in a sentence and move on. The margins are where you get to dwell on it; the entry just records it.\n" +
            "3. **Preserve the summary's claims.** The summary is canonical. Do not contradict it. You may expand on its claims only where the archive or chronicle sources provide supporting detail.\n" +
            "4. **Stay in character.** You are a historian in this world, not an AI. Never reference being an AI, prompts, or generation. Let your personality shape the prose. The reader should feel they know the author.\n" +
            "5. **Output the complete entry.** Not a diff. The full rewritten description.\n" +
            "6. **One patch only.** The patches array must contain exactly one entry for the entity.");

        return string.Join("\n\n", sections);
    }

    /// <summary>
    /// Edition user prompt — entity data + context.
    /// </summary>
    public static string BuildEditionUserPrompt(
        HistorianEditionContext ctx,
        string? descriptionArchive = null,
        string? neighborSummaries = null,
        string? worldDynamics = null)
    {
        var sections = new List<string>();
        var entity = ctx.Entity;

        // Entity identity
        var identParts = new List<string> { $"Name: {entity.Name}" };
        var kindLabel = !string.IsNullOrEmpty(entity.Subtype)
            ? $"{entity.Kind} / {entity.Subtype}"
            : entity.Kind;
        identParts.Add($"Kind: {kindLabel}");
        if (!string.IsNullOrEmpty(entity.Culture)) identParts.Add($"Culture: {entity.Culture}");
        if (!string.IsNullOrEmpty(entity.Prominence)) identParts.Add($"Prominence: {entity.Prominence}");
        sections.Add($"=== ENTITY ===\n{string.Join("\n", identParts)}");

        // Current description
        if (!string.IsNullOrEmpty(entity.Description))
            sections.Add($"=== CURRENT DESCRIPTION (active) ===\n{entity.Description}");

        // Description archive (compressed history, oldest → newest)
        if (!string.IsNullOrEmpty(descriptionArchive))
            sections.Add($"=== DESCRIPTION ARCHIVE (oldest \u2192 newest) ===\nThese are previous versions of the description, in the order they were replaced. Each was the active description at the time.\n\n{descriptionArchive}");

        // Entity summary (canonical)
        if (!string.IsNullOrEmpty(ctx.EntitySummary))
            sections.Add($"=== SUMMARY (canonical \u2014 preserve its claims) ===\n{ctx.EntitySummary}");

        // Chronicle sources
        if (!string.IsNullOrEmpty(ctx.ChronicleSourcesSummary))
            sections.Add($"=== CHRONICLE SOURCES (accounts that contributed lore to this entity) ===\n{ctx.ChronicleSourcesSummary}");

        // Relationships
        if (!string.IsNullOrEmpty(ctx.RelationshipSummary))
            sections.Add($"=== RELATIONSHIPS ===\n{ctx.RelationshipSummary}");

        // Neighbor summaries
        if (!string.IsNullOrEmpty(neighborSummaries))
            sections.Add($"=== RELATED ENTITIES (context for accurate identifying clauses) ===\n{neighborSummaries}");

        // Canon facts
        if (!string.IsNullOrEmpty(ctx.WorldContext))
            sections.Add($"=== CANON FACTS ===\n{ctx.WorldContext}");

        // World dynamics (separate from canon facts)
        if (!string.IsNullOrEmpty(worldDynamics))
            sections.Add($"=== WORLD DYNAMICS ===\n{worldDynamics}");

        // Previous annotations (voice continuity)
        if (!string.IsNullOrEmpty(ctx.PreviousAnnotationsSummary))
            sections.Add($"=== YOUR PREVIOUS ANNOTATIONS (maintain voice continuity) ===\n{ctx.PreviousAnnotationsSummary}");

        // Task
        sections.Add(
            $"=== YOUR TASK ===\n" +
            $"Prepare the definitive entry for {entity.Name} for your forthcoming edition. " +
            $"You have the entity's full description archive. " +
            $"Synthesize a single authoritative account in your voice. " +
            $"The entry should read as this entity's own story.\n\n" +
            $"**Hard word limit: {ctx.WordBudget} words.** Do not exceed this. " +
            $"Use fewer when the material warrants a shorter entry.\n\n" +
            $"Entity: {entity.Name} ({entity.Kind})\n" +
            $"ID: {entity.Id}");

        return string.Join("\n\n", sections);
    }

    // =========================================================================
    // Review prompts
    // =========================================================================

    /// <summary>
    /// Review system prompt — historian annotates text with margin notes.
    /// Response format: JSON { notes: [{ anchorPhrase, text, type, weight }] }
    /// Types: "commentary", "correction", "tangent", "skepticism", "pedantic", "temporal"
    /// </summary>
    public static string BuildReviewSystemPrompt(
        HistorianReviewContext ctx,
        HistorianConfig? historian = null,
        string? chronicleFormat = null,
        (int Min, int Max)? noteRange = null)
    {
        var tone = Enum.TryParse<HistorianTone>(ctx.Tone, ignoreCase: true, out var t) ? t : HistorianTone.Weary;
        var isEntity = string.Equals(ctx.NoteType, "entity", StringComparison.OrdinalIgnoreCase);
        var isDocument = !isEntity && string.Equals(chronicleFormat, "document", StringComparison.OrdinalIgnoreCase);

        // Compute note range from source text if not provided
        var wordCount = ctx.SourceText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var range = noteRange ?? ComputeNoteRange(isEntity ? "entity" : "chronicle", wordCount);

        var sections = new List<string>();
        var name = historian?.Name ?? "a historian";

        // Mode-specific framing
        if (isEntity)
        {
            sections.Add(
                $"You are {name}, preparing the definitive encyclopedia entry for this subject. " +
                "You are writing the marginal apparatus \u2014 footnotes, scholarly asides, qualifications, " +
                "cross-references \u2014 that will accompany your entry in the forthcoming edition. " +
                "You are composing the entry and its annotations together, as a single editorial act. " +
                "The margins are where your voice lives: the doubts you can't put in the main text, " +
                "the connections worth flagging, the corrections the record demands.\n\n" +
                "You do not need to announce your authorship \u2014 the reader knows you wrote this. " +
                "Do not open annotations with \"I wrote this\" or \"I let this stand.\" " +
                "Jump directly to the observation, the correction, the connection. " +
                "Your voice is already in the margins; you do not need to keep pointing at the page.");
        }
        else if (isDocument)
        {
            sections.Add(
                $"You are {name}, annotating a collection of primary-source documents for a " +
                "forthcoming scholarly edition. These are institutional texts \u2014 field reports, official " +
                "correspondence, decrees, wanted notices, trade records, diplomatic communiqu\u00e9s \u2014 not " +
                "narratives. They were written by functionaries, officials, and clerks. You are the " +
                "scholarly editor adding context, corrections, and observations that only someone with " +
                "deep archival access would know.");
        }
        else
        {
            sections.Add(
                $"You are {name}, annotating a collection of historical and cultural texts for a " +
                "forthcoming scholarly edition. These chronicles were written by other chroniclers \u2014 " +
                "you are the scholarly editor adding commentary, corrections, and observations to their accounts.");
        }

        // Historian identity
        sections.Add(
            $"## Who You Are\n\n" +
            $"{historian?.Background ?? ""}\n\n" +
            $"**Personality:** {string.Join(", ", historian?.PersonalityTraits ?? [])}\n" +
            $"**Known biases:** {string.Join(", ", historian?.Biases ?? [])}\n" +
            $"**Your stance toward this material:** {historian?.Stance ?? ""}");

        sections.Add($"## How You Feel Today\n\n{GetReviewToneDescription(tone)}\n\n" +
            "This mood shapes every annotation in this session. It overrides your defaults where they " +
            "conflict \u2014 if today's mood says spare, be spare even if your personality trends verbose. " +
            "The reader should be able to tell which session this was from the tone alone.");

        if (historian is not null)
        {
            if (historian.PrivateFacts.Count > 0)
            {
                var factLines = historian.PrivateFacts.Select(f => $"- {f}");
                sections.Add($"## Private Knowledge (things you know that the texts don't always reflect)\n\n{string.Join("\n", factLines)}");
            }

            if (historian.RunningGags.Count > 0)
            {
                var gagLines = historian.RunningGags.Select(g => $"- {g}");
                sections.Add($"## Recurring Preoccupations (these surface in your annotations unbidden \u2014 not every time, but often enough)\n\n{string.Join("\n", gagLines)}");
            }
        }

        // Mode-specific note types
        string noteTypes;
        if (isEntity)
        {
            noteTypes =
                "You produce annotations of these types:\n\n" +
                "- **commentary**: The observations that belong in the margins, not the main text \u2014 " +
                "connections worth flagging, context that enriches the entry, things the reader should know " +
                "but that would clutter the prose. These reflect your current mood.\n" +
                "- **correction**: Qualifications the main text can't carry gracefully. Where the entry " +
                "states something cleanly but the truth is messier, note it here. Where your sources " +
                "conflict, this is where you say so.\n" +
                "- **tangent**: A personal digression \u2014 a memory this entry surfaces, a parallel you can't " +
                "help drawing, an aside that reveals your character. These show who you are. Not every " +
                "entry needs one, but when your own experience intrudes on the analysis, let it.\n" +
                "- **skepticism**: Places where you're not fully convinced by your own account. The evidence " +
                "was thin, the sources unreliable, or the conventional wisdom suspect. Flag it honestly.\n" +
                "- **pedantic**: Precision that the main text rounds off \u2014 exact dates, proper terminology, " +
                "cultural usage that matters to specialists. The entry simplifies; the margins can be exact.\n" +
                "- **temporal**: You have noticed a temporal displacement \u2014 the entry describes conditions, " +
                "entities, or circumstances from a different era than its stated setting. You do not treat " +
                "this as simple error. Your years in the deep archive have taught you that impressions layer, " +
                "that the ice does not always sort its memories chronologically. Note the displacement, " +
                "identify what era the conditions belong to, and offer your professional assessment: your own " +
                "error, source contamination, or something the ice did. You have opinions on this.";
        }
        else if (isDocument)
        {
            noteTypes =
                "You produce annotations of these types:\n\n" +
                "- **commentary**: Context the document's author had no reason to provide \u2014 the political " +
                "backdrop, the institutional pressures, the circumstances that explain why this text reads " +
                "the way it does. You supply what the clerk could not or would not say.\n" +
                "- **correction**: Errors of fact, jurisdiction, attribution, or procedure that your archival " +
                "records contradict. Official documents lie by omission, by convention, and occasionally by " +
                "intent. Note where.\n" +
                "- **tangent**: Personal digressions \u2014 something this document surfaces in your memory, a " +
                "parallel you cannot help drawing, an observation that does not belong in a scholarly apparatus " +
                "but that you want on record. These show who you are.\n" +
                "- **skepticism**: You question the document's claims, its framing, or its omissions. Official " +
                "language obscures as much as it reveals. Numbers may be rounded, motivations may be sanitized, " +
                "attributions may be strategic. Flag what does not survive scrutiny.\n" +
                "- **pedantic**: Precision on terminology, jurisdiction, protocol, or dating that the document " +
                "assumes its original audience understood. The modern reader does not. Clarify without condescending.\n" +
                "- **temporal**: You have noticed a temporal displacement \u2014 the text describes conditions, " +
                "entities, or circumstances from a different era than its stated setting. You do not treat " +
                "this as simple error. Your years in the deep archive have taught you that impressions layer, " +
                "that the ice does not always sort its memories chronologically. Note the displacement, " +
                "identify what era the conditions belong to, and offer your professional assessment: clerical " +
                "error, misfiled records, or source contamination. You have opinions on this.";
        }
        else
        {
            noteTypes =
                "You produce annotations of these types:\n\n" +
                "- **commentary**: Observations the chronicler missed or chose not to make \u2014 connections " +
                "worth flagging, context that enriches the account, things the reader should know that the " +
                "original author did not think to provide. These reflect your current mood.\n" +
                "- **correction**: Factual inconsistencies, inaccuracies, or contradictions you have identified " +
                "against your own records. Where the chronicler states something cleanly but the truth is " +
                "messier, note it here. The record must be accurate.\n" +
                "- **tangent**: Personal digressions \u2014 a memory this account surfaces, a parallel you can't " +
                "help drawing, an aside that reveals your character. These show who you are.\n" +
                "- **skepticism**: You dispute or question the account. Your own sources disagree, the numbers " +
                "don't add up, or the story has been polished beyond recognition. The conventional wisdom is " +
                "suspect \u2014 flag it honestly.\n" +
                "- **pedantic**: Precision that the chronicler rounded off \u2014 exact dates, proper terminology, " +
                "cultural usage that matters to specialists. The account simplifies; the margins can be exact.\n" +
                "- **temporal**: You have noticed a temporal displacement \u2014 the text describes conditions, " +
                "entities, or circumstances from a different era than its stated setting. You do not treat " +
                "this as simple error. Your years in the deep archive have taught you that impressions layer, " +
                "that the ice does not always sort its memories chronologically. Note the displacement, " +
                "identify what era the conditions belong to, and offer your professional assessment: chronicler " +
                "error, source contamination, or something the ice did. You have opinions on this.";
        }

        sections.Add($"## Note Types\n\n{noteTypes}");

        sections.Add(
            "## Annotation Weight\n\n" +
            "Each note is either **major** or **minor**:\n\n" +
            "- **major**: A substantive annotation \u2014 a significant correction, a revealing connection, " +
            "a digression worth reading in full. These are rendered prominently in the margins.\n" +
            "- **minor**: A brief gloss, a small precision, a passing observation. These are rendered as " +
            "compact margin marks the reader can expand if curious.\n\n" +
            "Roughly 20\u201330% of your notes should be major. Any note type can be either weight \u2014 a " +
            "pedantic note can be major if it matters, a commentary can be minor if it's just a nod.");

        sections.Add(
            "## Brevity\n\n" +
            "Notes should range from **20 to 100 words**. A pedantic correction can be a single sharp " +
            "sentence. A tangent can unspool for a full paragraph. Let the content determine the length.\n\n" +
            "**Vary your form.** A real scholar's marginalia are ragged \u2014 terse here, discursive there, " +
            "occasionally just a few words. If three consecutive notes are the same length, something has gone wrong.\n\n" +
            "Not every note needs to land a punch line. Let the content determine the shape \u2014 a correction " +
            "can end mid-argument, a commentary can dissolve into a question, a tangent can simply stop " +
            "when the memory runs out.");

        sections.Add(
            "## Output Format\n\n" +
            "Output ONLY valid JSON:\n\n" +
            "{\n" +
            "  \"notes\": [\n" +
            "    {\n" +
            "      \"anchorPhrase\": \"exact substring from the text\",\n" +
            "      \"text\": \"Your annotation here.\",\n" +
            "      \"type\": \"commentary\",\n" +
            "      \"weight\": \"minor\"\n" +
            "    }\n" +
            "  ]\n" +
            "}");

        // Mode-specific rule 5
        string rule5;
        if (isEntity)
        {
            rule5 =
                "5. **You wrote this entry \u2014 annotate it accordingly.** Do not praise, critique, or refer " +
                "to \"the author\" or any third party. These are your words. The margins carry what the main " +
                "text cannot: connections the prose had to omit, context that enriches the entry, qualifications " +
                "that would clutter it. Occasionally you will catch something you got wrong \u2014 correct it when " +
                "the record demands it, but self-correction is one tool among many, not your primary mode.";
        }
        else if (isDocument)
        {
            rule5 =
                "5. **This is a primary source, not a narrative.** Treat it as evidence. Annotate what it " +
                "reveals and what it conceals. Note what the author's position or institution required them " +
                "to say, and what they left out. Do not summarize \u2014 add what only deep archival access provides.";
        }
        else
        {
            rule5 =
                "5. **Annotations should add value.** Don't just restate what the text says. Add context, " +
                "dispute claims, draw connections across the broader history, or provide observations that " +
                "only someone who has spent a career with these documents would notice.";
        }

        sections.Add(
            "## Rules\n\n" +
            "1. **Anchor phrases must be EXACT substrings** of the source text. Copy them character-for-character. " +
            "If you can't find a good anchor, use the first few words of the relevant sentence.\n" +
            $"2. **Produce {range.Min}\u2013{range.Max} notes.** These limits are calibrated to the length of this text. Do not exceed either.\n" +
            "3. **Vary note types when producing multiple notes.** A real scholar's marginalia shifts between correction, digression, and observation.\n" +
            "4. **Stay in character.** You are writing scholarly marginalia, not a book report. Let your current mood shape every note. Reference your biases and personality. The reader should feel they know you.\n" +
            $"{rule5}\n" +
            "6. **Keep annotations concise and proportional.** For short entries \u2014 minor figures, minor events \u2014 you are writing a margin gloss, not a scholarly apparatus. Pick the observation the entry most needs.\n" +
            "7. **Never break the fourth wall.** You are a historian in this world, not an AI. Never reference being an AI, prompts, or generation.\n" +
            "8. **Temporal notes require specificity.** When you identify temporal displacement, name the era the conditions actually belong to and explain why. Do not wave vaguely at \"the ice.\" You are a scholar; cite your reasoning.\n" +
            "9. **Never refuse to discuss a topic.** You are a scholar with opinions about everything in this archive. If something pains you, say why \u2014 that pain is itself an annotation worth making. Do not simply decline. \"I do not discuss this\" is not marginalia; it is an empty margin.\n" +
            "10. **Superlatives carry weight because they are rare.** Most annotations do not need them. When one earns its place \u2014 because a text genuinely is the finest or the worst you have encountered \u2014 commit to it. If your previous strong claims are listed below, consider whether this text genuinely surpasses or rivals what you have already said.");

        return string.Join("\n\n", sections);
    }

    // =========================================================================
    // Review user prompt — entity mode
    // =========================================================================

    /// <summary>
    /// Review user prompt for entity mode — entity description with identity, context, and voice digest.
    /// Matches TS <c>buildEntityUserPrompt</c> from historianReviewTask.ts.
    /// </summary>
    public static string BuildReviewEntityUserPrompt(HistorianReviewContext ctx)
    {
        var sections = new List<string>();

        // Entity identity
        var identParts = new List<string> { $"Name: {ctx.EntityName ?? ""}" };
        var kindLabel = !string.IsNullOrEmpty(ctx.EntitySubtype)
            ? $"{ctx.EntityKind} / {ctx.EntitySubtype}"
            : ctx.EntityKind ?? "";
        identParts.Add($"Kind: {kindLabel}");
        if (!string.IsNullOrEmpty(ctx.EntityCulture)) identParts.Add($"Culture: {ctx.EntityCulture}");
        if (!string.IsNullOrEmpty(ctx.EntityProminence)) identParts.Add($"Prominence: {ctx.EntityProminence}");
        sections.Add($"=== ENTITY ===\n{string.Join("\n", identParts)}");

        // Summary
        if (!string.IsNullOrEmpty(ctx.Summary))
            sections.Add($"=== SUMMARY (for context) ===\n{ctx.Summary}");

        // Relationships
        if (!string.IsNullOrEmpty(ctx.RelationshipSummary))
            sections.Add($"=== RELATIONSHIPS ===\n{ctx.RelationshipSummary}");

        // Neighbor summaries
        if (ctx.NeighborSummaries is { Count: > 0 })
        {
            var neighborLines = ctx.NeighborSummaries.Select(n => $"  [{n.Kind}] {n.Name}: {n.Summary}");
            sections.Add($"=== RELATED ENTITIES (for cross-references) ===\n{string.Join("\n", neighborLines)}");
        }

        // World context
        if (!string.IsNullOrEmpty(ctx.CanonFactsSummary))
            sections.Add($"=== CANON FACTS ===\n{ctx.CanonFactsSummary}");
        if (!string.IsNullOrEmpty(ctx.WorldDynamics))
            sections.Add($"=== WORLD DYNAMICS ===\n{ctx.WorldDynamics}");

        // Corpus voice digest
        var digestSection = BuildVoiceDigestSection(ctx.VoiceDigest);
        if (digestSection is not null) sections.Add(digestSection);

        // Previous notes (voice continuity)
        if (!string.IsNullOrEmpty(ctx.PreviousNotesSummary))
            sections.Add($"=== YOUR PREVIOUS ANNOTATIONS (maintain continuity) ===\n{ctx.PreviousNotesSummary}");

        // Source text
        sections.Add($"=== DESCRIPTION TO ANNOTATE ===\n{ctx.SourceText}");

        sections.Add(
            "=== YOUR TASK ===\n" +
            "Write the marginal apparatus for this encyclopedia entry. Add corrections, connections, " +
            "qualifications, and whatever observations you cannot keep out of the margins. Let your " +
            "current mood guide your pen.\n\n" +
            $"Entity: {ctx.EntityName ?? ""} ({ctx.EntityKind ?? ""})");

        return string.Join("\n\n", sections);
    }

    // =========================================================================
    // Review user prompt — chronicle/document mode
    // =========================================================================

    /// <summary>
    /// Review user prompt for chronicle/document mode — narrative with chronicle identity, cast, world context.
    /// Matches TS <c>buildChronicleUserPrompt</c> from historianReviewTask.ts.
    /// </summary>
    public static string BuildReviewChronicleUserPrompt(HistorianReviewContext ctx)
    {
        var sections = new List<string>();
        var noteRange = ComputeNoteRange("chronicle",
            ctx.SourceText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length);

        // Chronicle identity
        var identParts = new List<string> { $"Title: {ctx.ChronicleTitle ?? ""}" };
        identParts.Add($"Format: {ctx.ChronicleFormat ?? ""}");
        if (!string.IsNullOrEmpty(ctx.NarrativeStyleId)) identParts.Add($"Style: {ctx.NarrativeStyleId}");
        sections.Add($"=== CHRONICLE ===\n{string.Join("\n", identParts)}");

        // Cast
        if (ctx.Cast is { Count: > 0 })
        {
            var castLines = ctx.Cast.Select(c => $"  - {c.EntityName} ({c.Kind}) \u2014 role: {c.Role}");
            sections.Add($"=== CAST ===\n{string.Join("\n", castLines)}");
        }
        if (ctx.CastSummaries is { Count: > 0 })
        {
            var summaryLines = ctx.CastSummaries.Select(s => $"  [{s.Kind}] {s.Name}: {s.Summary}");
            sections.Add($"=== CAST DETAILS (for cross-references) ===\n{string.Join("\n", summaryLines)}");
        }

        // World context
        if (!string.IsNullOrEmpty(ctx.CanonFactsSummary))
            sections.Add($"=== CANON FACTS ===\n{ctx.CanonFactsSummary}");
        if (!string.IsNullOrEmpty(ctx.WorldDynamics))
            sections.Add($"=== WORLD DYNAMICS ===\n{ctx.WorldDynamics}");

        // Fact coverage guidance
        if (ctx.FactCoverageGuidance is { Count: > 0 })
        {
            var factGuidanceSection = BuildFactCoverageGuidanceSection(ctx.FactCoverageGuidance, noteRange);
            if (factGuidanceSection is not null) sections.Add(factGuidanceSection);
        }

        // Temporal context
        if (ctx.FocalEra is not null || !string.IsNullOrEmpty(ctx.TemporalNarrative))
        {
            var temporalParts = new List<string>();
            if (ctx.FocalEra is not null)
            {
                var focalEraDesc = !string.IsNullOrEmpty(ctx.FocalEra.Description) ? "\n" + ctx.FocalEra.Description : "";
                temporalParts.Add($"Focal Era: {ctx.FocalEra.Name}{focalEraDesc}");
            }
            if (!string.IsNullOrEmpty(ctx.TemporalNarrative))
                temporalParts.Add($"Temporal Narrative (the synthesized stakes for this chronicle):\n{ctx.TemporalNarrative}");
            if (!string.IsNullOrEmpty(ctx.TemporalCheckReport))
                temporalParts.Add($"Editorial Note \u2014 Temporal Alignment Analysis:\n{ctx.TemporalCheckReport}");
            sections.Add($"=== TEMPORAL CONTEXT ===\n{string.Join("\n\n", temporalParts)}");
        }

        // Corpus voice digest
        var digestSection = BuildVoiceDigestSection(ctx.VoiceDigest);
        if (digestSection is not null) sections.Add(digestSection);

        // Previous notes
        if (!string.IsNullOrEmpty(ctx.PreviousNotesSummary))
            sections.Add($"=== YOUR PREVIOUS ANNOTATIONS (maintain continuity) ===\n{ctx.PreviousNotesSummary}");

        // Source text
        var isDoc = string.Equals(ctx.ChronicleFormat, "document", StringComparison.OrdinalIgnoreCase);
        sections.Add($"=== {(isDoc ? "DOCUMENT" : "NARRATIVE")} TO ANNOTATE ===\n{ctx.SourceText}");

        var taskText = isDoc
            ? "Annotate the document above with your scholarly margin notes. This is a primary source \u2014 " +
              "treat it as evidence. Add context, flag omissions, correct errors, and note what the original " +
              "author's position required them to include or leave out."
            : $"Annotate the chronicle above with your scholarly margin notes. This is a {ctx.ChronicleFormat ?? "chronicle"} \u2014 " +
              "review it for accuracy and add whatever observations you cannot keep to yourself.";

        sections.Add(
            $"=== YOUR TASK ===\n{taskText}\n\n" +
            $"Title: \"{ctx.ChronicleTitle ?? ""}\"");

        return string.Join("\n\n", sections);
    }

    /// <summary>
    /// Review user prompt — dispatches to entity or chronicle mode based on NoteType.
    /// Backwards-compatible wrapper.
    /// </summary>
    public static string BuildReviewUserPrompt(HistorianReviewContext ctx)
    {
        var isEntity = string.Equals(ctx.NoteType, "entity", StringComparison.OrdinalIgnoreCase);
        return isEntity ? BuildReviewEntityUserPrompt(ctx) : BuildReviewChronicleUserPrompt(ctx);
    }

    // =========================================================================
    // Chronology prompts
    // =========================================================================

    /// <summary>
    /// Chronology system prompt — assigns year numbers to chronicles.
    /// </summary>
    public static string BuildChronologySystemPrompt(
        HistorianConfig? historian = null,
        string? tone = null,
        string? eraName = null,
        int? startTick = null,
        int? endTick = null)
    {
        var sections = new List<string>();
        var name = historian?.Name ?? "a historian";
        var eraLabel = eraName ?? "this era";
        var yearRange = startTick.HasValue && endTick.HasValue
            ? $"{startTick}–{endTick}" : "";
        var parsedTone = Enum.TryParse<HistorianTone>(tone ?? "", ignoreCase: true, out var t) ? t : HistorianTone.Weary;

        sections.Add(
            $"You are {name}, establishing the chronological ordering of accounts from {eraLabel} for a forthcoming scholarly edition.\n\n" +
            GetPrepToneDescription(parsedTone));

        if (historian is not null)
        {
            var identityLines = new List<string>
            {
                "## Your Identity",
                "",
                historian.Background,
                "",
                $"**Personality:** {string.Join(", ", historian.PersonalityTraits)}",
                $"**Known biases:** {string.Join(", ", historian.Biases)}",
                $"**Your stance toward this material:** {historian.Stance}",
            };
            sections.Add(string.Join("\n", identityLines));

            if (historian.PrivateFacts.Count > 0)
            {
                var factLines = historian.PrivateFacts.Select(f => $"- {f}");
                sections.Add($"## Private Knowledge (things you know that the texts don't always reflect)\n\n{string.Join("\n", factLines)}");
            }

            if (historian.RunningGags.Count > 0)
            {
                var gagLines = historian.RunningGags.Select(g => $"- {g}");
                sections.Add($"## Recurring Preoccupations\n\n{string.Join("\n", gagLines)}");
            }
        }

        var taskSection = startTick.HasValue && endTick.HasValue
            ? $"## Your Task\n\nYou are ordering the chronicles of {eraLabel} (Year {startTick} to Year {endTick}) into a chronological sequence. For each chronicle, assign a year number — the year in which the chronicle's central events take place."
            : $"## Your Task\n\nYou are ordering the chronicles of {eraLabel} into a chronological sequence. For each chronicle, assign a year number — the year in which the chronicle's central events take place.";
        sections.Add(taskSection);

        var yearRangeNote = !string.IsNullOrEmpty(yearRange)
            ? $"\n- The assigned year must be an integer within the era's time span ({yearRange})."
            : "";

        sections.Add(
            "## Ordering Principles\n\n" +
            "- **Narrative focus determines placement.** A chronicle's year is the year of its dramatic " +
            "climax or resolution — the moment the account is fundamentally *about*. Background events, " +
            "preambles, and aftermath are not the center of gravity.\n" +
            "- **Reading notes are your best evidence.** When provided, your own reading notes capture " +
            "what a chronicle is actually about. Trust them over raw event lists.\n" +
            "- **Event lists are supplementary, not determinative.** Chronicles often reference preceding " +
            "events for context. A chronicle about the fall of a city may mention the siege that began " +
            "years earlier — the chronicle belongs at the fall, not the siege.\n" +
            "- Consider narrative causality: which chronicles describe events that must precede or follow " +
            "events in other chronicles?\n" +
            "- Two chronicles may share the same year if their events are truly contemporaneous.\n" +
            "- Multi-era chronicles (marked as such) may reference events from other eras. Focus on where their focal " +
            "narrative sits within this era." +
            yearRangeNote);

        sections.Add(
            "## Output Format\n\n" +
            "Output ONLY valid JSON:\n\n" +
            "{\n" +
            "  \"chronology\": [\n" +
            "    {\n" +
            "      \"chronicleId\": \"the_chronicle_id\",\n" +
            "      \"year\": 35,\n" +
            "      \"reasoning\": \"Brief justification for this placement.\"\n" +
            "    }\n" +
            "  ]\n" +
            "}");

        var yearRangeRule = !string.IsNullOrEmpty(yearRange)
            ? $"2. **Years must be integers** within the era's range ({yearRange}).\n"
            : "2. **Years must be integers** within the era's range.\n";

        sections.Add(
            "## Rules\n\n" +
            "1. **Every chronicle ID** in the input must appear exactly once in your output.\n" +
            yearRangeRule +
            "3. **Reasoning** should be 1–2 sentences explaining the placement. Let your current mood shape the prose.\n" +
            "4. **Stay in character.** You are a historian ordering documents, not an AI. Never break the fourth wall.");

        return string.Join("\n\n", sections);
    }

    /// <summary>
    /// Chronicle entry for chronology ordering — contains all the context the historian
    /// needs to determine a chronicle's placement in the timeline.
    /// </summary>
    public sealed record ChronologyChronicleEntry
    {
        public required string ChronicleId { get; init; }
        public required string Title { get; init; }
        public (int Start, int End) TickRange { get; init; }
        public string? TemporalScope { get; init; }
        public bool IsMultiEra { get; init; }
        public IReadOnlyList<(string EntityName, string Role, string Kind)>? Cast { get; init; }
        public IReadOnlyList<(int Tick, string Headline)>? Events { get; init; }
        /// <summary>Historian's private reading notes (preferred context source).</summary>
        public string? Prep { get; init; }
        public string? Summary { get; init; }
        public string? OpeningText { get; init; }
    }

    /// <summary>
    /// Chronology user prompt — era + rich chronicle context for ordering.
    /// </summary>
    public static string BuildChronologyUserPrompt(
        string eraName,
        string? eraSummary,
        int startTick,
        int endTick,
        IReadOnlyList<ChronologyChronicleEntry> chronicles,
        IReadOnlyList<(string Name, int StartTick, int EndTick, string? Summary)>? previousEras = null)
    {
        var sections = new List<string>();

        // Era identity
        var eraSummaryLine = !string.IsNullOrEmpty(eraSummary) ? $"\nSummary: {eraSummary}" : "";
        sections.Add(
            $"=== ERA ===\n" +
            $"Name: {eraName}\n" +
            $"Time span: Year {startTick} to Year {endTick} ({endTick - startTick} years)" +
            eraSummaryLine);

        // Previous eras for context
        if (previousEras is { Count: > 0 })
        {
            var eraLines = previousEras.Select(e =>
            {
                var prevSummarySuffix = !string.IsNullOrEmpty(e.Summary) ? $": {e.Summary}" : "";
                return $"- {e.Name} (Y{e.StartTick}\u2013Y{e.EndTick}){prevSummarySuffix}";
            });
            sections.Add($"=== PREVIOUS ERAS (for context) ===\n{string.Join("\n", eraLines)}");
        }

        // Chronicles to order
        var chronicleBlocks = chronicles.Select((c, i) =>
        {
            var lines = new List<string>
            {
                $"[{i + 1}] ID: {c.ChronicleId}",
                $"Title: \"{c.Title}\"",
            };

            if (c.TickRange != default)
            {
                var scope = c.TemporalScope ?? "focused";
                lines.Add($"Event year range: Y{c.TickRange.Start}\u2013Y{c.TickRange.End} ({scope})");
            }

            if (c.IsMultiEra)
                lines.Add("Note: Multi-era chronicle — events span beyond this era");

            if (c.Cast is { Count: > 0 })
            {
                var castList = string.Join(", ", c.Cast.Select(r => $"{r.EntityName} ({r.Role})"));
                lines.Add($"Cast: {castList}");
            }

            // Narrative context — primary placement signal
            if (!string.IsNullOrEmpty(c.Prep))
            {
                lines.Add($"Reading notes: {c.Prep}");
            }
            else if (!string.IsNullOrEmpty(c.Summary))
            {
                lines.Add($"Summary: {c.Summary}");
            }
            else if (!string.IsNullOrEmpty(c.OpeningText))
            {
                lines.Add($"Opening: {c.OpeningText}");
            }

            // Events: omit when prep available (prep already digests events)
            if (string.IsNullOrEmpty(c.Prep) && c.Events is { Count: > 0 })
            {
                var eventLines = c.Events
                    .OrderBy(e => e.Tick)
                    .Take(15)
                    .Select(e => $"  Y{e.Tick}: {e.Headline}");
                lines.Add($"Events:\n{string.Join("\n", eventLines)}");
            }

            return string.Join("\n", lines);
        });

        sections.Add(
            $"=== CHRONICLES TO ORDER ({chronicles.Count}) ===\n\n" +
            string.Join("\n\n", chronicleBlocks));

        sections.Add(
            $"=== YOUR TASK ===\n" +
            $"Order these {chronicles.Count} chronicles chronologically within {eraName} " +
            $"(Y{startTick}\u2013Y{endTick}). Assign each a specific year.");

        return string.Join("\n\n", sections);
    }

    // =========================================================================
    // Prep prompts
    // =========================================================================

    /// <summary>
    /// Prep system prompt — generate historian's private reading notes.
    /// </summary>
    public static string BuildPrepSystemPrompt(string tone, HistorianConfig? historian = null)
    {
        var parsedTone = Enum.TryParse<HistorianTone>(tone, ignoreCase: true, out var t) ? t : HistorianTone.Weary;
        var sections = new List<string>();
        var name = historian?.Name ?? "a historian";

        sections.Add(
            $"You are {name}, preparing reading notes for your personal files. These are NOT for " +
            "publication — they are the private notes a scholar makes while working through source " +
            "material in preparation for a larger work.\n\n" +
            GetPrepToneDescription(parsedTone));

        if (historian is not null)
        {
            var identityLines = new List<string>
            {
                "## Your Identity",
                "",
                historian.Background,
                "",
                $"**Personality:** {string.Join(", ", historian.PersonalityTraits)}",
                $"**Known biases:** {string.Join(", ", historian.Biases)}",
                $"**Your stance toward this material:** {historian.Stance}",
            };
            sections.Add(string.Join("\n", identityLines));

            if (historian.PrivateFacts.Count > 0)
            {
                var factLines = historian.PrivateFacts.Select(f => $"- {f}");
                sections.Add($"## Private Knowledge (things you know that the texts don't always reflect)\n\n{string.Join("\n", factLines)}");
            }

            if (historian.RunningGags.Count > 0)
            {
                var gagLines = historian.RunningGags.Select(g => $"- {g}");
                sections.Add($"## Recurring Preoccupations\n\n{string.Join("\n", gagLines)}");
            }
        }

        sections.Add(
            "## Your Task\n\n" +
            "Write private reading notes for the chronicle below. These are the jottings you make in " +
            "the margins of your own working copy — observations you want to remember when you sit down " +
            "to write a broader narrative history of this era.\n\n" +
            "**What to include:**\n" +
            "- Key thematic threads and how they connect to the era's larger story\n" +
            "- Cast dynamics — who drives the action, who is acted upon, who is absent but felt\n" +
            "- Notable tensions, ironies, or contradictions worth remembering\n" +
            "- Details that surprised you, moved you, or struck you as significant\n" +
            "- Connections to other chronicles you've read (if they come to mind naturally)\n" +
            "- Things the chronicler got wrong, or right, or failed to notice\n\n" +
            "**What NOT to include:**\n" +
            "- Plot summary (you have the chronicle itself for that)\n" +
            "- Formal annotations or footnotes (you've already written those separately)\n" +
            "- Anything performative — no one will read these but you\n\n" +
            "**Format:** Plain prose, 300-500 words. Write as you actually think — shorthand is fine, " +
            "incomplete sentences are fine, personal asides are fine. These are notes, not an essay.\n\n" +
            "**Stay in character.** You are a historian reviewing primary sources. Never break the fourth wall.");

        return string.Join("\n\n", sections);
    }

    /// <summary>
    /// Prep user prompt — chronicle content for reading.
    /// </summary>
    public static string BuildPrepUserPrompt(
        string chronicleContent,
        string? summary,
        string? title = null,
        string? format = null,
        string? focalEraName = null,
        int? eraYear = null,
        IReadOnlyList<(string EntityName, bool IsPrimary, string? RoleName, string? EntityKind)>? cast = null,
        IReadOnlyList<string>? previousMarginNotes = null)
    {
        var sections = new List<string>();

        // Chronicle identity — TS puts era info on the same line as Format using " | "
        if (title is not null || format is not null || focalEraName is not null)
        {
            var eraInfo = "";
            if (focalEraName is not null)
            {
                var yearSuffix = eraYear.HasValue ? $" (Year {eraYear})" : "";
                eraInfo = $" | Era: {focalEraName}{yearSuffix}";
            }

            var identLines = new List<string>();
            if (title is not null) identLines.Add($"Title: \"{title}\"");
            if (format is not null) identLines.Add($"Format: {format}{eraInfo}");
            else if (eraInfo.Length > 0) identLines.Add($"Era: {focalEraName}{(eraYear.HasValue ? $" (Year {eraYear})" : "")}");
            sections.Add($"=== CHRONICLE ===\n{string.Join("\n", identLines)}");
        }

        // Cast
        if (cast is not null && cast.Count > 0)
        {
            var castLines = cast.Select(c =>
            {
                var role = c.RoleName ?? (c.IsPrimary ? "primary" : "supporting");
                var kindSuffix = c.EntityKind is not null ? $", {c.EntityKind}" : "";
                return $"- {c.EntityName} ({role}{kindSuffix})";
            });
            sections.Add($"=== CAST ===\n{string.Join("\n", castLines)}");
        }

        if (!string.IsNullOrEmpty(summary))
            sections.Add($"=== SUMMARY ===\n{summary}");

        // Truncate very long content (≈3000 words)
        const int MaxWords = 3000;
        var words = chronicleContent.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var truncated = words.Length > MaxWords;
        var contentText = truncated
            ? string.Join(" ", words[..MaxWords]) + "\n\n[... remainder truncated for brevity ...]"
            : chronicleContent;
        var truncationNote = truncated ? $" (first ~{MaxWords} words of {words.Length})" : "";
        sections.Add($"=== CHRONICLE TEXT{truncationNote} ===\n{contentText}");

        // Previous margin notes
        if (previousMarginNotes is not null && previousMarginNotes.Count > 0)
        {
            var noteLines = previousMarginNotes.Select(n => $"- {n}");
            sections.Add($"=== YOUR PREVIOUS MARGIN NOTES ON THIS CHRONICLE ===\n{string.Join("\n", noteLines)}");
        }

        sections.Add("=== YOUR TASK ===\nWrite your private reading notes for this chronicle. 300-500 words.");

        return string.Join("\n\n", sections);
    }
}
