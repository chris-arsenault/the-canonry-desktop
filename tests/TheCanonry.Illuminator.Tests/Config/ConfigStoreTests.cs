using Microsoft.EntityFrameworkCore;
using TheCanonry.Illuminator.Chronicle.PerspectiveSynthesis;
using TheCanonry.Illuminator.Config;
using TheCanonry.Persistence;

namespace TheCanonry.Illuminator.Tests.Config;

public sealed class ConfigStoreTests : IDisposable
{
    private readonly CanonryDbContext _db;

    public ConfigStoreTests()
    {
        var options = new DbContextOptionsBuilder<CanonryDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _db = new CanonryDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task WorldContext_round_trip_preserves_all_fields()
    {
        var store = new ConfigStore(_db);
        var context = new WorldContext
        {
            Name = "Aelindra",
            Description = "A world of ancient magic",
            ToneCore = "grim-epic",
            CanonFacts =
            [
                new CanonFact("f-1", "Magic is rare and dangerous", "world_truth", Required: true, Disabled: false),
                new CanonFact("f-2", "The old empire fell three centuries ago", "world_truth", Required: false, Disabled: false),
            ],
            SpeciesConstraint = "humans only",
            WorldDynamics =
            [
                new WorldDynamic { Id = "wd-1", Text = "Rising tension between city-states", Cultures = ["northern", "southern"] },
                new WorldDynamic { Id = "wd-2", Text = "Trade routes shifting eastward", Kinds = ["city", "guild"] },
            ],
        };

        await store.SaveWorldContextAsync("proj-1", context);
        var loaded = await store.LoadWorldContextAsync("proj-1");

        Assert.NotNull(loaded);
        Assert.Equal("Aelindra", loaded.Name);
        Assert.Equal("A world of ancient magic", loaded.Description);
        Assert.Equal("grim-epic", loaded.ToneCore);
        Assert.Equal(2, loaded.CanonFacts.Count);
        Assert.Equal("f-1", loaded.CanonFacts[0].Id);
        Assert.True(loaded.CanonFacts[0].Required);
        Assert.Equal("humans only", loaded.SpeciesConstraint);
        Assert.NotNull(loaded.WorldDynamics);
        Assert.Equal(2, loaded.WorldDynamics.Count);
        Assert.Equal("wd-1", loaded.WorldDynamics[0].Id);
        Assert.Equal("Rising tension between city-states", loaded.WorldDynamics[0].Text);
        Assert.Equal(2, loaded.WorldDynamics[0].Cultures!.Count);
        Assert.Equal("wd-2", loaded.WorldDynamics[1].Id);
        Assert.Equal(2, loaded.WorldDynamics[1].Kinds!.Count);
    }

    [Fact]
    public async Task EntityGuidance_round_trip_preserves_all_fields()
    {
        var store = new ConfigStore(_db);
        var guidance = new EntityGuidance
        {
            ByKind = new Dictionary<string, KindGuidance>
            {
                ["person"] = new KindGuidance(
                    DomainInstructions: "Focus on cultural heritage",
                    VisualAvoid: "modern clothing",
                    ProseHints: "Use formal speech patterns",
                    TraitGuidance: "Emphasize honour and loyalty"),
                ["city"] = new KindGuidance(
                    DomainInstructions: "Describe architecture",
                    VisualAvoid: null,
                    ProseHints: null,
                    TraitGuidance: null),
            },
        };

        await store.SaveEntityGuidanceAsync("proj-1", guidance);
        var loaded = await store.LoadEntityGuidanceAsync("proj-1");

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.ByKind.Count);
        Assert.Equal("Focus on cultural heritage", loaded.ByKind["person"].DomainInstructions);
        Assert.Equal("modern clothing", loaded.ByKind["person"].VisualAvoid);
        Assert.Null(loaded.ByKind["city"].ProseHints);
    }

