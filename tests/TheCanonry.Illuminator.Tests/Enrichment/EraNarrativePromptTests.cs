using TheCanonry.Illuminator.Chronicle;
using TheCanonry.Illuminator.Chronicle.PerspectiveSynthesis;
using TheCanonry.Illuminator.Enrichment.Prompts;
using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Tests.Enrichment;

public sealed class EraNarrativePromptTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    private static HistorianConfig MakeHistorian(
        string name = "Aurelius Keth",
        string background = "A weary archivist of five decades' service.",
        string stance = "skeptical but fair") =>
        new()
        {
            Name = name,
            Background = background,
            PersonalityTraits = ["precise", "sardonic", "patient"],
            Biases = ["distrust of oral tradition", "sympathy for builders"],
            Stance = stance,
            PrivateFacts = ["The northern seal was forged, not found"],
            RunningGags = ["the cost of heating the archive"],
        };

    private static EraNarrativePrompts.ThreadSynthesisContext MakeSynthesis() =>
        new()
        {
            Threads =
            [
                new EraNarrativePrompts.ThreadContext(
                    "The Forge Clans",
                    ["Forge Clans", "River Folk"],
                    "Dominance to dependency",
                    Register: "brittle ambitious collapse",
                    Material: "The forgemasters controlled iron supply until the river blockade."),
                new EraNarrativePrompts.ThreadContext(
                    "The River Folk",
                    ["River Folk"],
                    "Isolation to leverage",
                    Register: "quiet patient rising"),
            ],
            StrategicDynamics =
            [
                new EraNarrativePrompts.StrategicDynamicContext(
                    "Iron Dependency",
                    ["Forge Clans", "River Folk"],
                    "The River Folk controlled river access, forcing Forge Clan dependence."),
            ],
            Thesis = "The era was defined by the inversion of power between land and water.",
            Counterweight = "The bridge at Korrath survived — the one structure both peoples maintained.",
            Quotes =
            [
                new EraNarrativePrompts.QuoteContext(
                    "Iron remembers the hand that shaped it.",
                    "Forge Clan proverb, carved on the Great Anvil",
                    "Became ironic after the dependency formed."),
            ],
        };

    // =========================================================================
    // BuildThreadsSystemPrompt — tone personality
    // =========================================================================

    [Fact]
    public void BuildThreadsSystemPrompt_ContainsTonePersonality_Witty()
    {
        var prompt = EraNarrativePrompts.BuildThreadsSystemPrompt("witty");

        Assert.Contains("comic", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sly edge", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildThreadsSystemPrompt_ContainsTonePersonality_Cantankerous()
    {
        var prompt = EraNarrativePrompts.BuildThreadsSystemPrompt("cantankerous");

        Assert.Contains("angry", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("carpentry", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildThreadsSystemPrompt_ContainsTonePersonality_Bemused()
    {
        var prompt = EraNarrativePrompts.BuildThreadsSystemPrompt("bemused");

        Assert.Contains("naturalist", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("puzzled", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("defiant")]
    [InlineData("sardonic")]
    [InlineData("tender")]
    [InlineData("hopeful")]
    [InlineData("enthusiastic")]
    public void BuildThreadsSystemPrompt_ContainsTonePersonality_AllTones(string tone)
    {
        var prompt = EraNarrativePrompts.BuildThreadsSystemPrompt(tone);

        Assert.True(prompt.Length > 500, $"Expected prompt for tone '{tone}' to be substantial");
        Assert.Contains("threads", prompt);
        Assert.Contains("thesis", prompt);
    }

    [Fact]
    public void BuildThreadsSystemPrompt_ContainsJsonOutputFormat()
    {
        var prompt = EraNarrativePrompts.BuildThreadsSystemPrompt("witty");

        Assert.Contains("threads", prompt);
        Assert.Contains("thesis", prompt);
        Assert.Contains("counterweight", prompt);
        Assert.Contains("strategicDynamics", prompt);
        Assert.Contains("register", prompt);
    }

    [Fact]
    public void BuildThreadsSystemPrompt_UnknownTone_FallsBackToWitty()
    {
        var promptUnknown = EraNarrativePrompts.BuildThreadsSystemPrompt("nonexistent");
        var promptWitty = EraNarrativePrompts.BuildThreadsSystemPrompt("witty");

        Assert.Contains("sly edge", promptUnknown, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(promptWitty, promptUnknown);
    }

    // =========================================================================
    // BuildThreadsSystemPrompt — historian identity and arc direction
    // =========================================================================

    [Fact]
    public void BuildThreadsSystemPrompt_WithHistorian_IncludesIdentitySection()
    {
        var historian = MakeHistorian();
        var prompt = EraNarrativePrompts.BuildThreadsSystemPrompt(
            "witty", historian, eraName: "The Iron Age");

        Assert.Contains("Aurelius Keth", prompt);
        Assert.Contains("## Your Identity", prompt);
        Assert.Contains("precise, sardonic, patient", prompt);
        Assert.Contains("distrust of oral tradition", prompt);
        Assert.Contains("skeptical but fair", prompt);
        Assert.Contains("northern seal was forged", prompt);
        Assert.Contains("cost of heating the archive", prompt);
    }

    [Fact]
    public void BuildThreadsSystemPrompt_WithArcDirection_IncludesCriticalSection()
    {
        var prompt = EraNarrativePrompts.BuildThreadsSystemPrompt(
            "defiant", arcDirection: "The era ends in defiance, not despair.");

        Assert.Contains("## CRITICAL: ARC DIRECTION", prompt);
        Assert.Contains("The era ends in defiance, not despair.", prompt);
    }

    [Fact]
    public void BuildThreadsSystemPrompt_ContainsEraNameInOpening()
    {
        var prompt = EraNarrativePrompts.BuildThreadsSystemPrompt(
            "witty", eraName: "The Salt Wars");

        Assert.Contains("The Salt Wars", prompt);
    }

    // =========================================================================
    // BuildThreadsSystemPrompt — parity: source weight tiers and expanded rules
    // =========================================================================

    [Fact]
    public void BuildThreadsSystemPrompt_ContainsSourceWeightTiers()
    {
        var prompt = EraNarrativePrompts.BuildThreadsSystemPrompt("witty");

        Assert.Contains("Structural sources", prompt);
        Assert.Contains("Contextual sources", prompt);
        Assert.Contains("Flavor sources", prompt);
    }

    [Fact]
    public void BuildThreadsSystemPrompt_ContainsExpandedStrategicDynamics()
    {
        var prompt = EraNarrativePrompts.BuildThreadsSystemPrompt("witty");

        // TS parity: administrative records, trade ledgers, expansion zones
        Assert.Contains("administrative records", prompt);
        Assert.Contains("trade ledgers", prompt);
        Assert.Contains("expansion zones", prompt);
        Assert.Contains("connective tissue between cultural arcs", prompt);
    }

    [Fact]
    public void BuildThreadsSystemPrompt_ContainsExpandedRules()
    {
        var prompt = EraNarrativePrompts.BuildThreadsSystemPrompt("witty");

        // Rule 5: expanded with "who the peoples believe they are"
        Assert.Contains("who the peoples believe they are", prompt);
        // Rule 7: expanded with "hard constraint"
        Assert.Contains("hard constraint", prompt);
        // Rule 8: expanded with "internal process is a thread"
        Assert.Contains("internal process is a thread", prompt);
    }

    [Fact]
    public void BuildThreadsSystemPrompt_ContainsFocalEraSummaryExpansion()
    {
        var prompt = EraNarrativePrompts.BuildThreadsSystemPrompt("witty");

        // TS parity: expanded thesis guidance about era summary
        Assert.Contains("the era summary tells you what the era IS", prompt);
    }

    [Fact]
    public void BuildThreadsSystemPrompt_ContainsExpandedOutputFormatFields()
    {
        var prompt = EraNarrativePrompts.BuildThreadsSystemPrompt("witty");

        // TS parity: material field has extra guidance
        Assert.Contains("Characters, events, mechanisms, objects", prompt);
        // TS parity: origin field has artifact types
        Assert.Contains("carved phrase, precept, verse, saying", prompt);
        // TS parity: dynamic field has "State as fact"
        Assert.Contains("State as fact", prompt);
    }

    // =========================================================================
    // BuildThreadsUserPrompt — era context and source weight grouping
    // =========================================================================

    [Fact]
    public void BuildThreadsUserPrompt_IncludesEraNameAndDescription()
    {
        var prepBriefs = new List<EraNarrativePrompts.PrepBrief>();

        var prompt = EraNarrativePrompts.BuildThreadsUserPrompt(
            "The Salt Wars",
            "Conflict over the inland salt flats.",
            prepBriefs);

        Assert.Contains("The Salt Wars", prompt);
        Assert.Contains("Conflict over the inland salt flats.", prompt);
    }

    [Fact]
    public void BuildThreadsUserPrompt_IncludesPrepBriefsGroupedByWeight()
    {
        var prepBriefs = new List<EraNarrativePrompts.PrepBrief>
        {
            new("chr_1", "The Siege", "Siege fell in year 35.", EraYear: 35, Weight: "structural"),
            new("chr_2", "Cultural Rites", "Identity through ritual.", EraYear: 20, Weight: "contextual"),
            new("chr_3", "A Merchant's Tale", "Trade routes collapsed.", EraYear: 25),
        };

        var prompt = EraNarrativePrompts.BuildThreadsUserPrompt(
            "The Burning Age",
            "Fire and succession.",
            prepBriefs,
            worldDynamics: "Rising tension between the coastal factions.");

        Assert.Contains("STRUCTURAL SOURCES", prompt);
        Assert.Contains("CONTEXTUAL SOURCES", prompt);
        Assert.Contains("UNCLASSIFIED SOURCES", prompt);
        Assert.Contains("The Siege", prompt);
        Assert.Contains("[Year 35]", prompt);
        Assert.Contains("Rising tension between the coastal factions.", prompt);
    }

    [Fact]
    public void BuildThreadsUserPrompt_IncludesAdjacentErasAndThesis()
    {
        var prepBriefs = new List<EraNarrativePrompts.PrepBrief>();

        var prompt = EraNarrativePrompts.BuildThreadsUserPrompt(
            "The Iron Age",
            "An era of metal and ambition.",
            prepBriefs,
            previousEraName: "The Stone Age",
            previousEraSummary: "Tools were crude but effective.",
            nextEraName: "The Steam Age",
            nextEraSummary: "Machines replaced muscle.",
            previousEraThesis: "The world learned to shape stone before it learned to shape itself.");

        Assert.Contains("PRECEDING ERA", prompt);
        Assert.Contains("The Stone Age", prompt);
        Assert.Contains("FOLLOWING ERA", prompt);
        Assert.Contains("The Steam Age", prompt);
        Assert.Contains("PRECEDING ERA THESIS", prompt);
        Assert.Contains("learned to shape stone", prompt);
    }

    // =========================================================================
    // BuildGenerateSystemPrompt — word count target and craft guidance
    // =========================================================================

    [Fact]
    public void BuildGenerateSystemPrompt_ContainsWordCountTarget()
    {
        var prompt = EraNarrativePrompts.BuildGenerateSystemPrompt("witty", craftPosture: null);

        Assert.Contains("5,000", prompt);
        Assert.Contains("7,000", prompt);
    }

    [Fact]
    public void BuildGenerateSystemPrompt_ContainsAltitudeGuidance()
    {
        var prompt = EraNarrativePrompts.BuildGenerateSystemPrompt("bemused", craftPosture: null);

        Assert.Contains("Altitude", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cultures", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("grammatical subjects", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildGenerateSystemPrompt_WithCraftPosture_IncludesPostureText()
    {
        const string posture = "Prioritize the sensory over the analytical.";
        var prompt = EraNarrativePrompts.BuildGenerateSystemPrompt("hopeful", craftPosture: posture);

        Assert.Contains(posture, prompt);
    }

    [Fact]
    public void BuildGenerateSystemPrompt_WithoutCraftPosture_UsesDefaultPosture()
    {
        var prompt = EraNarrativePrompts.BuildGenerateSystemPrompt("hopeful", craftPosture: null);

        Assert.Contains("vividness", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("momentum", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildGenerateSystemPrompt_ContainsStructureGuidance()
    {
        var prompt = EraNarrativePrompts.BuildGenerateSystemPrompt("sardonic", craftPosture: null);

        Assert.Contains("Invocation", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("---", prompt);
        Assert.Contains("Closing", prompt, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // BuildGenerateSystemPrompt — parity: missing sections from TS
    // =========================================================================

    [Fact]
    public void BuildGenerateSystemPrompt_ContainsSilmarillionReference()
    {
        var prompt = EraNarrativePrompts.BuildGenerateSystemPrompt("witty", craftPosture: null);

        Assert.Contains("Silmarillion", prompt);
        Assert.Contains("Noldor", prompt);
        Assert.Contains("Dwarves", prompt);
        Assert.Contains("Doriath", prompt);
    }

    [Fact]
    public void BuildGenerateSystemPrompt_ContainsDeathMattersAltitudeParagraph()
    {
        var prompt = EraNarrativePrompts.BuildGenerateSystemPrompt("witty", craftPosture: null);

        Assert.Contains("A death matters because of what it reveals", prompt);
        Assert.Contains("individuals are the footnotes", prompt);
    }

    [Fact]
    public void BuildGenerateSystemPrompt_ContainsExpandedInterCulturalDynamics()
    {
        var prompt = EraNarrativePrompts.BuildGenerateSystemPrompt("witty", craftPosture: null);

        Assert.Contains("Internal cultural stories serve the geopolitical arc", prompt);
        Assert.Contains("weak at the negotiating table", prompt);
        Assert.Contains("dependencies weaponized", prompt);
    }

    [Fact]
    public void BuildGenerateSystemPrompt_ContainsVoiceExpansion()
    {
        var prompt = EraNarrativePrompts.BuildGenerateSystemPrompt("witty", craftPosture: null);

        Assert.Contains("analytical restraint is not prosodic restraint", prompt);
    }

    [Fact]
    public void BuildGenerateSystemPrompt_ContainsProseCraftExpansions()
    {
        var prompt = EraNarrativePrompts.BuildGenerateSystemPrompt("witty", craftPosture: null);

        // "Describe what is present"
        Assert.Contains("Describe what is present", prompt);
        Assert.Contains("absent earns silence", prompt);
        // "The world as actor"
        Assert.Contains("The world as actor", prompt);
        Assert.Contains("ice records what institutions miss", prompt);
        // "The variation IS the music"
        Assert.Contains("variation IS the music", prompt);
        // "Three named things"
        Assert.Contains("Three named things", prompt);
    }

    [Fact]
    public void BuildGenerateSystemPrompt_ContainsTimeSectionFromTs()
    {
        var prompt = EraNarrativePrompts.BuildGenerateSystemPrompt("witty", craftPosture: null);

        Assert.Contains("## Time", prompt);
        Assert.Contains("Compress years to a clause", prompt);
    }

    [Fact]
    public void BuildGenerateSystemPrompt_ContainsCharactersSectionFromTs()
    {
        var prompt = EraNarrativePrompts.BuildGenerateSystemPrompt("witty", craftPosture: null);

        Assert.Contains("## Characters", prompt);
        Assert.Contains("Characters arrive as forces", prompt);
    }

    [Fact]
    public void BuildGenerateSystemPrompt_ContainsMotifsSectionFromTs()
    {
        var prompt = EraNarrativePrompts.BuildGenerateSystemPrompt("witty", craftPosture: null);

        Assert.Contains("## Motifs", prompt);
        Assert.Contains("Recurring images give the work cohesion", prompt);
    }

    [Fact]
    public void BuildGenerateSystemPrompt_ContainsWhatThisIsForSectionFromTs()
    {
        var prompt = EraNarrativePrompts.BuildGenerateSystemPrompt("witty", craftPosture: null);

        Assert.Contains("## What This Is For", prompt);
        Assert.Contains("The world arc", prompt);
        Assert.Contains("The cultural arcs", prompt);
        Assert.Contains("Foreknowledge", prompt);
        Assert.Contains("Do not summarize the chronicles", prompt);
    }

    [Fact]
    public void BuildGenerateSystemPrompt_WithHistorian_ContainsHistorianSectionAtBottom()
    {
        var historian = MakeHistorian();
        var prompt = EraNarrativePrompts.BuildGenerateSystemPrompt(
            "witty", craftPosture: null, historian: historian, eraName: "The Iron Age");

        Assert.Contains("## The Historian", prompt);
        Assert.Contains("Aurelius Keth", prompt);
        Assert.Contains("weary archivist", prompt);
        Assert.Contains("editorial choices", prompt);
        Assert.Contains("private knowledge is stated as fact", prompt);
        // Historian section comes after "What This Is For" and before "Output"
        var historianIdx = prompt.IndexOf("## The Historian", StringComparison.Ordinal);
        var outputIdx = prompt.IndexOf("## Output", StringComparison.Ordinal);
        Assert.True(historianIdx < outputIdx, "Historian section should come before Output section");
    }

    [Fact]
    public void BuildGenerateSystemPrompt_ContainsExpandedAvoidBullets()
    {
        var prompt = EraNarrativePrompts.BuildGenerateSystemPrompt("witty", craftPosture: null);

        // TS parity: expanded avoid bullets
        Assert.Contains("negation adds nothing", prompt);
        Assert.Contains("recurrence is the argument", prompt);
        Assert.Contains("Trust the action", prompt);
        Assert.Contains("do not echo their phrasings", prompt);
        Assert.Contains("dead weight — cut it or give it a verb", prompt);
    }

    // =========================================================================
    // BuildGenerateUserPrompt — structured synthesis and world context
    // =========================================================================

    [Fact]
    public void BuildGenerateUserPrompt_IncludesEraNameAndDescription()
    {
        var prompt = EraNarrativePrompts.BuildGenerateUserPrompt(
            "The Quiet Century",
            "A hundred years of careful forgetting.");

        Assert.Contains("The Quiet Century", prompt);
        Assert.Contains("A hundred years of careful forgetting.", prompt);
    }

    [Fact]
    public void BuildGenerateUserPrompt_IncludesYearRange()
    {
        var prompt = EraNarrativePrompts.BuildGenerateUserPrompt(
            "The Iron Age",
            "An era of metal.",
            yearStart: 100,
            yearEnd: 350);

        Assert.Contains("Year range: 100–350", prompt);
    }

    [Fact]
    public void BuildGenerateUserPrompt_IncludesWorldArc()
    {
        var prompt = EraNarrativePrompts.BuildGenerateUserPrompt(
            "The Iron Age",
            "Metal and ambition.",
            previousEraName: "The Stone Age",
            previousEraSummary: "Tools were crude.",
            nextEraName: "The Steam Age",
            nextEraSummary: "Machines replaced muscle.");

        Assert.Contains("WORLD ARC", prompt);
        Assert.Contains("THE WORLD BEFORE (The Stone Age)", prompt);
        Assert.Contains("WHAT FOLLOWS (The Steam Age)", prompt);
    }

    [Fact]
    public void BuildGenerateUserPrompt_IncludesPrecedingVolumeThesis()
    {
        var prompt = EraNarrativePrompts.BuildGenerateUserPrompt(
            "The Iron Age",
            "Metal.",
            previousEraName: "The Stone Age",
            previousEraThesis: "Stone shaped the world before the world shaped stone.");

        Assert.Contains("PRECEDING VOLUME THESIS", prompt);
        Assert.Contains("Your argument in The Stone Age", prompt);
    }

    [Fact]
    public void BuildGenerateUserPrompt_IncludesStructuredSynthesisContext()
    {
        var synthesis = MakeSynthesis();

        var prompt = EraNarrativePrompts.BuildGenerateUserPrompt(
            "The Iron Age",
            "Metal.",
            synthesis: synthesis);

        // Cultural arcs
        Assert.Contains("CONTEXT: CULTURAL ARCS", prompt);
        Assert.Contains("The Forge Clans", prompt);
        Assert.Contains("Forge Clans, River Folk", prompt);
        Assert.Contains("Register: brittle ambitious collapse", prompt);
        Assert.Contains("forgemasters controlled iron supply", prompt);
        // Strategic dynamics
        Assert.Contains("CONTEXT: STRATEGIC DYNAMICS", prompt);
        Assert.Contains("Iron Dependency", prompt);
        // Thesis
        Assert.Contains("CONTEXT: THESIS", prompt);
        Assert.Contains("inversion of power", prompt);
        // Counterweight
        Assert.Contains("CONTEXT: COUNTERWEIGHT", prompt);
        Assert.Contains("bridge at Korrath", prompt);
        // Quotes
        Assert.Contains("CONTEXT: QUOTES", prompt);
        Assert.Contains("Iron remembers the hand that shaped it", prompt);
        Assert.Contains("Forge Clan proverb", prompt);
    }

    [Fact]
    public void BuildGenerateUserPrompt_IncludesCulturalIdentities()
    {
        var identities = new Dictionary<string, string>
        {
            ["The Northern Clans"] = "VALUES: honor above trade\nGOVERNANCE: council of elders",
        };

        var prompt = EraNarrativePrompts.BuildGenerateUserPrompt(
            "The Frozen Age",
            "Cold years.",
            culturalIdentities: identities);

        Assert.Contains("The Northern Clans", prompt);
        Assert.Contains("honor above trade", prompt);
    }

    [Fact]
    public void BuildGenerateUserPrompt_TaskMentionsQuotesAndGeopoliticalArc()
    {
        var prompt = EraNarrativePrompts.BuildGenerateUserPrompt(
            "The Iron Age",
            "Metal.");

        // TS parity: task section mentions quotes and geopolitical arc
        Assert.Contains("quotes are in-world text", prompt);
        Assert.Contains("Internal cultural stories serve the geopolitical arc", prompt);
    }

    // =========================================================================
    // BuildEditSystemPrompt — voice preservation and scene insertion
    // =========================================================================

    [Fact]
    public void BuildEditSystemPrompt_MentionsVoicePreservation()
    {
        var prompt = EraNarrativePrompts.BuildEditSystemPrompt();

        Assert.Contains("voice", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("clean", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildEditSystemPrompt_MentionsLengthPreservation()
    {
        var prompt = EraNarrativePrompts.BuildEditSystemPrompt();

        Assert.Contains("5,000", prompt);
        Assert.Contains("shorten", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildEditSystemPrompt_ContainsSceneInsertion()
    {
        var prompt = EraNarrativePrompts.BuildEditSystemPrompt();

        // TS parity: scene insertion bullet
        Assert.Contains("Scene insertion", prompt);
        Assert.Contains("natural home", prompt);
    }

    [Fact]
    public void BuildEditSystemPrompt_ContainsExpandedBullets()
    {
        var prompt = EraNarrativePrompts.BuildEditSystemPrompt();

        // TS parity: expanded register breaks bullet
        Assert.Contains("Remove or rewrite to match the surrounding register", prompt);
        // TS parity: expanded structural weight bullet
        Assert.Contains("last sustained paragraph before the coda", prompt);
        // TS parity: expanded internal contradiction bullet
        Assert.Contains("invocation is wrong and should be adjusted", prompt);
    }

    [Fact]
    public void BuildEditSystemPrompt_NoToneParameter_MatchesTsFixedPrompt()
    {
        var prompt = EraNarrativePrompts.BuildEditSystemPrompt();

        // TS edit system prompt has no tone description
        Assert.DoesNotContain("comic rather than tragic", prompt);
        Assert.DoesNotContain("angry", prompt);
        Assert.DoesNotContain("thrilled", prompt);
    }

    [Fact]
    public void BuildEditSystemPrompt_ContainsEditingGuidance()
    {
        var prompt = EraNarrativePrompts.BuildEditSystemPrompt();

        Assert.Contains("Register breaks", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Redundancy", prompt, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // BuildEditUserPrompt — tone, arc direction, threads, scene insertion
    // =========================================================================

    [Fact]
    public void BuildEditUserPrompt_ContainsNarrativeAndEraName()
    {
        const string narrative = "In the age before the wars, the river peoples built their towers...";

        var prompt = EraNarrativePrompts.BuildEditUserPrompt(
            "The Iron Age",
            narrative);

        Assert.Contains(narrative, prompt);
        Assert.Contains("ERA NARRATIVE: The Iron Age", prompt);
    }

    [Fact]
    public void BuildEditUserPrompt_ContainsToneDefault()
    {
        var prompt = EraNarrativePrompts.BuildEditUserPrompt(
            "The Iron Age",
            "Narrative text.");

        Assert.Contains("Tone: witty", prompt);
    }

    [Fact]
    public void BuildEditUserPrompt_ContainsExplicitTone()
    {
        var prompt = EraNarrativePrompts.BuildEditUserPrompt(
            "The Iron Age",
            "Narrative text.",
            tone: "defiant");

        Assert.Contains("Tone: defiant", prompt);
    }

    [Fact]
    public void BuildEditUserPrompt_ContainsArcDirection()
    {
        var prompt = EraNarrativePrompts.BuildEditUserPrompt(
            "The Iron Age",
            "Narrative text.",
            arcDirection: "The era ends in defiance, not despair.");

        Assert.Contains("Arc direction:", prompt);
        Assert.Contains("The era ends in defiance, not despair.", prompt);
    }

    [Fact]
    public void BuildEditUserPrompt_ContainsThreadRegisters()
    {
        var synthesis = MakeSynthesis();

        var prompt = EraNarrativePrompts.BuildEditUserPrompt(
            "The Iron Age",
            "Narrative text.",
            synthesis: synthesis);

        Assert.Contains("Thread registers", prompt);
        Assert.Contains("The Forge Clans", prompt);
        Assert.Contains("brittle ambitious collapse", prompt);
    }

    [Fact]
    public void BuildEditUserPrompt_ContainsThesisAndCounterweight()
    {
        var synthesis = MakeSynthesis();

        var prompt = EraNarrativePrompts.BuildEditUserPrompt(
            "The Iron Age",
            "Narrative text.",
            synthesis: synthesis);

        Assert.Contains("Thesis (structural reference", prompt);
        Assert.Contains("inversion of power", prompt);
        Assert.Contains("Counterweight (protect", prompt);
        Assert.Contains("bridge at Korrath", prompt);
    }

    [Fact]
    public void BuildEditUserPrompt_ContainsSceneInsertion()
    {
        var prompt = EraNarrativePrompts.BuildEditUserPrompt(
            "The Iron Age",
            "Narrative text.",
            editInsertion: "A passage about the forge-fires dying.");

        Assert.Contains("SCENE TO WEAVE IN", prompt);
        Assert.Contains("A passage about the forge-fires dying.", prompt);
    }

    [Fact]
    public void BuildEditUserPrompt_ContainsTaskInstruction()
    {
        var prompt = EraNarrativePrompts.BuildEditUserPrompt(
            "The Iron Age",
            "Some narrative text.");

        Assert.Contains("Copy-edit", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("voice is correct", prompt, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // BuildChronologySystemPrompt — historian identity and year range
    // =========================================================================

    [Fact]
    public void BuildChronologySystemPrompt_WithHistorian_IncludesIdentity()
    {
        var historian = MakeHistorian();
        var prompt = HistorianPrompts.BuildChronologySystemPrompt(
            historian, eraName: "The Iron Age", startTick: 100, endTick: 350);

        Assert.Contains("Aurelius Keth", prompt);
        Assert.Contains("## Your Identity", prompt);
        Assert.Contains("100", prompt);
        Assert.Contains("350", prompt);
        Assert.Contains("northern seal was forged", prompt);
        Assert.Contains("cost of heating the archive", prompt);
    }

    [Fact]
    public void BuildChronologySystemPrompt_WithYearRange_IncludesRangeInRules()
    {
        var prompt = HistorianPrompts.BuildChronologySystemPrompt(
            startTick: 10, endTick: 50);

        Assert.Contains("10–50", prompt);
    }

    // =========================================================================
    // Perspective synthesis prompt — parity features
    // =========================================================================

    [Fact]
    public void PerspectivePrompts_UserPrompt_ContainsWorldDynamicsSection()
    {
        var entities = new[]
        {
            new EntityContext
            {
                Id = "e1", Name = "Aldric", Kind = "npc",
                Prominence = "recognized", Status = "active", Culture = "northern",
            },
        };
        var dynamics = new[]
        {
            new WorldDynamic
            {
                Id = "wd-1",
                Text = "Northern expansion pressures southern trade routes.",
                Cultures = ["northern"],
            },
        };
        var input = new PerspectiveSynthesisInput
        {
            Constellation = ConstellationAnalyzer.Analyze(entities, []),
            Entities = entities,
            FactsWithMetadata = [],
            ToneFragments = new ToneFragments("Mythic and melancholic"),
            WorldDynamics = dynamics,
        };

        var prompt = PerspectivePrompts.BuildUserPrompt(input);

        Assert.Contains("WORLD DYNAMICS", prompt);
        Assert.Contains("Northern expansion pressures", prompt);
    }

    [Fact]
    public void PerspectivePrompts_FactSelectionDisplay_DefaultRange()
    {
        var (line, instruction) = PerspectivePrompts.ComputeFactSelectionDisplay(null, 0);

        Assert.Contains("4-6", line);
        Assert.Equal("Select 4-6", instruction);
    }

    [Fact]
    public void PerspectivePrompts_FactSelectionDisplay_CustomRange()
    {
        var config = new FactSelectionConfig(MinCount: 3, MaxCount: 8);
        var (line, instruction) = PerspectivePrompts.ComputeFactSelectionDisplay(config, 0);

        Assert.Contains("3-8", line);
        Assert.Contains("3-8", instruction);
    }

    [Fact]
    public void PerspectivePrompts_FactSelectionDisplay_RequiredFactsRaiseFloor()
    {
        var config = new FactSelectionConfig(MinCount: 2, MaxCount: 5);
        // 4 required facts: effectiveMin = max(2, 4) = 4
        var (_, instruction) = PerspectivePrompts.ComputeFactSelectionDisplay(config, 4);

        Assert.Contains("4-5", instruction);
    }
}
