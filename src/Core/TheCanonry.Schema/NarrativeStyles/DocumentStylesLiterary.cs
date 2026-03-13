namespace TheCanonry.Schema.NarrativeStyles;

/// <summary>
/// Literary and creative document styles: Proverbs through Haiku Collection.
/// </summary>
internal static partial class DefaultNarrativeStyles
{
    public static IReadOnlyList<DocumentNarrativeStyle> LiteraryDocumentStyles { get; } =
    [
        // 14. PROVERBS & SAYINGS
        new DocumentNarrativeStyle
        {
            Id = "proverbs-sayings",
            Name = "Proverbs & Sayings",
            Description = "Collection of folk wisdom, traditional sayings, and cultural aphorisms",
            Tags = ["document", "wisdom", "folklore", "cultural"],
            EraNarrativeWeight = EraNarrativeWeight.Flavor,
            DocumentInstructions = """
                This is a collection of authentic-feeling folk wisdom and proverbs.

                STRUCTURE:
                - Introduction (~60 words): Where you hear these sayings — which markets, which trades, which firesides. Present tense, positioning, not interpretation.
                - Common Sayings (~120 words): 4-6 proverbs about daily life, work, family. Practical wisdom.
                - Cautionary Sayings (~100 words): 3-4 proverbs warning against folly, danger, or moral failure.
                - Virtue Sayings (~100 words): 3-4 proverbs praising positive qualities valued by the culture.
                - Old Sayings (~80 words, optional): 2-3 more mysterious proverbs. Meaning debated or lost.

                VOICE & TONE: Collective wisdom. Third person observations. Some in imperative mood. Pithy, memorable, earthy, wise, traditional.

                Include local imagery, rhythm and rhyme where natural, concrete metaphors, occasional contradictions. Legendary figures might appear in sayings.

                Avoid modern concepts, abstract language, lengthy explanations within proverbs, forced rhymes.
                """,
            EventInstructions = """Historical events become cautionary tales. "Remember the [disaster]" type sayings.""",
            CraftPosture = "",
            TitleGuidance = "The title names the source — the people, the place, or the tradition from which the sayings come. Communal and referential register: what a culture calls its own collected wisdom when speaking about it. It should feel inherited rather than authored, oral rather than written. If a standout proverb serves as the title, it should be the one everyone already knows.",
            Roles =
            [
                new RoleDefinition { Role = "cultural-value", Count = new(1, 3), Description = "Tradition, belief, or principle expressed in the sayings", SelectionCriteria = "" },
                new RoleDefinition { Role = "folk-hero", Count = new(0, 2), Description = "Legendary figure referenced in proverbs", SelectionCriteria = "" },
                new RoleDefinition { Role = "cultural-institution", Count = new(0, 1), Description = "Guild, temple, or social group whose wisdom is cited", SelectionCriteria = "" },
                new RoleDefinition { Role = "proverbial-place", Count = new(0, 1), Description = "Location referenced in cautionary tales", SelectionCriteria = "" },
            ],
            Pacing = new DocumentPacingConfig { WordCount = new(350, 550) },
        },

        // 13. PRODUCT REVIEWS
        new DocumentNarrativeStyle
        {
            Id = "product-reviews",
            Name = "Product Reviews",
            Description = "Customer testimonials and critiques of goods, services, or establishments",
            Tags = ["document", "commercial", "reviews", "informal"],
            EraNarrativeWeight = EraNarrativeWeight.Flavor,
            DocumentInstructions = """
                This is a collection of authentic-feeling customer reviews with varied voices and opinions.

                STRUCTURE:
                - Subject Header (~30 words): Name of product/service/place. Vendor. Basic info.
                - Satisfied Customer (~100 words): Enthusiastic review. Specific praise. Would recommend.
                - Disappointed Customer (~100 words): Complaint with specifics. What went wrong. Warning to others.
                - Balanced Review (~100 words): Pros and cons. Specific use cases. Qualified recommendation.
                - Quick Takes (~80 words): 2-3 very brief reviews. Different perspectives. Varied literacy levels.

                VOICE & TONE: Multiple first-person voices. Varied education levels and personalities. Some formal, some casual. Varied, authentic, opinionated, specific, personal.

                Include specific details, comparisons to alternatives, usage context, personality quirks. Reviewers are ordinary people. Vendor might be a known entity.

                Avoid identical voices, all positive or all negative, generic praise, modern review site language.
                """,
            EventInstructions = "Reviews reference occasions. \"Bought for the festival\" or \"Needed after the flood.\"",
            CraftPosture = "",
            TitleGuidance = "Name the thing being reviewed — the product, the shop, or the vendor. Informal and opinionated register: what an unsatisfied or delighted customer would scrawl as a heading. Specific and grounded in the commercial world of the setting. The title should sound like ordinary people talking about ordinary transactions.",
            Roles =
            [
                new RoleDefinition { Role = "reviewed-subject", Count = new(1, 1), Description = "Product, service, or establishment being reviewed", SelectionCriteria = "" },
                new RoleDefinition { Role = "vendor", Count = new(0, 1), Description = "The seller or provider", SelectionCriteria = "" },
                new RoleDefinition { Role = "notable-reviewer", Count = new(0, 1), Description = "Famous customer whose opinion carries weight", SelectionCriteria = "" },
            ],
            Pacing = new DocumentPacingConfig { WordCount = new(350, 500) },
        },

        // 14. PERSONAL DIARY
        new DocumentNarrativeStyle
        {
            Id = "personal-diary",
            Name = "Personal Diary",
            Description = "Private journal entries spanning days or weeks - unguarded, inconsistent, never meant to be read",
            Tags = ["document", "personal", "private", "journal"],
            EraNarrativeWeight = EraNarrativeWeight.Contextual,
            DocumentInstructions = """
                This is a private diary. The writer is talking to themselves. There is no audience, no performance, no filter. Some entries are three words. Some are rambling paragraphs. The tone shifts with the writer's mood - bored, panicked, tender, petty, profound.

                STRUCTURE:
                - Entry 1 (~80 words): A mundane day. Establish the writer's voice, daily concerns, small complaints or pleasures. Dated.
                - Entry 2 (~120 words): Something happened. The writer processes it messily - not as narrative but as emotional reaction. Incomplete sentences. Second-guessing. Crossed-out phrases represented as [struck through: ...] or dashes trailing off.
                - Entry 3 (~150 words): The event's aftermath. The writer tries to make sense of things. Fails partially. Reveals something they wouldn't tell anyone - a fear, a desire, a secret opinion of someone.
                - Entry 4 (~80 words): Time has passed. Shorter. Either the crisis resolved or the writer moved on. A new mundane concern. Life continues.
                - Final Entry (~40 words, optional): Brief. Cryptic. May hint at something coming. Or may just be "Rain again. Need candles."

                VOICE & TONE: First person, utterly private. Grammar loosens when emotional. Abbreviations, nicknames, private references the reader can't fully decode. The writer doesn't explain context - they already know it. Unfiltered, inconsistent, raw, intimate, messy.

                Include private opinions about known entities (harsher or kinder than public face), mundane details (meals, weather, health), and emotional processing that's incomplete and honest.

                Avoid consistent tone, literary polish, awareness of audience, complete narratives, exposition for the reader's benefit. The diary is NOT trying to communicate.
                """,
            EventInstructions = """Events are experienced personally, not reported. "That thing at the market today" not "The merchant guild trade dispute." The writer assumes they remember context.""",
            CraftPosture = "",
            TitleGuidance = "The title is what the diarist or someone who found the diary later would write on the cover — a place name, a date range, or a simple identifying label. Not literary, not clever. Private and functional register: a notebook marked for the writer's own use, never meant to be a title at all. The less composed it sounds, the more authentic it feels.",
            Roles =
            [
                new RoleDefinition { Role = "diarist", Count = new(1, 1), Description = "The private voice - their unguarded self, messier than their public persona", SelectionCriteria = "" },
                new RoleDefinition { Role = "mentioned-person", Count = new(0, 2), Description = "People the diarist writes about - described with unfiltered private opinion", SelectionCriteria = "" },
                new RoleDefinition { Role = "private-concern", Count = new(0, 1), Description = "The thing weighing on them - may be petty, profound, or both", SelectionCriteria = "" },
            ],
            Pacing = new DocumentPacingConfig { WordCount = new(350, 550) },
        },

        // 15. INTERROGATION RECORD
        new DocumentNarrativeStyle
        {
            Id = "interrogation-record",
            Name = "Interrogation Record",
            Description = "Official transcript of questioning - terse exchanges, redacted sections, margin notes from a reviewing officer",
            Tags = ["document", "official", "transcript", "adversarial"],
            EraNarrativeWeight = EraNarrativeWeight.Contextual,
            DocumentInstructions = """
                This is an official interrogation transcript. Bureaucratic header. Terse Q&A format. The interrogator is persistent. The subject is evasive, frightened, or defiant. Truth leaks out through cracks in the subject's composure.

                STRUCTURE:
                - Header (~40 words): Official classification. Date. Location. Interrogator rank and name. Subject name and status (prisoner, witness, detainee). Authorization reference.
                - Opening Exchange (~100 words): Formal identification. Subject's state noted. The interrogator establishes authority and topic. The subject's first responses reveal their strategy - cooperation, defiance, or calculated partial truth.
                - Core Questioning (~200 words): The interrogator presses on key points. Questions get sharper. Answers get shorter or more evasive. Include at least one [REDACTED] passage and one [Subject pauses] or [Subject becomes agitated] notation.
                - Pressure Point (~100 words): The interrogator reveals something the subject didn't expect them to know. The subject's composure breaks briefly. What they say (or refuse to say) in this moment is the transcript's key revelation.
                - Margin Notes (~60 words): Handwritten notes from a reviewing officer, formatted as [MARGIN NOTE: ...]. These are a second voice commenting on the transcript - noting inconsistencies, flagging follow-up questions, or recording their own assessment.
                - Closing (~40 words): Session end notation. Subject returned to holding. Interrogator's signature. Recommendation for further action or release.

                VOICE & TONE: Two voices. The interrogator is professional, persistent, occasionally threatening through implication. The subject is constrained - every word chosen carefully because words have consequences here. Terse, adversarial, bureaucratic, tense, controlled.

                Include timestamps at key moments, [REDACTED] for sensitive information, stage directions in brackets ([Subject looks away], [Long silence], [Interrogator produces document]), and official reference numbers.

                Avoid casual language, long speeches, the subject volunteering information freely, clear resolution. The transcript raises as many questions as it answers.
                """,
            EventInstructions = """Events are what the interrogation is about. They appear as contested facts - the interrogator's version versus the subject's version, with the truth somewhere between.""",
            CraftPosture = "",
            TitleGuidance = "The title is a case file heading — subject name, case number, or incident description. Bureaucratic register: the voice of an institution processing a person. Clinical, impersonal, reducing a human situation to an administrative category. The colder the title sounds, the more the reader feels what is being done to the person inside it.",
            Roles =
            [
                new RoleDefinition { Role = "subject", Count = new(1, 1), Description = "The one being questioned - their evasions reveal as much as their answers", SelectionCriteria = "" },
                new RoleDefinition { Role = "interrogator", Count = new(1, 1), Description = "The questioner - professional, persistent, holding information back strategically", SelectionCriteria = "" },
                new RoleDefinition { Role = "reviewing-officer", Count = new(0, 1), Description = "The margin-note voice - reads the transcript later, catches what the interrogator missed", SelectionCriteria = "" },
            ],
            Pacing = new DocumentPacingConfig { WordCount = new(450, 650) },
        },

        // 16. FOLK SONG
        new DocumentNarrativeStyle
        {
            Id = "folk-song",
            Name = "Folk Song",
            Description = "Verse with stanzas and refrain - rhyming flow is primary, story optional or told obliquely through imagery",
            Tags = ["document", "verse", "song", "oral-tradition"],
            EraNarrativeWeight = EraNarrativeWeight.Flavor,
            DocumentInstructions = """
                This is a folk song meant to be sung. Rhythm and rhyme are paramount. Story is optional - if present, it's told obliquely through images and refrains rather than narrated directly. The song should feel like it's been passed through many voices, smoothed by repetition into something that sounds inevitable.

                STRUCTURE:
                - Collector's Note (~40 words): Brief context. Where this song is sung, by whom, on what occasions. Attribution if known ("commonly heard among the river traders" or "attributed to the blind poet Kael").
                - Verse 1 (~60 words, 4-6 lines): Establish the song's world in images, not exposition. A place, a mood, a season, a figure. The rhythm and rhyme scheme are set here - ABAB, AABB, or ABCB. The reader should hear the melody even without music.
                - Refrain (~30 words, 2-4 lines): The emotional heart. Repeated after each verse. Should be the most singable, most memorable lines. May be a question, a lament, a boast, or a cryptic phrase that gains meaning through repetition.
                - Verse 2 (~60 words, 4-6 lines): Develop or shift. New images that rhyme (thematically, not just phonetically) with verse 1. If there's a story, it advances here - but through image, not plot. The same rhyme scheme.
                - Refrain (repeat)
                - Verse 3 (~60 words, 4-6 lines): The turn or deepening. What seemed simple now carries weight. If a story, its consequence. If mood, its darkest or brightest point. The same structure, different content.
                - Refrain (repeat, possibly with one word changed for devastating effect)
                - Final Verse or Coda (~40 words, 2-4 lines, optional): Brief. May circle back to verse 1's opening image. May break the pattern. Ends the song with resonance.

                VOICE & TONE: The voice of many mouths. This song belongs to a community, not an author. The language is simple but precise - no word wasted. Singable, rhythmic, communal, haunting, worn-smooth.

                Rhyme and rhythm are more important than narrative clarity. Near-rhymes and slant-rhymes are welcome. Meter should be consistent within verses (tetrameter or trimeter work well for folk songs). The song should sound good read aloud.

                Include imagery specific to the world and culture. Use concrete nouns over abstractions. The song's meaning may be debated - "some say this is about the fall of the Silver Court, others claim it's a love song."

                Avoid prose disguised as verse, forced rhymes that sacrifice meaning, modern idioms, complex vocabulary. Folk songs use common words arranged uncommonly.
                """,
            EventInstructions = """Events become imagery. A war becomes "the red year." A leader becomes "the one who wore the crown of thorns." The song remembers events as feelings and images, not facts.""",
            CraftPosture = "",
            TitleGuidance = "The title is what the singer says before they begin — the name everyone in the room already knows. It comes from the song itself: a character's name, the opening phrase, the refrain's key words, or the place where events happened. Oral register: worn smooth by many mouths, easy to say, easy to remember. It belongs to a community, not an author.",
            Roles =
            [
                new RoleDefinition { Role = "song-subject", Count = new(1, 2), Description = "What or whom the song is about - may be a person, place, event, or feeling, transformed into imagery", SelectionCriteria = "" },
                new RoleDefinition { Role = "cultural-origin", Count = new(0, 1), Description = "The community that sings this - their concerns and values shape the song", SelectionCriteria = "" },
            ],
            Pacing = new DocumentPacingConfig { WordCount = new(250, 400) },
        },

        // 17. NURSERY RHYMES
        new DocumentNarrativeStyle
        {
            Id = "nursery-rhymes",
            Name = "Nursery Rhymes",
            Description = "Collection of short children's rhymes on diverse subjects - simple meter, memorable, often darker than they seem",
            Tags = ["document", "verse", "children", "folklore"],
            EraNarrativeWeight = EraNarrativeWeight.Flavor,
            DocumentInstructions = """
                This is a collection of nursery rhymes - the songs and chants children use for games, skipping, counting, or bedtime. Each rhyme is independent, touching a different subject. Together they form a mosaic of a culture's anxieties, values, and history filtered through children's mouths.

                STRUCTURE:
                - Collector's Introduction (~50 words): Where you gathered these — which schoolyards, which hearths, which streets. Present tense. You are a folklorist with a notebook, not a historian with a thesis. "The children of [place] sing these" not "these rhymes preserve history."
                - Rhyme 1 (~40 words, 4-6 lines): A counting or game rhyme. Bouncy meter. Used for choosing who's "it" or counting steps. References a historical figure or event obliquely. Children don't know the origin.
                - Rhyme 2 (~40 words, 4-6 lines): A cautionary rhyme. Warning dressed as play. "Don't go past the old wall / when the moon is thin" - real danger made into singsong.
                - Rhyme 3 (~40 words, 4-6 lines): A nonsense rhyme. Sounds like gibberish but may preserve corrupted names, places, or events. The meaning is lost; the rhythm survives.
                - Rhyme 4 (~40 words, 4-6 lines): A lullaby or bedtime rhyme. Gentler. May contain an undertone of sadness or threat ("and if the wind should take you away...").
                - Rhyme 5 (~40 words, 4-6 lines, optional): A clapping or taunting rhyme. Used between children. May reference a real person or group in mocking terms.
                - Collector's Note (~40 words, optional): A present-tense observation — what the children do with these rhymes, where the words change between neighborhoods, which ones the adults flinch at. Not historical analysis.

                VOICE & TONE: Children's voices. Simple words, strong rhythm, perfect or near-perfect rhyme. The rhymes should be genuinely singable and clappable. The darkness is accidental - children repeat what they've heard without understanding. Innocent, rhythmic, catchy, ancient, dark-underneath.

                Each rhyme should be metrically tight. Nursery rhymes are defined by their rhythm - they must scan. Trochaic or iambic tetrameter/trimeter. The beat should be so strong a child could clap to it.

                Include corrupted versions of real names from the world, references to real events transformed beyond recognition, specific local details (places, creatures, foods).

                Avoid adult vocabulary, complex syntax, obvious allegory, rhymes that sound composed rather than inherited. These should feel like they've been chanted by a thousand children who have no idea what the words originally meant.
                """,
            EventInstructions = """Historical events become unrecognizable in children's mouths. A plague becomes "Ring around the rosie." A tyrant becomes "Old King Grumblethorn." The rhyme preserves the emotional residue, not the facts.""",
            CraftPosture = "",
            TitleGuidance = "The title names the collection the way a community names its children's songs — by the most memorable character, the place where children sing them, or the figure who supposedly gathered them. Simple enough for a child to repeat: concrete nouns, strong rhythm, words that are fun in the mouth. It should feel like it has been shouted across playgrounds for longer than anyone remembers.",
            Roles =
            [
                new RoleDefinition { Role = "rhyme-subjects", Count = new(2, 4), Description = "Diverse subjects across the rhymes - historical figures, places, events, all transformed into children's chant material", SelectionCriteria = "" },
                new RoleDefinition { Role = "cultural-origin", Count = new(0, 1), Description = "The culture whose children sing these - their world is embedded in the imagery", SelectionCriteria = "" },
            ],
            Pacing = new DocumentPacingConfig { WordCount = new(250, 400) },
        },

        // 18. HAIKU COLLECTION
        new DocumentNarrativeStyle
        {
            Id = "haiku-collection",
            Name = "Haiku Collection",
            Description = "Three to four haikus - extreme compression, nature imagery, a single moment seized in 5-7-5 syllables",
            Tags = ["document", "verse", "minimal", "contemplative"],
            EraNarrativeWeight = EraNarrativeWeight.Flavor,
            DocumentInstructions = """
                This is a small collection of 3-4 haikus. Each is exactly three lines: 5 syllables, 7 syllables, 5 syllables. The haiku captures a single moment of perception - not a thought, not a narrative, but a seeing. The world briefly holds still.

                STRUCTURE:
                - Attribution (~20 words): The poet's name (or "anonymous"), the occasion or season, and the place. Brief and formal.
                - Haiku 1 (3 lines, 5-7-5): A nature observation. What is seen, heard, or felt in this moment. Present tense. No commentary. The image carries everything.
                - Haiku 2 (3 lines, 5-7-5): A human moment set against nature. A person doing something ordinary while the world does something extraordinary around them (or vice versa).
                - Haiku 3 (3 lines, 5-7-5): The deepest perception. An image that opens into something larger. Loneliness, mortality, wonder, change - expressed entirely through concrete detail.
                - Haiku 4 (3 lines, 5-7-5, optional): A final image. May circle back to the first haiku's season or setting. A closing note.

                VOICE & TONE: No voice. The haiku is a window, not a speaker. Present tense, concrete nouns, active verbs. No adjectives unless they are precise and sensory. No abstractions. No metaphors explained. Still, precise, vast, fleeting, natural.

                The 5-7-5 syllable count is STRICT. Count every syllable. This constraint is the form - do not violate it.

                Seasonal reference (kigo) in at least two haikus - specific plants, weather, light quality, animal behavior that places the moment in a season.

                A cutting word or break (kireji) in each haiku - a pause that divides the haiku into two parts, creating juxtaposition. This is what gives the haiku its resonance: two images held together, the space between them alive with meaning.

                Include imagery specific to the world - its flora, fauna, weather, landscapes. The haiku should feel rooted in this world, not generic.

                Avoid sentimentality, abstraction, commentary, narrative, metaphor spelled out, anything that explains rather than shows. The haiku trusts the reader completely.
                """,
            EventInstructions = "Events are absent. The haiku exists outside history, in the eternal present of perception. If a war raged yesterday, the haiku notices the frost on a blade of grass this morning.",
            CraftPosture = "",
            TitleGuidance = "The title grounds the collection in a concrete particular — a place, a season, a natural element, or the circumstance of composition. One to three words. Precise and sensory register, carrying more than it says. No abstraction, no metaliterary framing. The title should feel like the first perception — the one that opened the poet's attention.",
            Roles =
            [
                new RoleDefinition { Role = "observed-moment", Count = new(1, 2), Description = "The subject of perception - a season, a place, a fleeting natural event", SelectionCriteria = "" },
                new RoleDefinition { Role = "poet-context", Count = new(0, 1), Description = "The poet or the occasion - minimal, providing just enough frame", SelectionCriteria = "" },
            ],
            Pacing = new DocumentPacingConfig { WordCount = new(80, 150) },
        },
    ];
}
