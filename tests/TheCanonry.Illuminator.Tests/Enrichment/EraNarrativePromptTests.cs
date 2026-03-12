using TheCanonry.Illuminator.Enrichment.Prompts;

namespace TheCanonry.Illuminator.Tests.Enrichment;

public sealed class EraNarrativePromptTests
{
    // =========================================================================
    // BuildThreadsSystemPrompt — tone personality
    // =========================================================================

    [Fact]
    public void BuildThreadsSystemPrompt_ContainsTonePersonality_Witty()
    {
        var prompt = EraNarrativePrompts.BuildThreadsSystemPrompt("witty");

        // Witty tone distinctive phrase
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

        // Every tone description is non-trivial; the prompt should be substantial
        Assert.True(prompt.Length > 500, $"Expected prompt for tone '{tone}' to be substantial");
        // Each prompt includes the JSON output format
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

        // Should contain the same tone description text
        Assert.Contains("sly edge", promptUnknown, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(promptWitty, promptUnknown);
    }

    // =========================================================================
    // BuildGenerateSystemPrompt — word count target and craft guidance
    // =========================================================================

    [Fact]
    public void BuildGenerateSystemPrompt_ContainsWordCountTarget()
    {
        var prompt = EraNarrativePrompts.BuildGenerateSystemPrompt("witty", craftPosture: null);

        // The 5,000–7,000 word target must be present
        Assert.Contains("5,000", prompt);
        Assert.Contains("7,000", prompt);
    }

    [Fact]
    public void BuildGenerateSystemPrompt_ContainsAltitudeGuidance()
    {
        var prompt = EraNarrativePrompts.BuildGenerateSystemPrompt("bemused", craftPosture: null);

        Assert.Contains("Altitude", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cultures", prompt, StringComparison.OrdinalIgnoreCase);
        // No individuals — cultures are the actors
        Assert.Contains("grammatical subjects", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildGenerateSystemPrompt_ContainsToneDescription()
    {
        var prompt = EraNarrativePrompts.BuildGenerateSystemPrompt("defiant", craftPosture: null);

        Assert.Contains("grief", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("proud", prompt, StringComparison.OrdinalIgnoreCase);
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

        // Default posture is present when none provided
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
    // BuildGenerateUserPrompt — chronicle summaries and thread synthesis
    // =========================================================================

    [Fact]
    public void BuildGenerateUserPrompt_IncludesChroniclesSummaries()
    {
        var chronicles = new List<(string, string, string)>
        {
            ("chr_1", "The First War", "A war that ended in ruin."),
            ("chr_2", "The Builder's Age", "An era of construction and ambition."),
        };

        var prompt = EraNarrativePrompts.BuildGenerateUserPrompt(
            "The Iron Age",
            "An era defined by conflict and construction.",
            "Threads: rise of the Iron Lords, fall of the river clans.",
            chronicles,
            culturalIdentities: null);

        Assert.Contains("The First War", prompt);
        Assert.Contains("A war that ended in ruin.", prompt);
        Assert.Contains("The Builder's Age", prompt);
        Assert.Contains("An era of construction and ambition.", prompt);
    }

    [Fact]
    public void BuildGenerateUserPrompt_IncludesThreadSynthesis()
    {
        var chronicles = new List<(string, string, string)>
        {
            ("chr_1", "A Chronicle", "Summary of events."),
        };

        const string synthesis = "Thread 1: The Merchant Empire — rise and fragmentation (register: brittle ambitious collapse)";

        var prompt = EraNarrativePrompts.BuildGenerateUserPrompt(
            "The Trade Era",
            "Commerce dominated.",
            synthesis,
            chronicles,
            culturalIdentities: null);

        Assert.Contains(synthesis, prompt);
    }

    [Fact]
    public void BuildGenerateUserPrompt_IncludesCulturalIdentities()
    {
        var chronicles = new List<(string, string, string)>
        {
            ("chr_1", "A Tale", "A summary."),
        };
        var identities = new Dictionary<string, string>
        {
            ["The Northern Clans"] = "VALUES: honor above trade\nGOVERNANCE: council of elders",
        };

        var prompt = EraNarrativePrompts.BuildGenerateUserPrompt(
            "The Frozen Age",
            "Cold years and colder politics.",
            "Thread synthesis here.",
            chronicles,
            culturalIdentities: identities);

        Assert.Contains("The Northern Clans", prompt);
        Assert.Contains("honor above trade", prompt);
    }

    [Fact]
    public void BuildGenerateUserPrompt_IncludesEraNameAndDescription()
    {
        var chronicles = new List<(string, string, string)>();

        var prompt = EraNarrativePrompts.BuildGenerateUserPrompt(
            "The Quiet Century",
            "A hundred years of careful forgetting.",
            "No threads.",
            chronicles,
            culturalIdentities: null);

        Assert.Contains("The Quiet Century", prompt);
        Assert.Contains("A hundred years of careful forgetting.", prompt);
    }

    // =========================================================================
    // BuildThreadsUserPrompt — era context and chronicles
    // =========================================================================

    [Fact]
    public void BuildThreadsUserPrompt_IncludesEraNameAndDescription()
    {
        var chronicles = new List<(string, string, string)>();

        var prompt = EraNarrativePrompts.BuildThreadsUserPrompt(
            "The Salt Wars",
            "Conflict over the inland salt flats.",
            chronicles,
            culturalIdentities: null,
            worldDynamics: null,
            prepBriefs: null);

        Assert.Contains("The Salt Wars", prompt);
        Assert.Contains("Conflict over the inland salt flats.", prompt);
    }

    [Fact]
    public void BuildThreadsUserPrompt_IncludesChroniclesAndPrepBriefs()
    {
        var chronicles = new List<(string, string, string)>
        {
            ("chr_42", "The Siege of Korrath", "A three-year siege that ended the dynasty."),
        };
        var prepBriefs = new List<string>
        {
            "Reading notes: The chronicler is unreliable past year 40.",
        };

        var prompt = EraNarrativePrompts.BuildThreadsUserPrompt(
            "The Burning Age",
            "Fire and succession.",
            chronicles,
            culturalIdentities: null,
            worldDynamics: "Rising tension between the coastal factions.",
            prepBriefs: prepBriefs);

        Assert.Contains("chr_42", prompt);
        Assert.Contains("A three-year siege that ended the dynasty.", prompt);
        Assert.Contains("The chronicler is unreliable past year 40.", prompt);
        Assert.Contains("Rising tension between the coastal factions.", prompt);
    }

    // =========================================================================
    // BuildEditSystemPrompt — voice preservation
    // =========================================================================

    [Fact]
    public void BuildEditSystemPrompt_MentionsVoicePreservation()
    {
        var prompt = EraNarrativePrompts.BuildEditSystemPrompt("witty", craftPosture: null);

        Assert.Contains("voice", prompt, StringComparison.OrdinalIgnoreCase);
        // The edit should not change the draft — clean it
        Assert.Contains("clean", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildEditSystemPrompt_MentionsLengthPreservation()
    {
        var prompt = EraNarrativePrompts.BuildEditSystemPrompt("tender", craftPosture: null);

        Assert.Contains("5,000", prompt);
        Assert.Contains("shorten", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildEditSystemPrompt_ContainsToneInformation()
    {
        var prompt = EraNarrativePrompts.BuildEditSystemPrompt("enthusiastic", craftPosture: null);

        // Enthusiastic tone should appear in the system prompt so the editor knows the voice
        Assert.Contains("thrilled", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildEditSystemPrompt_WithCraftPosture_IncludesPostureConstraints()
    {
        const string posture = "Avoid passive constructions at all costs.";
        var prompt = EraNarrativePrompts.BuildEditSystemPrompt("sardonic", craftPosture: posture);

        Assert.Contains(posture, prompt);
    }

    [Fact]
    public void BuildEditSystemPrompt_ContainsEditingGuidance()
    {
        var prompt = EraNarrativePrompts.BuildEditSystemPrompt("hopeful", craftPosture: null);

        Assert.Contains("Register breaks", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Redundancy", prompt, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // BuildEditUserPrompt — wraps narrative
    // =========================================================================

    [Fact]
    public void BuildEditUserPrompt_ContainsNarrative()
    {
        const string narrative = "In the age before the wars, the river peoples built their towers...";

        var prompt = EraNarrativePrompts.BuildEditUserPrompt(narrative);

        Assert.Contains(narrative, prompt);
    }

    [Fact]
    public void BuildEditUserPrompt_ContainsTaskInstruction()
    {
        var prompt = EraNarrativePrompts.BuildEditUserPrompt("Some narrative text.");

        Assert.Contains("Copy-edit", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("voice is correct", prompt, StringComparison.OrdinalIgnoreCase);
    }
}
