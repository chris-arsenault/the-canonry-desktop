namespace TheCanonry.Schema.NarrativeStyles;

internal static partial class DefaultNarrativeStyles
{
    public static IReadOnlyList<StoryNarrativeStyle> ExperimentalStyles { get; } =
    [
        // ========================================================================
        // Rashomon
        // ========================================================================
        new StoryNarrativeStyle
        {
            Id = "rashomon",
            Name = "Rashomon",
            Description = "One pivotal moment told three times - each account complete, each contradictory, truth assembled by the reader",
            Tags = ["multi-POV", "unreliable", "layered"],
            EraNarrativeWeight = EraNarrativeWeight.Structural,

            NarrativeInstructions = """
                STRUCTURE: THREE ACCOUNTS OF ONE MOMENT
                This story retells the SAME pivotal event three times from three different positions. Not three sequential events - ONE event, THREE versions.

                The pivotal event is provided in your cast (the-moment). This is the ONLY event you dramatize in Scenes 1-3. Each scene tells this same moment from a different witness.

                === SCENE 1: FIRST WITNESS ===
                Open with a header naming this witness. Tell the pivotal event from their position - first-person or close third, inside their head. Include what they physically observed from where they stood, what they concluded about others' motives, and one specific detail they emphasize.

                This account should feel COMPLETE. A reader stopping here would believe this is the truth.

                === SCENE 2: SECOND WITNESS ===
                Header naming the second witness. Tell THE SAME EVENT from their position. The same observable facts, noticed differently. A different interpretation of the same actions. The emphasized detail from Scene 1 should be contradicted, ignored, or given opposite meaning. Include something Witness-A could not have seen from their position.

                The reader now holds two incompatible truths.

                === SCENE 3: THIRD WITNESS ===
                Header naming the third witness - often someone marginal to the main players. Tell THE SAME EVENT from this third position. Include something BOTH previous witnesses missed. A detail that destabilizes both accounts. No resolution - this account adds uncertainty, not clarity.

                === SCENE 4: AFTER ===
                Brief. No header. The moment is past. Show ONE of the witnesses alone, acting on their version of events. The reader knows their understanding is partial. The witness does not.

                End in that gap between what they believe and what we suspect.
                """,

            ProseInstructions = """
                TONE: Certain, observant, partial. Each witness speaks with complete confidence about their incomplete view. The prose carries no doubt even as the contradictions multiply. Three distinct voices - different rhythms, different concerns, different ways of seeing the same room.

                DIALOGUE: The same exchange appears in multiple accounts, quoted differently each time. The words shift slightly between tellings. Both versions feel accurate. The reader cannot know which is true.

                DESCRIPTION: Selective, character-driven. Each witness notices according to their nature. The same space rendered three ways, each rendering complete and confident.

                TECHNIQUE - THE PIVOT: One moment appears in all three accounts - a phrase, gesture, or glance. Each witness interprets it completely differently. This repeated-and-reframed moment is the heart of the story.

                TECHNIQUE - CONFIDENT INCOMPATIBILITY: No witness hedges. No "I think" or "perhaps." Each states their version as fact. The contradiction emerges from certainty meeting certainty.

                AVOID: Omniscient resolution. One account being obviously correct. Witnesses acknowledging their view is partial. Scene 4 revealing what really happened. Any voice outside the witnesses' perspectives.
                """,

            EventInstructions = "The event is given to you as the-moment in the cast. This is the ONLY thing you dramatize. Do not invent additional events. Tell this one moment three ways.",

            CraftPosture = """
                - Each account fully elaborated and confident. Certainty is the technique — no hedging.
                - Contradiction emerges from selective attention, not from altering facts.
                - Restraint in the closing. Brief, concrete, unresolved. Do not adjudicate.
                """,

            TitleGuidance = "The title names the event or object at the center — the thing all witnesses agree exists but disagree about entirely. It should feel stable, even factual, while the story beneath it fractures. A concrete noun phrase carrying the weight of contested truth. The title is the one thing everyone recognizes; everything else is disputed.",

            Roles =
            [
                new RoleDefinition { Role = "witness-a", Count = new(1, 1), Description = "First perspective - their account opens the story and establishes the baseline truth that subsequent accounts will complicate" },
                new RoleDefinition { Role = "witness-b", Count = new(1, 1), Description = "Second perspective - contradicts or complicates the first account through different position and interpretation" },
                new RoleDefinition { Role = "witness-c", Count = new(1, 1), Description = "Third perspective - often marginal to the main players, reveals what the principals missed or misread" },
                new RoleDefinition { Role = "the-moment", Count = new(1, 1), Description = "The pivotal event all three witnesses observed - must be specific and bounded, a single scene lasting minutes not hours" },
            ],

            Pacing = new StoryPacingConfig
            {
                TotalWordCount = new(1400, 1800),
                SceneCount = new(4, 4),
            },
        },

        // ========================================================================
        // Poetic/Lyrical
        // ========================================================================
        new StoryNarrativeStyle
        {
            Id = "poetic-lyrical",
            Name = "Poetic/Lyrical",
            Description = "Circular structure - the ending returns to the opening image, transformed by what came between",
            Tags = ["literary", "circular", "meditative"],
            EraNarrativeWeight = EraNarrativeWeight.Flavor,

            NarrativeInstructions = """
                STRUCTURE: CIRCULAR RETURN
                The story is a loop. The final scene returns to the opening image, but everything has changed. The structure itself carries meaning - time circles, understanding deepens, what seemed simple becomes complex.

                === SCENE 1: THE IMAGE ===
                A single vivid image, described with full attention. This is the poem's secret heart. Concrete and specific - a particular light, a particular object, a particular quality of air.

                Do not explain what it means. The meaning is in the seeing.

                This scene should be SHORT - a paragraph or two of pure presence. End the scene while still in the image.

                === SCENE 2: DEPARTURE ===
                Movement away. The consciousness begins to wander - through memory, through association, through what the image evokes. Time becomes fluid. Past and present may interweave.

                One image leads to another through hidden rhymes - color, texture, feeling, sound. The path is emotional logic, not narrative logic.

                The absence (if one is assigned) may hover here - what is longed for or lost.

                === SCENE 3: THE ENCOUNTER ===
                A presence enters. Another consciousness, a visitor, a memory made vivid. Conversation is less about information than about rhythm - what's said, what's almost said, what remains silent.

                This is not plot. This is two presences sharing space, briefly.

                === SCENE 4: THE RETURN ===
                Return to the opening image. Use SIMILAR OR IDENTICAL LANGUAGE from Scene 1, but now every word carries the weight of what came between.

                The image has not changed. The consciousness has.

                End IN the image, not after it. No explanation. No moral. Just the image, seen newly.
                """,

            ProseInstructions = """
                TONE: Luminous, precise, haunting. Every word chosen for sound as well as meaning.

                DIALOGUE: Sparse. When words come, they carry weight. Silences are as important as speech. What is not said.

                DESCRIPTION: Concrete details that open into abstraction. Synesthesia welcome - colors that sound, textures that taste. Find the exact word even if it takes the whole sentence to get there.

                TECHNIQUE - REPETITION WITH VARIATION: Key phrases, images, rhythms should echo. Not identical repetition but rhyme - the same shape with different content.

                TECHNIQUE - WHITE SPACE: Let scenes breathe. Short paragraphs. Space between movements. Trust silence.

                TECHNIQUE - THE RETURN: The final scene should quote or closely echo the opening. The reader should feel the loop close - same words, different weight.

                AVOID: Plot mechanics. Explaining what images mean. Rushing to conclusion. Generic "beautiful" language - find the strange, specific beauty.
                """,

            EventInstructions = "Events are prompts for meditation, not drivers. They exist to be contemplated, not resolved.",

            CraftPosture = """
                - Trust the image. If it needs explanation, replace the explanation with a better image.
                - White space is compositional. Short paragraphs. Let the poem breathe in gaps.
                - Sound and meaning carry equal weight. Rhythm is a structural element.
                """,

            TitleGuidance = "The title is an image, not a description of one. One to four words. Concrete and sensory — a color, a texture, a quality of light, a natural element. It should carry the emotional weight of the whole piece in a single phrase the reader returns to after finishing. Sound matters as much as meaning; say it aloud.",

            Roles =
            [
                new RoleDefinition { Role = "consciousness", Count = new(1, 1), Description = "The perceiving presence - we see through them, feel with them" },
                new RoleDefinition { Role = "the-image", Count = new(1, 1), Description = "The central image that opens and closes the loop - must be concrete and specific" },
                new RoleDefinition { Role = "presence", Count = new(0, 1), Description = "What enters awareness - visitor, memory, other consciousness" },
                new RoleDefinition { Role = "absence", Count = new(0, 1), Description = "What is longed for or lost - may never appear directly" },
            ],

            Pacing = new StoryPacingConfig
            {
                TotalWordCount = new(1000, 1400),
                SceneCount = new(3, 4),
            },
        },

        // ========================================================================
        // Dark Comedy
        // ========================================================================
        new StoryNarrativeStyle
        {
            Id = "dark-comedy",
            Name = "Dark Comedy",
            Description = "One disaster escalating through reasonable responses - the gap between catastrophe and procedure is the comedy",
            Tags = ["comedy", "escalation", "deadpan"],
            EraNarrativeWeight = EraNarrativeWeight.Structural,

            NarrativeInstructions = """
                STRUCTURE: CASCADING CATASTROPHE
                A single disaster that escalates because every reasonable response makes it worse. Not multiple funny situations - one serious situation met with inadequate tools. The comedy lives in the gap between what's happening and how it's being handled.

                Real stakes. Real consequences. Real damage. The fool does everything right and everything goes wrong anyway.

                === SCENE 1: THE SMALL PROBLEM ===
                A routine task. Standard procedure. The fool is competent, professional, following protocol. Something small goes wrong - not their fault, just circumstance. They respond reasonably.

                Establish the system's rules and the fool's competence within them. The audience should trust that this person knows what they're doing.

                === SCENE 2: THE ESCALATION ===
                The reasonable response has made things worse. The problem is no longer small. The fool consults procedure, finds the next appropriate step, implements it correctly.

                Things get worse. The system's tools are inadequate but they're the only tools available. The fool keeps documenting.

                === SCENE 3: THE CATASTROPHE ===
                Full disaster. Real consequences - people are hurt, things are permanently damaged, the situation is beyond recovery. The fool is still following procedure because what else can they do?

                The comedy peaks here: catastrophe unfolding while someone fills out the correct forms. "I followed procedure" spoken into the abyss.

                === SCENE 4: THE SYSTEM CONTINUES ===
                Aftermath. The disaster is contained or past. The damage is real and lasting. The system processes what happened through its inadequate categories.

                The fool is rewarded - promoted, commended, given more responsibility. Their documentation was thorough. The system learned nothing. A new task awaits.

                End with the fool accepting the next assignment, or a new fool approaching the same trap.
                """,

            ProseInstructions = """
                TONE: Deadpan, clinical, precise. The narrator observes catastrophe with the detachment of an incident report. No one thinks they're in a comedy. Everyone is doing their best.

                DIALOGUE: Characters mean what they say. They're not being funny - they're being professional in unprofessional circumstances. Bureaucratic language applied to disaster. Technical terms for catastrophe.

                DESCRIPTION: Specific observation of escalating disaster. The exact form number. The precise policy that doesn't cover this situation. The careful documentation of things going irreversibly wrong.

                TECHNIQUE - THE GAP: Comedy lives in the distance between what's happening and how it's being processed. Catastrophe described in bureaucratic language. Cosmic horror met with paperwork.

                TECHNIQUE - REAL STAKES: People get hurt. Things break permanently. The disaster has consequences that outlast the story. This is not slapstick - the collateral damage matters.

                TECHNIQUE - THE COMPETENT FOOL: The protagonist isn't stupid. They're good at their job. They follow procedure correctly. The system is what fails, not the person. The fool must be sympathetic - we would do the same thing in their position.

                TECHNIQUE - DEADPAN ESCALATION: Each scene worse than the last, same tone throughout. Never acknowledge the absurdity. The characters take everything seriously. The gap between their seriousness and the situation is the joke.

                AVOID: Jokes. Punchlines. Winking at the audience. Characters being funny on purpose. Consequence-free disaster. Stupid protagonists. The tragedy must be real for the comedy to land.
                """,

            EventInstructions = "Events are triggers for systemic failure. The catalyst should be small, reasonable, forgettable - something anyone might do. The catastrophe emerges from the system, not the individual.",

            CraftPosture = """
                - Never acknowledge the absurdity. The gap between prose register and content does the work.
                - Escalation is procedural, not dramatic. Each step follows logically from the last.
                - Linger on consequences. The comedy requires that the damage is real and specific.
                """,

            TitleGuidance = """The title should sound like a bureaucratic label, an incident report heading, or a perfectly reasonable description of something that is not reasonable at all. Flat register, no winking. The gap between the title's composure and the story's catastrophe is where the comedy lives. The more procedural and precise, the funnier.""",

            Roles =
            [
                new RoleDefinition { Role = "fool", Count = new(1, 2), Description = "The reasonable person trapped in unreasonable circumstances - competent, professional, doing everything right" },
                new RoleDefinition { Role = "system", Count = new(1, 1), Description = "The inadequate structure - bureaucracy, protocol, or procedure that cannot handle what it encounters" },
                new RoleDefinition { Role = "catalyst", Count = new(0, 1), Description = "What sets the disaster in motion - small, routine, the kind of thing that happens every day" },
                new RoleDefinition { Role = "victim", Count = new(0, 2), Description = "Collateral damage - those permanently affected by the catastrophe through no fault of their own" },
            ],

            Pacing = new StoryPacingConfig
            {
                TotalWordCount = new(1600, 2200),
                SceneCount = new(4, 4),
            },
        },

        // ========================================================================
        // Heroic Fantasy
        // ========================================================================
        new StoryNarrativeStyle
        {
            Id = "heroic-fantasy",
            Name = "Heroic Fantasy",
            Description = "The classic hero's journey in explicit three-act form - departure, ordeal, return",
            Tags = ["heroic", "three-act", "mythic"],
            EraNarrativeWeight = EraNarrativeWeight.Structural,

            NarrativeInstructions = """
                STRUCTURE: CLASSIC THREE-ACT
                The hero's journey in its clearest form. Three distinct movements with clear breaks between them. This is mythic storytelling - good and evil are real, transformation is possible, the world can be saved.

                === ACT I: DEPARTURE (1-2 scenes) ===
                The hero in their ordinary world. Establish what they have to lose. The world is already touched by darkness or lacking something vital.

                THE CALL: Disruption arrives - the guide appears, the threat manifests, the quest-object reveals itself. The hero may resist ("I can't leave" / "I'm not ready" / "Choose someone else").

                THE THRESHOLD: The hero commits. They leave behind everything familiar. The ordinary world recedes. Mark this crossing clearly - a door that won't reopen, a shore that fades, a word that can't be unsaid.

                === ACT II: THE ORDEAL (2-3 scenes) ===
                The longest section. The hero faces trials that test specific virtues. Each challenge should test something different - courage, wisdom, sacrifice, trust.

                COMPANIONS: Allies appear. Each represents something the hero will need. Their loyalty should be tested and proven.

                THE ABYSS: The darkest moment. Apparent defeat. Perhaps a companion falls. The quest seems lost. The hero must find something in themselves they didn't know was there.

                === ACT III: RETURN (1 scene) ===
                The final confrontation. Internal and external battles converge. The hero uses everything learned. Victory comes not from strength alone but from transformation.

                THE NEW WORLD: Brief glimpse of what victory created. The hero is changed. The world is changed. End with the new order taking shape - not every detail resolved, but the shape clear.
                """,

            ProseInstructions = """
                TONE: Heroic, stirring, grand. The language of legends. This story wants to be told around fires.

                DIALOGUE: Oaths and declarations. Characters speak as if their words will be remembered. Avoid modern idioms. "I will hold this passage" not "I've got this."

                DESCRIPTION: Vivid, colorful. Good is beautiful (but not soft); evil is terrible (but not cartoonish). Magic costs something and means something. Landscapes carry moral weight.

                TECHNIQUE - THE THRESHOLD: Mark act breaks clearly. The hero crossing into adventure should feel momentous. Don't rush past transitions.

                TECHNIQUE - THE TRIAL: Each trial tests something specific. Name it (even if only to yourself). Courage. Trust. Sacrifice. The hero fails or succeeds based on virtue, not luck.

                TECHNIQUE - THE TRANSFORMATION: By Act III, the hero should be visibly different from Act I. Show it in how they move, speak, choose.

                AVOID: Irony. Deconstruction. Moral ambiguity. Anticlimactic endings. This is not the place to subvert the genre - play it straight.
                """,

            EventInstructions = "Events are trials and victories. Each is a step in the hero's transformation. Treat them as legendary deeds.",

            CraftPosture = """
                - Mythic simplicity. Clean, powerful strokes over elaborate texture. When in doubt, cut.
                - Let sacrifice and transformation speak for themselves. Do not narrativize internal process.
                - The world exists through what characters touch and see, not through explanation.
                """,

            TitleGuidance = "Common words arranged with mythic weight. The title should sound ancient even if every word is simple — the kind of name that survives oral retelling across generations. It names the hero, the quest, or the legendary thing in a way that feels inevitable. Short, rhythmic, spoken-aloud quality. Simple monosyllables over Latinate abstractions.",

            Roles =
            [
                new RoleDefinition { Role = "hero", Count = new(1, 1), Description = "The chosen one - starts ordinary, becomes extraordinary" },
                new RoleDefinition { Role = "darkness", Count = new(1, 1), Description = "The evil to be vanquished - dark lord, corrupting power, or malevolent force" },
                new RoleDefinition { Role = "guide", Count = new(0, 1), Description = "Mentor figure who provides wisdom and/or the call" },
                new RoleDefinition { Role = "companion", Count = new(0, 2), Description = "Those who journey with the hero - may fall, may be saved" },
                new RoleDefinition { Role = "quest-object", Count = new(0, 1), Description = "What is sought - weapon, knowledge, place of power" },
                new RoleDefinition { Role = "the-calling", Count = new(0, 1), Description = "The prophecy, ancient law, forbidden power, or world-event that sets the quest in motion. Defines what the hero must confront beyond any single enemy" },
            ],

            Pacing = new StoryPacingConfig
            {
                TotalWordCount = new(1800, 2400),
                SceneCount = new(4, 6),
            },
        },
    ];
}