    [Fact]
    public async Task CultureIdentityMap_round_trip_preserves_all_fields()
    {
        var store = new ConfigStore(_db);
        var identities = new CultureIdentityMap
        {
            ByCulture = new Dictionary<string, Dictionary<string, string>>
            {
                ["northern"] = new Dictionary<string, string>
                {
                    ["naming_style"] = "Norse-inspired",
                    ["visual_motif"] = "ice and stone",
                    ["dominant_trait"] = "stoic endurance",
                },
                ["southern"] = new Dictionary<string, string>
                {
                    ["naming_style"] = "Mediterranean-inspired",
                    ["visual_motif"] = "sun and marble",
                },
            },
        };

        await store.SaveCultureIdentitiesAsync("proj-1", identities);
        var loaded = await store.LoadCultureIdentitiesAsync("proj-1");

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.ByCulture.Count);
        Assert.Equal("Norse-inspired", loaded.ByCulture["northern"]["naming_style"]);
        Assert.Equal("ice and stone", loaded.ByCulture["northern"]["visual_motif"]);
        Assert.Equal("stoic endurance", loaded.ByCulture["northern"]["dominant_trait"]);
        Assert.Equal(2, loaded.ByCulture["southern"].Count);
    }

    [Fact]
    public async Task LlmCallConfigMap_round_trip_preserves_all_fields()
    {
        var store = new ConfigStore(_db);
        var config = new LlmCallConfigMap
        {
            Overrides = new Dictionary<string, LlmCallOverride>
            {
                ["description"] = new LlmCallOverride(
                    Model: "claude-opus-4-5",
                    MaxTokens: 2048,
                    Temperature: 0.7,
                    ThinkingBudget: null),
                ["historian"] = new LlmCallOverride(
                    Model: null,
                    MaxTokens: 8192,
                    Temperature: null,
                    ThinkingBudget: 10000),
            },
        };

        await store.SaveLlmCallConfigAsync("proj-1", config);
        var loaded = await store.LoadLlmCallConfigAsync("proj-1");

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Overrides.Count);
        Assert.Equal("claude-opus-4-5", loaded.Overrides["description"].Model);
        Assert.Equal(2048, loaded.Overrides["description"].MaxTokens);
        Assert.Equal(0.7, loaded.Overrides["description"].Temperature);
        Assert.Null(loaded.Overrides["description"].ThinkingBudget);
        Assert.Null(loaded.Overrides["historian"].Model);
        Assert.Equal(10000, loaded.Overrides["historian"].ThinkingBudget);
    }

    [Fact]
    public async Task ImageGenerationConfig_round_trip_preserves_all_fields()
    {
        var store = new ConfigStore(_db);
        var config = new ImageGenerationConfig
        {
            Model = "fal-ai/flux-pro/v1.1-ultra",
            Width = 1280,
            Height = 960,
            AspectRatio = "4:3",
            NumInferenceSteps = 35,
            GuidanceScale = 4.0,
            SafetyTolerance = false,
        };

        await store.SaveImageConfigAsync("proj-1", config);
        var loaded = await store.LoadImageConfigAsync("proj-1");

        Assert.NotNull(loaded);
        Assert.Equal("fal-ai/flux-pro/v1.1-ultra", loaded.Model);
        Assert.Equal(1280, loaded.Width);
        Assert.Equal(960, loaded.Height);
        Assert.Equal("4:3", loaded.AspectRatio);
        Assert.Equal(35, loaded.NumInferenceSteps);
        Assert.Equal(4.0, loaded.GuidanceScale);
        Assert.False(loaded.SafetyTolerance);
    }

    [Fact]
    public async Task Load_returns_null_when_no_config_exists()
    {
        var store = new ConfigStore(_db);

        var worldContext = await store.LoadWorldContextAsync("proj-nonexistent");
        var entityGuidance = await store.LoadEntityGuidanceAsync("proj-nonexistent");
        var cultures = await store.LoadCultureIdentitiesAsync("proj-nonexistent");
        var llmConfig = await store.LoadLlmCallConfigAsync("proj-nonexistent");
        var imageConfig = await store.LoadImageConfigAsync("proj-nonexistent");

        Assert.Null(worldContext);
        Assert.Null(entityGuidance);
        Assert.Null(cultures);
        Assert.Null(llmConfig);
        Assert.Null(imageConfig);
    }

    [Fact]
    public async Task Save_overwrites_existing_config_without_duplicating_rows()
    {
        var store = new ConfigStore(_db);
        var original = new WorldContext
        {
            Name = "Original",
            Description = "First version",
            ToneCore = "light",
            CanonFacts = [],
        };
        var updated = new WorldContext
        {
            Name = "Updated",
            Description = "Second version",
            ToneCore = "dark",
            CanonFacts = [new CanonFact("f-1", "Updated fact", "world_truth", false, false)],
        };

        await store.SaveWorldContextAsync("proj-1", original);
        await store.SaveWorldContextAsync("proj-1", updated);

        var loaded = await store.LoadWorldContextAsync("proj-1");
        Assert.NotNull(loaded);
        Assert.Equal("Updated", loaded.Name);
        Assert.Single(loaded.CanonFacts);

        // Only one row should exist
        var rowCount = await _db.ConfigSlots.CountAsync(s => s.ProjectId == "proj-1");
        Assert.Equal(1, rowCount);
    }
}
