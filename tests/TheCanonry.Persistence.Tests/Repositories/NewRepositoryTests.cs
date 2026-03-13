using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence.Entities;
using TheCanonry.Persistence.Repositories;

namespace TheCanonry.Persistence.Tests.Repositories;

public sealed class NewRepositoryTests : IDisposable
{
    private readonly CanonryDbContext _db;

    public NewRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CanonryDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _db = new CanonryDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
    }

    // =====================================================================
    // EntityRepository
    // =====================================================================

    private static PersistedEntity MakeEntity(string id, string runId, string kind = "person", string name = "Test Entity") =>
        new()
        {
            Id = id,
            SimulationRunId = runId,
            Kind = kind,
            Subtype = "noble",
            Name = name,
            Description = "A description",
            Summary = "A summary",
            Status = "active",
            Prominence = 0.5,
            Culture = "western",
            EraId = "era-1",
            CoordX = 0.0,
            CoordY = 0.0,
            CoordZ = 0.0,
            CreatedAtTick = 1,
            UpdatedAtTick = 1,
        };

    [Fact]
    public async Task Entity_Upsert_creates_new_entity()
    {
        var repo = new EntityRepository(_db);
        var entity = MakeEntity("e-1", "run-1");

        var result = await repo.Upsert(entity);

        var loaded = await repo.GetById("e-1", "run-1");
        Assert.NotNull(loaded);
        Assert.Equal("Test Entity", loaded.Name);
        Assert.Equal("person", loaded.Kind);
    }

    [Fact]
    public async Task Entity_Upsert_updates_existing_entity()
    {
        var repo = new EntityRepository(_db);
        var entity = MakeEntity("e-1", "run-1");
        await repo.Upsert(entity);

        entity.Name = "Updated Name";
        entity.Status = "historical";
        await repo.Upsert(entity);

        var loaded = await repo.GetById("e-1", "run-1");
        Assert.NotNull(loaded);
        Assert.Equal("Updated Name", loaded.Name);
        Assert.Equal("historical", loaded.Status);
    }

    [Fact]
    public async Task Entity_GetBySimulation_returns_only_matching_run()
    {
        var repo = new EntityRepository(_db);
        await repo.Upsert(MakeEntity("e-1", "run-1", "person", "Alice"));
        await repo.Upsert(MakeEntity("e-2", "run-1", "city", "Rome"));
        await repo.Upsert(MakeEntity("e-3", "run-2", "person", "Bob"));

        var results = await repo.GetBySimulation("run-1");
        Assert.Equal(2, results.Count);
        Assert.All(results, e => Assert.Equal("run-1", e.SimulationRunId));
    }

    [Fact]
    public async Task Entity_GetByKind_filters_correctly()
    {
        var repo = new EntityRepository(_db);
        await repo.Upsert(MakeEntity("e-1", "run-1", "person", "Alice"));
        await repo.Upsert(MakeEntity("e-2", "run-1", "city", "Rome"));
        await repo.Upsert(MakeEntity("e-3", "run-1", "person", "Bob"));

        var persons = await repo.GetByKind("run-1", "person");
        Assert.Equal(2, persons.Count);
        Assert.All(persons, e => Assert.Equal("person", e.Kind));
    }

    [Fact]
    public async Task Entity_Search_matches_name_and_description()
    {
        var repo = new EntityRepository(_db);
        var e1 = MakeEntity("e-1", "run-1");
        e1.Name = "dragon queen";
        e1.Description = "A powerful ruler";
        await repo.Upsert(e1);

        var e2 = MakeEntity("e-2", "run-1");
        e2.Name = "Old Wizard";
        e2.Description = "Wise dragon tamer";
        await repo.Upsert(e2);

        var e3 = MakeEntity("e-3", "run-1");
        e3.Name = "Farmer";
        e3.Description = "Lives on a farm";
        await repo.Upsert(e3);

        var results = await repo.Search("run-1", "dragon");
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task Entity_Delete_removes_by_composite_key()
    {
        var repo = new EntityRepository(_db);
        await repo.Upsert(MakeEntity("e-1", "run-1"));
        await repo.Upsert(MakeEntity("e-1", "run-2")); // same id, different run

        await repo.Delete("e-1", "run-1");

        Assert.Null(await repo.GetById("e-1", "run-1"));
        Assert.NotNull(await repo.GetById("e-1", "run-2")); // other run untouched
    }

    // =====================================================================
    // RelationshipRepository
    // =====================================================================

    private static PersistedRelationship MakeRel(string runId, string sourceId, string targetId, string kind = "allied_with") =>
        new()
        {
            SimulationRunId = runId,
            SourceId = sourceId,
            TargetId = targetId,
            Kind = kind,
            Strength = 0.8,
            Distance = 1.0,
            Category = "social",
            Status = "active",
            CreatedAtTick = 1,
        };

    [Fact]
    public async Task Relationship_Create_and_GetBySimulation()
    {
        var repo = new RelationshipRepository(_db);

        await repo.Create(MakeRel("run-1", "e-1", "e-2"));
        await repo.Create(MakeRel("run-1", "e-2", "e-3"));
        await repo.Create(MakeRel("run-2", "e-1", "e-2"));

        var results = await repo.GetBySimulation("run-1");
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("run-1", r.SimulationRunId));
    }

    [Fact]
    public async Task Relationship_GetByEntity_returns_source_and_target_matches()
    {
        var repo = new RelationshipRepository(_db);

        await repo.Create(MakeRel("run-1", "e-1", "e-2")); // e-1 as source
        await repo.Create(MakeRel("run-1", "e-3", "e-1")); // e-1 as target
        await repo.Create(MakeRel("run-1", "e-2", "e-3")); // unrelated

        var results = await repo.GetByEntity("run-1", "e-1");
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task Relationship_Delete_removes_relationship()
    {
        var repo = new RelationshipRepository(_db);

        var rel = await repo.Create(MakeRel("run-1", "e-1", "e-2"));
        Assert.Single(await repo.GetBySimulation("run-1"));

        await repo.Delete(rel.Id);
        Assert.Empty(await repo.GetBySimulation("run-1"));
    }

    // =====================================================================
    // SimulationSlotRepository
    // =====================================================================

    private static SimulationSlotEntity MakeSlot(string projectId, int slotIndex, string runId) =>
        new()
        {
            ProjectId = projectId,
            SlotIndex = slotIndex,
            SimulationRunId = runId,
            UpdatedAt = DateTime.UtcNow,
        };

    [Fact]
    public async Task SimulationSlot_Upsert_creates_new_slot()
    {
        var repo = new SimulationSlotRepository(_db);
        var slot = MakeSlot("proj-1", 0, "run-abc");

        await repo.Upsert(slot);

        var loaded = await repo.GetByIndex("proj-1", 0);
        Assert.NotNull(loaded);
        Assert.Equal("run-abc", loaded.SimulationRunId);
    }

    [Fact]
    public async Task SimulationSlot_Upsert_updates_existing_slot()
    {
        var repo = new SimulationSlotRepository(_db);
        await repo.Upsert(MakeSlot("proj-1", 0, "run-abc"));

        var updated = MakeSlot("proj-1", 0, "run-xyz");
        updated.Label = "New World";
        await repo.Upsert(updated);

        var loaded = await repo.GetByIndex("proj-1", 0);
        Assert.NotNull(loaded);
        Assert.Equal("run-xyz", loaded.SimulationRunId);
        Assert.Equal("New World", loaded.Label);

        // Ensure only one row exists
        var all = await repo.GetByProject("proj-1");
        Assert.Single(all);
    }

    [Fact]
    public async Task SimulationSlot_GetByProject_returns_all_slots_ordered()
    {
        var repo = new SimulationSlotRepository(_db);
        await repo.Upsert(MakeSlot("proj-1", 2, "run-c"));
        await repo.Upsert(MakeSlot("proj-1", 0, "run-a"));
        await repo.Upsert(MakeSlot("proj-1", 1, "run-b"));
        await repo.Upsert(MakeSlot("proj-2", 0, "run-d"));

        var slots = await repo.GetByProject("proj-1");
        Assert.Equal(3, slots.Count);
        Assert.Equal(0, slots[0].SlotIndex);
        Assert.Equal(1, slots[1].SlotIndex);
        Assert.Equal(2, slots[2].SlotIndex);
    }

    [Fact]
    public async Task SimulationSlot_Delete_removes_slot()
    {
        var repo = new SimulationSlotRepository(_db);
        var slot = await repo.Upsert(MakeSlot("proj-1", 0, "run-abc"));

        await repo.Delete(slot.Id);

        Assert.Null(await repo.GetByIndex("proj-1", 0));
    }

    // =====================================================================
    // HistorianRunRepository
    // =====================================================================

    [Fact]
    public async Task HistorianRun_Create_and_GetBySimulation()
    {
        var repo = new HistorianRunRepository(_db);

        await repo.Create(new HistorianRun
        {
            SimulationRunId = "run-1",
            EntityId = "e-1",
            Notes = "[]",
            Status = "completed",
        });
        await repo.Create(new HistorianRun
        {
            SimulationRunId = "run-1",
            EntityId = "e-2",
            Notes = "[]",
            Status = "pending",
        });
        await repo.Create(new HistorianRun
        {
            SimulationRunId = "run-2",
            EntityId = "e-1",
            Notes = "[]",
            Status = "completed",
        });

        var results = await repo.GetBySimulation("run-1");
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task HistorianRun_GetByEntity_filters_correctly()
    {
        var repo = new HistorianRunRepository(_db);

        await repo.Create(new HistorianRun
        {
            SimulationRunId = "run-1",
            EntityId = "e-1",
            Notes = "[]",
            Status = "completed",
        });
        await repo.Create(new HistorianRun
        {
            SimulationRunId = "run-1",
            EntityId = "e-2",
            Notes = "[]",
            Status = "pending",
        });

        var results = await repo.GetByEntity("run-1", "e-1");
        Assert.Single(results);
        Assert.Equal("e-1", results[0].EntityId);
    }

    [Fact]
    public async Task HistorianRun_Update_and_Delete()
    {
        var repo = new HistorianRunRepository(_db);

        var run = await repo.Create(new HistorianRun
        {
            SimulationRunId = "run-1",
            EntityId = "e-1",
            Notes = "[]",
            Status = "pending",
        });

        run.Status = "completed";
        run.Notes = "[{\"note\":\"test\"}]";
        await repo.Update(run);

        var loaded = await repo.GetBySimulation("run-1");
        Assert.Single(loaded);
        Assert.Equal("completed", loaded[0].Status);

        await repo.Delete(run.Id);
        Assert.Empty(await repo.GetBySimulation("run-1"));
    }

    // =====================================================================
    // EraNarrativeRepository
    // =====================================================================

    [Fact]
    public async Task EraNarrative_Create_and_GetBySimulation()
    {
        var repo = new EraNarrativeRepository(_db);

        await repo.Create(new EraNarrative
        {
            SimulationRunId = "run-1",
            EraId = "era-1",
            Content = "The first era content",
        });
        await repo.Create(new EraNarrative
        {
            SimulationRunId = "run-1",
            EraId = "era-2",
            Content = "The second era content",
        });
        await repo.Create(new EraNarrative
        {
            SimulationRunId = "run-2",
            EraId = "era-1",
            Content = "Different run era content",
        });

        var results = await repo.GetBySimulation("run-1");
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task EraNarrative_GetByEra_returns_single_narrative()
    {
        var repo = new EraNarrativeRepository(_db);

        await repo.Create(new EraNarrative
        {
            SimulationRunId = "run-1",
            EraId = "era-1",
            Content = "Era one narrative",
        });
        await repo.Create(new EraNarrative
        {
            SimulationRunId = "run-1",
            EraId = "era-2",
            Content = "Era two narrative",
        });

        var result = await repo.GetByEra("run-1", "era-1");
        Assert.NotNull(result);
        Assert.Equal("Era one narrative", result.Content);
    }

    [Fact]
    public async Task EraNarrative_Update_and_Delete()
    {
        var repo = new EraNarrativeRepository(_db);

        var narrative = await repo.Create(new EraNarrative
        {
            SimulationRunId = "run-1",
            EraId = "era-1",
            Content = "Original content",
        });

        narrative.Content = "Revised content";
        narrative.Summary = "Brief summary";
        await repo.Update(narrative);

        var loaded = await repo.GetByEra("run-1", "era-1");
        Assert.NotNull(loaded);
        Assert.Equal("Revised content", loaded.Content);
        Assert.Equal("Brief summary", loaded.Summary);

        await repo.Delete(narrative.Id);
        Assert.Null(await repo.GetByEra("run-1", "era-1"));
    }

    // =====================================================================
    // NarrativeEventRepository
    // =====================================================================

    [Fact]
    public async Task NarrativeEvent_Create_and_GetBySimulation()
    {
        var repo = new NarrativeEventRepository(_db);

        await repo.Create(new NarrativeEventEntity
        {
            SimulationRunId = "run-1",
            Tick = 5,
            EraId = "era-1",
            EventKind = "entity_created",
            Significance = 0.7,
            SubjectId = "e-1",
            Action = "was born",
            Description = "A hero was born",
        });
        await repo.Create(new NarrativeEventEntity
        {
            SimulationRunId = "run-1",
            Tick = 10,
            EraId = "era-1",
            EventKind = "relationship_formed",
            Significance = 0.5,
            SubjectId = "e-2",
            Action = "allied with",
            Description = "Two factions allied",
        });
        await repo.Create(new NarrativeEventEntity
        {
            SimulationRunId = "run-2",
            Tick = 3,
            EraId = "era-1",
            EventKind = "entity_created",
            Significance = 0.6,
            SubjectId = "e-1",
            Action = "founded",
            Description = "A city was founded",
        });

        var results = await repo.GetBySimulation("run-1");
        Assert.Equal(2, results.Count);
        // Should be ordered by tick
        Assert.Equal(5, results[0].Tick);
        Assert.Equal(10, results[1].Tick);
    }

    [Fact]
    public async Task NarrativeEvent_GetByEra_filters_correctly()
    {
        var repo = new NarrativeEventRepository(_db);

        await repo.Create(new NarrativeEventEntity
        {
            SimulationRunId = "run-1",
            Tick = 1,
            EraId = "era-1",
            EventKind = "entity_created",
            Significance = 0.5,
            SubjectId = "e-1",
            Action = "appeared",
            Description = "A hero appeared",
        });
        await repo.Create(new NarrativeEventEntity
        {
            SimulationRunId = "run-1",
            Tick = 20,
            EraId = "era-2",
            EventKind = "conflict",
            Significance = 0.9,
            SubjectId = "e-2",
            Action = "attacked",
            Description = "A war began",
        });

        var era1 = await repo.GetByEra("run-1", "era-1");
        Assert.Single(era1);
        Assert.Equal("era-1", era1[0].EraId);
    }

    [Fact]
    public async Task NarrativeEvent_GetByEntity_matches_SubjectId()
    {
        var repo = new NarrativeEventRepository(_db);

        await repo.Create(new NarrativeEventEntity
        {
            SimulationRunId = "run-1",
            Tick = 1,
            EraId = "era-1",
            EventKind = "entity_created",
            Significance = 0.5,
            SubjectId = "e-1",
            Action = "appeared",
            Description = "Hero appeared",
        });
        await repo.Create(new NarrativeEventEntity
        {
            SimulationRunId = "run-1",
            Tick = 2,
            EraId = "era-1",
            EventKind = "entity_created",
            Significance = 0.4,
            SubjectId = "e-2",
            Action = "appeared",
            Description = "Villain appeared",
        });

        var results = await repo.GetByEntity("run-1", "e-1");
        Assert.Single(results);
        Assert.Equal("e-1", results[0].SubjectId);
    }

    [Fact]
    public async Task NarrativeEvent_BulkCreate_inserts_all_in_single_save()
    {
        var repo = new NarrativeEventRepository(_db);

        var events = Enumerable.Range(1, 5).Select(i => new NarrativeEventEntity
        {
            SimulationRunId = "run-1",
            Tick = i,
            EraId = "era-1",
            EventKind = "tick_event",
            Significance = 0.3,
            SubjectId = $"e-{i}",
            Action = "happened",
            Description = $"Event {i}",
        }).ToList();

        await repo.BulkCreate(events);

        var results = await repo.GetBySimulation("run-1");
        Assert.Equal(5, results.Count);
    }

    // =====================================================================
    // TraitPaletteRepository
    // =====================================================================

    [Fact]
    public async Task TraitPalette_Upsert_creates_new_palette()
    {
        var repo = new TraitPaletteRepository(_db);
        var palette = new TraitPaletteEntity
        {
            ProjectId = "proj-1",
            EntityKind = "person",
            CategoriesJson = "[{\"name\":\"personality\"}]",
        };

        await repo.Upsert(palette);

        var loaded = await repo.GetByKind("proj-1", "person");
        Assert.NotNull(loaded);
        Assert.Contains("personality", loaded.CategoriesJson);
    }

    [Fact]
    public async Task TraitPalette_Upsert_updates_existing_palette()
    {
        var repo = new TraitPaletteRepository(_db);
        await repo.Upsert(new TraitPaletteEntity
        {
            ProjectId = "proj-1",
            EntityKind = "person",
            CategoriesJson = "[{\"name\":\"personality\"}]",
        });

        await repo.Upsert(new TraitPaletteEntity
        {
            ProjectId = "proj-1",
            EntityKind = "person",
            CategoriesJson = "[{\"name\":\"personality\"},{\"name\":\"skills\"}]",
        });

        var all = await repo.GetByProject("proj-1");
        Assert.Single(all); // Only one row per kind

        var loaded = await repo.GetByKind("proj-1", "person");
        Assert.NotNull(loaded);
        Assert.Contains("skills", loaded.CategoriesJson);
    }

    [Fact]
    public async Task TraitPalette_GetByProject_returns_all_kinds()
    {
        var repo = new TraitPaletteRepository(_db);
        await repo.Upsert(new TraitPaletteEntity { ProjectId = "proj-1", EntityKind = "person", CategoriesJson = "[]" });
        await repo.Upsert(new TraitPaletteEntity { ProjectId = "proj-1", EntityKind = "city", CategoriesJson = "[]" });
        await repo.Upsert(new TraitPaletteEntity { ProjectId = "proj-2", EntityKind = "person", CategoriesJson = "[]" });

        var results = await repo.GetByProject("proj-1");
        Assert.Equal(2, results.Count);
        Assert.All(results, p => Assert.Equal("proj-1", p.ProjectId));
    }

    // =====================================================================
    // StaticPageRepository
    // =====================================================================

    [Fact]
    public async Task StaticPage_Create_and_GetBySimulation()
    {
        var repo = new StaticPageRepository(_db);

        await repo.Create(new StaticPageEntity
        {
            PageId = "page-1",
            ProjectId = "proj-1",
            SimulationRunId = "run-1",
            Title = "The Great War",
            Slug = "the-great-war",
            Content = "Content here",
            Status = "published",
        });
        await repo.Create(new StaticPageEntity
        {
            PageId = "page-2",
            ProjectId = "proj-1",
            SimulationRunId = "run-1",
            Title = "Aelindra City",
            Slug = "aelindra-city",
            Content = "City content",
            Status = "draft",
        });
        await repo.Create(new StaticPageEntity
        {
            PageId = "page-3",
            ProjectId = "proj-1",
            SimulationRunId = "run-2",
            Title = "Other Run Page",
            Slug = "other",
            Content = "Other content",
            Status = "draft",
        });

        var results = await repo.GetBySimulation("run-1");
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task StaticPage_GetByPageId_returns_correct_page()
    {
        var repo = new StaticPageRepository(_db);

        await repo.Create(new StaticPageEntity
        {
            PageId = "page-abc",
            ProjectId = "proj-1",
            SimulationRunId = "run-1",
            Title = "Test Page",
            Slug = "test-page",
            Content = "Test content",
            Status = "draft",
        });

        var result = await repo.GetByPageId("page-abc");
        Assert.NotNull(result);
        Assert.Equal("Test Page", result.Title);
    }

    [Fact]
    public async Task StaticPage_Update_and_Delete()
    {
        var repo = new StaticPageRepository(_db);

        var page = await repo.Create(new StaticPageEntity
        {
            PageId = "page-1",
            ProjectId = "proj-1",
            SimulationRunId = "run-1",
            Title = "Draft Title",
            Slug = "draft",
            Content = "Draft content",
            Status = "draft",
        });

        page.Title = "Published Title";
        page.Status = "published";
        await repo.Update(page);

        var loaded = await repo.GetByPageId("page-1");
        Assert.NotNull(loaded);
        Assert.Equal("Published Title", loaded.Title);
        Assert.Equal("published", loaded.Status);

        await repo.Delete(page.Id);
        Assert.Null(await repo.GetByPageId("page-1"));
    }

    // =====================================================================
    // ContentTreeRepository
    // =====================================================================

    [Fact]
    public async Task ContentTree_Upsert_creates_new_tree()
    {
        var repo = new ContentTreeRepository(_db);
        var tree = new ContentTreeEntity
        {
            ProjectId = "proj-1",
            SimulationRunId = "run-1",
            TreeJson = "[{\"id\":\"node-1\"}]",
        };

        await repo.Upsert(tree);

        var loaded = await repo.Get("proj-1", "run-1");
        Assert.NotNull(loaded);
        Assert.Contains("node-1", loaded.TreeJson);
    }

    [Fact]
    public async Task ContentTree_Upsert_updates_existing_tree()
    {
        var repo = new ContentTreeRepository(_db);
        await repo.Upsert(new ContentTreeEntity
        {
            ProjectId = "proj-1",
            SimulationRunId = "run-1",
            TreeJson = "[{\"id\":\"node-1\"}]",
        });

        await repo.Upsert(new ContentTreeEntity
        {
            ProjectId = "proj-1",
            SimulationRunId = "run-1",
            TreeJson = "[{\"id\":\"node-1\"},{\"id\":\"node-2\"}]",
        });

        var loaded = await repo.Get("proj-1", "run-1");
        Assert.NotNull(loaded);
        Assert.Contains("node-2", loaded.TreeJson);
        Assert.Equal(1, await _db.ContentTrees.CountAsync());
    }

    [Fact]
    public async Task ContentTree_Get_returns_null_when_not_found()
    {
        var repo = new ContentTreeRepository(_db);

        var result = await repo.Get("proj-nonexistent", "run-nonexistent");
        Assert.Null(result);
    }

    // =====================================================================
    // PageLayoutRepository
    // =====================================================================

    [Fact]
    public async Task PageLayout_Upsert_creates_new_layout()
    {
        var repo = new PageLayoutRepository(_db);
        var layout = new PageLayoutEntity
        {
            SimulationRunId = "run-1",
            PageId = "page-1",
            LayoutMode = "two-column",
            AnnotationDisplay = "sidebar",
        };

        await repo.Upsert(layout);

        var loaded = await repo.Get("run-1", "page-1");
        Assert.NotNull(loaded);
        Assert.Equal("two-column", loaded.LayoutMode);
    }

    [Fact]
    public async Task PageLayout_Upsert_updates_existing_layout()
    {
        var repo = new PageLayoutRepository(_db);
        await repo.Upsert(new PageLayoutEntity
        {
            SimulationRunId = "run-1",
            PageId = "page-1",
            LayoutMode = "default",
        });

        await repo.Upsert(new PageLayoutEntity
        {
            SimulationRunId = "run-1",
            PageId = "page-1",
            LayoutMode = "full-width",
            ContentWidth = "wide",
        });

        var loaded = await repo.Get("run-1", "page-1");
        Assert.NotNull(loaded);
        Assert.Equal("full-width", loaded.LayoutMode);
        Assert.Equal(1, await _db.PageLayouts.CountAsync());
    }

    [Fact]
    public async Task PageLayout_GetBySimulation_returns_all_layouts_for_run()
    {
        var repo = new PageLayoutRepository(_db);
        await repo.Upsert(new PageLayoutEntity { SimulationRunId = "run-1", PageId = "page-1", LayoutMode = "default" });
        await repo.Upsert(new PageLayoutEntity { SimulationRunId = "run-1", PageId = "page-2", LayoutMode = "default" });
        await repo.Upsert(new PageLayoutEntity { SimulationRunId = "run-2", PageId = "page-1", LayoutMode = "default" });

        var results = await repo.GetBySimulation("run-1");
        Assert.Equal(2, results.Count);
        Assert.All(results, l => Assert.Equal("run-1", l.SimulationRunId));
    }

    // =====================================================================
    // StyleLibraryRepository
    // =====================================================================

    [Fact]
    public async Task StyleLibrary_Upsert_creates_new_library()
    {
        var repo = new StyleLibraryRepository(_db);
        var library = new StyleLibraryEntity
        {
            ProjectId = "proj-1",
            ArtisticStylesJson = "[{\"id\":\"oil-painting\"}]",
            CompositionStylesJson = "[]",
            ColorPalettesJson = "[]",
        };

        await repo.Upsert(library);

        var loaded = await repo.GetByProject("proj-1");
        Assert.NotNull(loaded);
        Assert.Contains("oil-painting", loaded.ArtisticStylesJson);
    }

    [Fact]
    public async Task StyleLibrary_Upsert_updates_existing_library()
    {
        var repo = new StyleLibraryRepository(_db);
        await repo.Upsert(new StyleLibraryEntity
        {
            ProjectId = "proj-1",
            ArtisticStylesJson = "[]",
            CompositionStylesJson = "[]",
            ColorPalettesJson = "[]",
        });

        await repo.Upsert(new StyleLibraryEntity
        {
            ProjectId = "proj-1",
            ArtisticStylesJson = "[{\"id\":\"watercolor\"}]",
            CompositionStylesJson = "[{\"id\":\"rule-of-thirds\"}]",
            ColorPalettesJson = "[]",
        });

        var loaded = await repo.GetByProject("proj-1");
        Assert.NotNull(loaded);
        Assert.Contains("watercolor", loaded.ArtisticStylesJson);
        Assert.Equal(1, await _db.StyleLibraries.CountAsync());
    }

    [Fact]
    public async Task StyleLibrary_GetByProject_returns_null_when_not_found()
    {
        var repo = new StyleLibraryRepository(_db);

        var result = await repo.GetByProject("proj-nonexistent");
        Assert.Null(result);
    }

    // =====================================================================
    // WorldSchemaRepository
    // =====================================================================

    [Fact]
    public async Task WorldSchema_Upsert_creates_new_schema()
    {
        var repo = new WorldSchemaRepository(_db);
        var schema = new WorldSchemaEntity
        {
            ProjectId = "proj-1",
            SchemaJson = "{\"entityKinds\":[\"person\",\"city\"]}",
        };

        await repo.Upsert(schema);

        var loaded = await repo.GetByProject("proj-1");
        Assert.NotNull(loaded);
        Assert.Contains("person", loaded.SchemaJson);
    }

    [Fact]
    public async Task WorldSchema_Upsert_updates_existing_schema()
    {
        var repo = new WorldSchemaRepository(_db);
        await repo.Upsert(new WorldSchemaEntity
        {
            ProjectId = "proj-1",
            SchemaJson = "{\"entityKinds\":[\"person\"]}",
        });

        await repo.Upsert(new WorldSchemaEntity
        {
            ProjectId = "proj-1",
            SchemaJson = "{\"entityKinds\":[\"person\",\"dragon\"]}",
        });

        var loaded = await repo.GetByProject("proj-1");
        Assert.NotNull(loaded);
        Assert.Contains("dragon", loaded.SchemaJson);
        Assert.Equal(1, await _db.WorldSchemas.CountAsync());
    }

    [Fact]
    public async Task WorldSchema_GetByProject_returns_null_when_not_found()
    {
        var repo = new WorldSchemaRepository(_db);

        var result = await repo.GetByProject("proj-nonexistent");
        Assert.Null(result);
    }

    // =====================================================================
    // DynamicsRunRepository
    // =====================================================================

    [Fact]
    public async Task DynamicsRun_Create_and_GetBySimulation()
    {
        var repo = new DynamicsRunRepository(_db);

        await repo.Create(new DynamicsRunEntity
        {
            RunId = "dr-1",
            SimulationRunId = "run-1",
            Status = "pending",
        });
        await repo.Create(new DynamicsRunEntity
        {
            RunId = "dr-2",
            SimulationRunId = "run-1",
            Status = "completed",
        });
        await repo.Create(new DynamicsRunEntity
        {
            RunId = "dr-3",
            SimulationRunId = "run-2",
            Status = "pending",
        });

        var results = await repo.GetBySimulation("run-1");
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("run-1", r.SimulationRunId));
    }

    [Fact]
    public async Task DynamicsRun_Update_changes_status()
    {
        var repo = new DynamicsRunRepository(_db);

        var run = await repo.Create(new DynamicsRunEntity
        {
            RunId = "dr-1",
            SimulationRunId = "run-1",
            Status = "running",
        });

        run.Status = "completed";
        run.ResultJson = "{\"result\":\"success\"}";
        await repo.Update(run);

        var loaded = (await repo.GetBySimulation("run-1")).First();
        Assert.Equal("completed", loaded.Status);
        Assert.NotNull(loaded.ResultJson);
    }

    [Fact]
    public async Task DynamicsRun_Delete_removes_run()
    {
        var repo = new DynamicsRunRepository(_db);

        var run = await repo.Create(new DynamicsRunEntity
        {
            RunId = "dr-1",
            SimulationRunId = "run-1",
            Status = "pending",
        });

        await repo.Delete(run.Id);

        Assert.Empty(await repo.GetBySimulation("run-1"));
    }

    // =====================================================================
    // SummaryRevisionRunRepository
    // =====================================================================

    [Fact]
    public async Task SummaryRevisionRun_Create_and_GetBySimulation()
    {
        var repo = new SummaryRevisionRunRepository(_db);

        await repo.Create(new SummaryRevisionRunEntity
        {
            RunId = "sr-1",
            SimulationRunId = "run-1",
            Status = "pending",
            BatchesJson = "[]",
        });
        await repo.Create(new SummaryRevisionRunEntity
        {
            RunId = "sr-2",
            SimulationRunId = "run-1",
            Status = "completed",
            BatchesJson = "[{\"entityId\":\"e-1\"}]",
        });
        await repo.Create(new SummaryRevisionRunEntity
        {
            RunId = "sr-3",
            SimulationRunId = "run-2",
            Status = "pending",
            BatchesJson = "[]",
        });

        var results = await repo.GetBySimulation("run-1");
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("run-1", r.SimulationRunId));
    }

    [Fact]
    public async Task SummaryRevisionRun_Update_changes_status_and_batches()
    {
        var repo = new SummaryRevisionRunRepository(_db);

        var run = await repo.Create(new SummaryRevisionRunEntity
        {
            RunId = "sr-1",
            SimulationRunId = "run-1",
            Status = "pending",
            BatchesJson = "[]",
        });

        run.Status = "completed";
        run.BatchesJson = "[{\"entityId\":\"e-1\",\"patch\":{}}]";
        await repo.Update(run);

        var loaded = (await repo.GetBySimulation("run-1")).First();
        Assert.Equal("completed", loaded.Status);
        Assert.Contains("e-1", loaded.BatchesJson);
    }

    [Fact]
    public async Task SummaryRevisionRun_Delete_removes_run()
    {
        var repo = new SummaryRevisionRunRepository(_db);

        var run = await repo.Create(new SummaryRevisionRunEntity
        {
            RunId = "sr-1",
            SimulationRunId = "run-1",
            Status = "pending",
            BatchesJson = "[]",
        });

        await repo.Delete(run.Id);

        Assert.Empty(await repo.GetBySimulation("run-1"));
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _db.Dispose();
    }
}
