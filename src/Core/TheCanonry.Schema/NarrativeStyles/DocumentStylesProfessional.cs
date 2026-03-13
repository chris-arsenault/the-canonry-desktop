namespace TheCanonry.Schema.NarrativeStyles;

internal static partial class DefaultNarrativeStyles
{
    /// <summary>6 professional and sacred document styles (tavern-notices through origin-myth).</summary>
    public static IReadOnlyList<DocumentNarrativeStyle> ProfessionalDocumentStyles { get; } =
    [
        // ── Tavern Notice Board ──────────────────────────────────────────
        new DocumentNarrativeStyle
        {
            Id = "tavern-notices",
            Name = "Tavern Notice Board",
            Description = "Collection of community postings: jobs, rumors, announcements, personal ads",
            Tags = ["document", "community", "rumors", "informal"],
            EraNarrativeWeight = EraNarrativeWeight.Flavor,

            DocumentInstructions = """
                This is a collection of notices as they would appear on a public tavern board.

                STRUCTURE:
                - Board Location (~30 words): Name of establishment. Brief atmosphere.
                - Help Wanted (~80 words): Someone needs something done. Clear task and payment.
                - Local Talk (~100 words): What people are whispering about. May or may not be true.
                - Announcements (~80 words): Upcoming events, changes, official notices.
                - Personal Notices (~80 words): Seeking companions, lost items, looking for relatives.
                - Curious Posting (~60 words, optional): Something intriguing or ominous. Questions unanswered.

                VOICE & TONE: Multiple first-person voices. Each notice reflects its poster - educated or not, local or foreign. Varied, authentic, community, informal, diverse-voices.

                Include spelling quirks for some posters, local slang, specific locations, realistic requests. Different social classes write differently.

                Avoid modern references, all notices sounding the same, only dramatic content.
                """,

            EventInstructions = "Events become rumors and gossip. Different takes on the same events add texture.",

            CraftPosture = "",

            TitleGuidance = "The title names the establishment or the board itself \u2014 what a regular would call the collection of notices pinned to the wall. Informal register: local, specific, the voice of a community that doesn't explain itself to outsiders. Grounded in a place name or a location people know by reputation.",

            Roles =
            [
                new RoleDefinition { Role = "establishment", Count = new(0, 1), Description = "The tavern or public house hosting the board" },
                new RoleDefinition { Role = "job-poster", Count = new(0, 1), Description = "Someone seeking help" },
                new RoleDefinition { Role = "rumor-subject", Count = new(0, 2), Description = "Person or event being gossiped about" },
                new RoleDefinition { Role = "mysterious-poster", Count = new(0, 1), Description = "Unknown entity leaving intriguing notice" },
            ],

            Pacing = new DocumentPacingConfig { WordCount = new(350, 550) },
        },

        // ── Field Report ─────────────────────────────────────────────────
        new DocumentNarrativeStyle
        {
            Id = "field-report",
            Name = "Field Report",
            Description = "Military scout report, expedition log, or reconnaissance document",
            Tags = ["document", "military", "reconnaissance", "tactical"],
            EraNarrativeWeight = EraNarrativeWeight.Contextual,

            DocumentInstructions = """
                This is a professional military or expedition field report.

                STRUCTURE:
                - Report Header (~50 words): Classification, date, unit, commander addressed.
                - Mission & Status (~60 words): What the mission was. Current status of unit.
                - Observations (~200 words): What was seen, heard, learned. Numbers, positions, movements.
                - Encounters (~100 words): Any interactions with hostiles, locals, or allies. Outcomes.
                - Tactical Assessment (~80 words): What this means. Threats, opportunities, unknowns.
                - Recommendations (~60 words): What the reporting officer suggests. Specific and actionable.

                VOICE & TONE: First person plural for unit actions. Third person for observations. Military register. Professional, concise, tactical, factual, urgent.

                Include numbers and quantities, directions and distances, time references, unit designations. Describe entities tactically - capabilities, positions.

                Avoid emotional language, speculation without marking it, irrelevant details, casual tone.
                """,

            EventInstructions = "Events are mission-relevant occurrences. Report with tactical implications.",

            CraftPosture = "",

            TitleGuidance = "The title is a file designation \u2014 what gets stamped on the cover before it is sent up the chain. Name the location, the operation, or the tactical subject. Military register: functional, abbreviated, stripped of personality. The title is for filing, not for reading aloud.",

            Roles =
            [
                new RoleDefinition { Role = "enemy-force", Count = new(0, 2), Description = "Hostile faction or army being observed" },
                new RoleDefinition { Role = "terrain-assessed", Count = new(0, 2), Description = "Territory, fortification, or location being reported on" },
                new RoleDefinition { Role = "capability-observed", Count = new(0, 2), Description = "Enemy abilities, magic, or weapons noted" },
                new RoleDefinition { Role = "reporting-unit", Count = new(0, 1), Description = "Scout or reconnaissance party submitting report" },
                new RoleDefinition { Role = "strategic-asset", Count = new(0, 1), Description = "Resource, weapon, or item of tactical importance" },
            ],

            Pacing = new DocumentPacingConfig { WordCount = new(450, 650) },
        },

        // ── Artisan's Catalogue ──────────────────────────────────────────
        new DocumentNarrativeStyle
        {
            Id = "artisans-catalogue",
            Name = "Artisan's Catalogue",
            Description = "Detailed catalog of items, artifacts, or creations with descriptions and provenance",
            Tags = ["document", "catalog", "items", "artifacts"],
            EraNarrativeWeight = EraNarrativeWeight.Flavor,

            DocumentInstructions = """
                This is an item catalog or collection inventory from a knowledgeable collector or artisan.

                STRUCTURE:
                - Introduction (~80 words): What this catalog covers. Notable inclusions. Curator credentials.
                - Catalog Entry (~150 words): Full description of one significant item. History, properties, significance.
                - Second Entry (~150 words): Different type of item. Contrast with first entry.
                - Third Entry (~120 words, optional): Perhaps a more mysterious or less documented piece.
                - Curator's Notes (~60 words, optional): Patterns observed, items sought, authentication concerns.

                VOICE & TONE: First person curatorial. Knowledgeable but accessible. Pride in the collection. Knowledgeable, appreciative, detailed, authoritative.

                Include physical details, provenance, special properties, comparative value. Items may be associated with entities as creators or former owners.

                Avoid generic descriptions, identical formats for each item, excessive jargon.
                """,

            EventInstructions = "Events give items history - \"used in the Battle of X\" or \"created during the Y crisis.\"",

            CraftPosture = "",

            TitleGuidance = "The title names the collection, the workshop, or the artisan \u2014 what would appear on the catalog's cover page in a confident hand. Trade register: proud but practical, establishing credibility through specificity. It should sound like something an artisan would hand to a patron, naming what they make and where to find them.",

            Roles =
            [
                new RoleDefinition { Role = "catalogued-item", Count = new(1, 3), Description = "Artifact, creation, or treasure being documented" },
                new RoleDefinition { Role = "creator-or-owner", Count = new(0, 2), Description = "Artisan who made it or notable previous owners" },
                new RoleDefinition { Role = "provenance-place", Count = new(0, 2), Description = "Locations significant to the item history" },
                new RoleDefinition { Role = "associated-power", Count = new(0, 1), Description = "Ability or enchantment the item possesses" },
            ],

            Pacing = new DocumentPacingConfig { WordCount = new(450, 700) },
        },

        // ── Sacred Text ──────────────────────────────────────────────────
        new DocumentNarrativeStyle
        {
            Id = "sacred-text",
            Name = "Sacred Text",
            Description = "Religious scripture, prophecy, or spiritual teaching from a culture or faith tradition",
            Tags = ["document", "religious", "spiritual", "sacred"],
            EraNarrativeWeight = EraNarrativeWeight.Contextual,

            DocumentInstructions = """
                This is a religious or sacred text with reverence and weight appropriate to sacred literature.

                STRUCTURE:
                - Invocation (~40 words): Traditional opening. Names of the divine. Blessing on the reader.
                - Core Teaching (~200 words): The main spiritual or moral content. Poetic structure. Memorable phrases.
                - Parable or Vision (~150 words, optional): A teaching story, prophetic vision, or divine encounter.
                - Precepts (~100 words): What followers must do or avoid. Stated with authority.
                - Closing Blessing (~50 words): Final blessing, promise, or warning. Memorable closing.

                VOICE & TONE: Divine voice, prophetic utterance, or ancient sage. Second person for commandments. Third person for narrative. Reverent, elevated, ancient, authoritative, poetic.

                Include repetition for emphasis, metaphor and symbol, direct address to faithful, cosmic scope. Divine beings, prophets, or founders may be named.

                Avoid casual language, modern idioms, uncertainty or hedging, irony.
                """,

            EventInstructions = "Mythic events, creation stories, or prophesied future events. Frame as eternal truths.",

            CraftPosture = "",

            TitleGuidance = "The title is a name, not a description \u2014 spoken the way believers speak the name of their scripture. It should feel like it has always existed: not chosen but revealed, not composed but received. Sacred register: elevated, set apart from common speech, carrying the weight of doctrine in as few words as possible. One to three words.",

            Roles =
            [
                new RoleDefinition { Role = "divine-teaching", Count = new(1, 2), Description = "Doctrine, law, or spiritual truth being revealed" },
                new RoleDefinition { Role = "sacred-power", Count = new(0, 1), Description = "Divine ability, blessing, or cosmic force" },
                new RoleDefinition { Role = "prophesied-era", Count = new(0, 1), Description = "Age that was, is, or will be" },
                new RoleDefinition { Role = "divine-figure", Count = new(0, 2), Description = "God, prophet, or holy person" },
                new RoleDefinition { Role = "sacred-place", Count = new(0, 1), Description = "Holy site or realm" },
            ],

            Pacing = new DocumentPacingConfig { WordCount = new(400, 650) },
        },

        // ── Creation Myth ────────────────────────────────────────────────
        new DocumentNarrativeStyle
        {
            Id = "creation-myth",
            Name = "Creation Myth",
            Description = "Cosmogonic narration \u2014 how the world was made, why it divided, what was sealed. Competing traditions, multiple shapers, mythic specificity",
            Tags = ["document", "myth", "cosmogony", "origin"],
            EraNarrativeWeight = EraNarrativeWeight.Contextual,

            DocumentInstructions = """
                This is a creation myth \u2014 a cosmogonic text narrating how the world was made, divided, and settled into its present shape.

                STRUCTURE:
                The myth moves from undifferentiation to differentiation: formless to formed, nameless to named, unified to divided. Let the cast and the world's fractures determine the proportions, but the arc follows this cosmogonic sequence:

                1. PRIMORDIAL STATE: Open with negative cosmology \u2014 enumerate what did not yet exist. "Before X had been named, before Y had been separated from Z." The primordial state is specific: primordial waters, a cosmic body, commingled substances, a generative darkness. Something exists, but nothing has been distinguished from anything else.

                2. THE COSMOGONIC ACT: How differentiation began. Multiple shapers with conflicting agendas \u2014 one builds while another steals, one creates by speech while another creates by sacrifice or dismemberment. Draw from the toolkit of cosmogonic motifs: separation of sky and earth, body-to-world transformation (a being's blood becomes rivers, bones become mountains), naming and speech as creative acts, cosmic combat whose aftermath becomes landscape, failed attempts before the world holds its shape. The shapers' contributions are real and costly. Their acts leave marks on the world that persist.

                3. THE DIVISION: Why the world split. The central fracture \u2014 what separated cultures, powers, or geographies. Caused by specific acts with specific consequences, where both sides of the split have legitimate claims.

                4. THE UNRESOLVED: What was sealed, buried, or left open. The myth carries its world's anxieties forward: the door that stays shut, the force contained rather than destroyed, the question the traditions still argue over.

                TEMPORAL ANCHOR:
                The myth belongs to the time of making. Its central acts are cosmogonic \u2014 the shaping, the dividing, the sealing. Events from later ages are consequences the myth foreshadows, not events it narrates. The figures exist here at their fullest scale.

                COMPETING TRADITIONS:
                This text was assembled from multiple source traditions that agree on events but disagree on meaning. The compiler is visible \u2014 the seams between accounts show. Where traditions contradict, both versions stand. The text has layers and argues with itself.

                COSMOGONIC REGISTER:
                Deep-time narration \u2014 geological ages compressed into paragraphs. Declarative, confident, primordial past tense ("in the time before time," "when the first vein split"). Parallelism and structural repetition: catalog passages that enumerate what was made from what ("from the teeth, the ridgeline stones; from the breath, the trade winds; from the open eye, the northern sea"). Paired opposites recur (light/dark, above/below, shaped/unworked). The rhythm is incantatory \u2014 closer to genealogical chant than to prose narrative.

                MYTHIC SPECIFICITY:
                Even in deep time, the world's physical reality holds. Gods and shapers carry specific objects, leave specific marks, bleed specific colors. Body-to-world correspondences are concrete and sensory: particular anatomies become particular geographies. Sacred means heavy with detail, dense with material.
                """,

            EventInstructions = "Foundational events are the myth itself. Creation events, schisms, and sealed catastrophes are narrated as the acts of shapers and the resistance of the substrate. Frame events as cosmological acts with physical consequences that persist in the present landscape.",

            CraftPosture = """
                Confident declaration throughout. Each tradition states its version as fact.
                The compiler shows the seams but does not resolve the contradictions.
                Restraint at the edges \u2014 what was sealed stays sealed, what is unanswered stays unanswered. The myth ends with the world as it is: fractured, contested, held together by acts still in progress.
                """,

            TitleGuidance = "The title names the text the way a civilization names its foundational document \u2014 a proper name that carries weight, spoken the way a people speak the name of their origin. Short, declarative, old-sounding. One to four words. A noun phrase, spoken as if it has always existed.",

            Roles =
            [
                new RoleDefinition { Role = "shaper", Count = new(1, 3), Description = "Entities that actively shaped or divided the world \u2014 creators, tricksters, builders. Their agendas conflict." },
                new RoleDefinition { Role = "adversary-witness", Count = new(0, 2), Description = "Forces that observed, tested, or opposed creation \u2014 older presences, cosmic opponents, those with competing claims on the substrate" },
                new RoleDefinition { Role = "prophet-keeper", Count = new(0, 2), Description = "Those who carry or guard knowledge from the making \u2014 hermits, seers, door-wardens" },
                new RoleDefinition { Role = "sacred-order", Count = new(0, 2), Description = "Groups or factions descended from the shapers' work \u2014 priesthoods, guilds, custodial orders" },
                new RoleDefinition { Role = "primordial-body", Count = new(1, 2), Description = "The world-substrate itself \u2014 locations that ARE the creation. The body from which geography was carved, the matter that was separated or dismembered into landscape." },
                new RoleDefinition { Role = "sacred-artifact", Count = new(0, 3), Description = "Objects of power from or before the making \u2014 instruments, weapons, sealed containers" },
                new RoleDefinition { Role = "sealed-threshold", Count = new(0, 2), Description = "Places where creation's work meets its limits \u2014 sealed doors, boundaries, containment sites" },
                new RoleDefinition { Role = "foundational-event", Count = new(0, 2), Description = "Occurrences that anchor the myth's timeline \u2014 the shattering, the division, the sealing" },
            ],

            Pacing = new DocumentPacingConfig { WordCount = new(1500, 3500) },
        },

        // ── Origin Myth ──────────────────────────────────────────────────
        new DocumentNarrativeStyle
        {
            Id = "origin-myth",
            Name = "Origin Myth",
            Description = "Gods who walk in the world \u2014 how the current age was forged by divine-scale figures whose acts reshaped the landscape itself",
            Tags = ["document", "myth", "origin", "age-transition", "divine"],
            EraNarrativeWeight = EraNarrativeWeight.Contextual,

            DocumentInstructions = """
                This is an origin myth \u2014 the story of divine or near-divine figures whose acts during a previous age shaped the world into its current form. The world already existed. These figures walked in it, and the world bent around them. Their griefs reshaped coastlines. Their conflicts created new geographies. Their departures changed the climate. Where a mortal chronicle records a battle, this records the mountain that was raised to win it.

                STRUCTURE:
                Three to five chapters, numbered with Roman numerals. Each chapter is a substantial movement of the myth \u2014 long enough to build, dense enough to carry weight. Let chapter breaks fall at genuine turning points in the narrative, not at each new topic or each new figure. Establish the figures in relation to each other and to the world in the same movement \u2014 their story is how they interacted, how their powers collided and complemented, not a sequence of isolated portraits.

                The arc: establish the old age and the figures who shaped it. Build toward what destabilized that age \u2014 divine-scale acts with physical consequences on the world. Move through the transition: what was destroyed, transformed, or carried. End at the threshold of the current age, where the figures are receding and what survives of them is partial.

                TEMPORAL ANCHOR:
                This myth belongs to the old age. Its central acts, its defining choices, its dramatic weight all belong to the time before the transition. Events that the current age records as recent history are consequences the myth foreshadows \u2014 echoes and inheritances, not the myth's own story. The figures' mortal-era deeds are aftermath. The myth tells what they did when they were still walking at full scale.

                VOICE:
                The myth speaks for itself. No compiler frame, no curatorial apparatus, no editorial commentary explaining where traditions diverge. Where traditions contradict, weave both versions into the narrative directly \u2014 let the reader feel the seam without a narrator pointing to it. The text is the myth as it has been told and retold, not an academic assembly of sources.

                MYTHIC REGISTER:
                Deep-time narration at divine scale. The figures' actions have geological and climatic consequences described with physical specificity. Parallelism and catalog passages that enumerate what a figure made, destroyed, or left behind. Declarative, confident, incantatory at the transitions.

                MYTHIC SPECIFICITY:
                Divine scale means more detail, not less. A god's weapon has a name and a material. A divine act leaves a specific geographic consequence \u2014 this particular ridge, that particular current, the silence in this specific valley. Their physical presence is overwhelming and particular.
                """,

            EventInstructions = "Events are the acts of divine-scale figures with world-shaping consequences. Anchor events in the old age \u2014 the myth tells what these figures did at full scale, before they diminished. Later-era events are consequences the myth foreshadows, not events it narrates.",

            CraftPosture = """
                Confident narration throughout. The myth knows what happened, even when it disagrees with itself about why.
                Where traditions contradict, both stand without resolution \u2014 the seams show in the telling, not in editorial commentary.
                Economy over exhaustiveness \u2014 each passage earns its place.
                """,

            TitleGuidance = "The title names the old age, the transition, or the figures themselves \u2014 what later generations call the time when gods walked. Short, heavy, carrying the weight of deep memory. One to four words. A noun phrase that sounds ancient and well-worn, spoken with reverence or fear depending on who speaks it.",

            Roles =
            [
                new RoleDefinition { Role = "elder-power", Count = new(1, 3), Description = "Divine or near-divine figures of the old age \u2014 beings whose acts reshaped geography, climate, and the structure of the world" },
                new RoleDefinition { Role = "inheritor", Count = new(0, 2), Description = "Those who carried something through the transition \u2014 keepers of knowledge, founders of the new age's first institutions" },
                new RoleDefinition { Role = "lost-order", Count = new(0, 2), Description = "Powers, alliances, or institutions that existed in the old age and were destroyed or transformed by the transition" },
                new RoleDefinition { Role = "shaped-ground", Count = new(1, 2), Description = "Locations that bear the marks of divine action \u2014 landscapes carved, frozen, raised, or broken by the figures of the old age" },
                new RoleDefinition { Role = "catalyst-event", Count = new(0, 2), Description = "The specific acts that triggered or defined the transition \u2014 divine choices with world-scale consequences" },
                new RoleDefinition { Role = "relic", Count = new(0, 3), Description = "Objects of power from the old age or from before it \u2014 things that survived the transition, things even the divine figures did not fully understand" },
                new RoleDefinition { Role = "sealed-legacy", Count = new(0, 2), Description = "What was sealed, buried, or withdrawn \u2014 divine works that the new age contains rather than understands" },
                new RoleDefinition { Role = "contested-figure", Count = new(0, 2), Description = "Figures the traditions disagree about \u2014 savior to one account, destroyer to another. Large enough that different communities experienced them differently." },
            ],

            Pacing = new DocumentPacingConfig { WordCount = new(1500, 3500) },
        },
    ];
}
