namespace TheCanonry.Illuminator.Tests.Operations;

using TheCanonry.Illuminator.Operations;
using TheCanonry.Persistence.Entities;

// ─── Helpers ──────────────────────────────────────────────────────────────────

file static class RenameFactory
{
    public static PersistedEntity MakeEntity(
        string id,
        string name,
        string description = "",
        string summary = "") => new()
    {
        Id              = id,
        SimulationRunId = "sim-1",
        Kind            = "character",
        Subtype         = "npc",
        Name            = name,
        Description     = description,
        Summary         = summary,
        Status          = "active",
        Prominence      = 0.5,
        Culture         = "default",
        EraId           = "era-1",
        CoordX          = 0,
        CoordY          = 0,
        CoordZ          = 0,
        CreatedAtTick   = 1,
        UpdatedAtTick   = 1,
    };

    public static Chronicle MakeChronicle(
        long id,
        string content,
        string? title = null,
        string? summary = null) => new()
    {
        Id              = id,
        SimulationRunId = "sim-1",
        EntityIds       = "[]",
        Format          = "narrative",
        Content         = content,
        Title           = title,
        Summary         = summary,
    };

    public static NarrativeEventEntity MakeEvent(
        long id,
        string description,
        string action = "",
        string subjectId = "subj-1") => new()
    {
        Id              = id,
        SimulationRunId = "sim-1",
        Tick            = 1,
        EraId           = "era-1",
        EventKind       = "entity_created",
        Significance    = 0.5,
        SubjectId       = subjectId,
        Action          = action,
        Description     = description,
    };

    public static StaticPageEntity MakePage(
        string pageId,
        string title,
        string content) => new()
    {
        PageId          = pageId,
        ProjectId       = "proj-1",
        SimulationRunId = "sim-1",
        Title           = title,
        Slug            = pageId,
        Content         = content,
        Status          = "draft",
    };
}

// ─── ScanReferences Tests ─────────────────────────────────────────────────────

public sealed class EntityRenameScanTests
{
    // -------------------------------------------------------------------------
    // Test 1: Finds entity name in entity descriptions
    // -------------------------------------------------------------------------

    [Fact]
    public void ScanReferences_FindsNameInEntityDescriptions()
    {
        var entities = new[]
        {
            RenameFactory.MakeEntity("e1", "Thornwood",
                description: "Thornwood was once a great keep."),
            RenameFactory.MakeEntity("e2", "Lake Sorren",
                description: "Near Thornwood lies a frozen lake."),
        };

        var result = EntityRenameService.ScanReferences(
            entityId: "e1",
            oldName:  "Thornwood",
            entities: entities,
            chronicles: []);

        // Should find match in e1.Name, e1.Description, and e2.Description
        Assert.True(result.Matches.Count >= 2);
        Assert.Contains(result.Matches, m =>
            m.Source == EntityRenameService.MatchSource.Entity &&
            m.Field  == "Description" &&
            m.SourceId == "e2");
    }

    // -------------------------------------------------------------------------
    // Test 2: Finds entity name in chronicle content
    // -------------------------------------------------------------------------

    [Fact]
    public void ScanReferences_FindsNameInChronicleContent()
    {
        var chronicles = new[]
        {
            RenameFactory.MakeChronicle(101,
                content: "The tale of Thornwood begins at the edge of the old forest.",
                title:   "Chronicle of the Keep"),
        };

        var result = EntityRenameService.ScanReferences(
            entityId:   "e1",
            oldName:    "Thornwood",
            entities:   [],
            chronicles: chronicles);

        var contentMatch = Assert.Single(result.Matches, m =>
            m.Source == EntityRenameService.MatchSource.Chronicle &&
            m.Field  == "Content");

        Assert.Equal("Thornwood", contentMatch.MatchedText);
        Assert.Equal(101.ToString(), contentMatch.SourceId);
    }

    // -------------------------------------------------------------------------
    // Test 3: Finds name in static page content
    // -------------------------------------------------------------------------

    [Fact]
    public void ScanReferences_FindsNameInStaticPageContent()
    {
        var pages = new[]
        {
            RenameFactory.MakePage("page-1", "History", "Thornwood was founded by settlers."),
        };

        var result = EntityRenameService.ScanReferences(
            entityId:     "e1",
            oldName:      "Thornwood",
            entities:     [],
            chronicles:   [],
            staticPages:  pages);

        Assert.Contains(result.Matches, m =>
            m.Source  == EntityRenameService.MatchSource.StaticPage &&
            m.Field   == "Content" &&
            m.SourceId == "page-1");
    }

    // -------------------------------------------------------------------------
    // Test 4: Context extraction provides ±60 chars
    // -------------------------------------------------------------------------

    [Fact]
    public void ScanReferences_ContextExtraction_ProvidesNearbyText()
    {
        const string prefix  = "Before text padding here and more words to pad out the context for test. ";
        const string name    = "Thornwood";
        const string suffix  = " After text padding here and more words to pad out the context for test.";
        var content = prefix + name + suffix;

        var pages = new[]
        {
            RenameFactory.MakePage("p1", "Wiki Page", content),
        };

        var result = EntityRenameService.ScanReferences(
            entityId:    "e1",
            oldName:     "Thornwood",
            entities:    [],
            chronicles:  [],
            staticPages: pages);

        var match = Assert.Single(result.Matches, m => m.Field == "Content");

        Assert.True(match.ContextBefore.Length > 0, "Expected non-empty context before");
        Assert.True(match.ContextAfter.Length  > 0, "Expected non-empty context after");
        Assert.True(match.ContextBefore.Length <= 60, "Context before should be ≤ 60 chars");
        Assert.True(match.ContextAfter.Length  <= 60, "Context after should be ≤ 60 chars");
    }

    // -------------------------------------------------------------------------
    // Test 5: Case-insensitive matching
    // -------------------------------------------------------------------------

    [Fact]
    public void ScanReferences_CaseInsensitive_FindsLowercaseOccurrence()
    {
        var chronicles = new[]
        {
            RenameFactory.MakeChronicle(200, content: "The thornwood keep was ancient."),
        };

        var result = EntityRenameService.ScanReferences(
            entityId:   "e1",
            oldName:    "Thornwood",
            entities:   [],
            chronicles: chronicles);

        Assert.Single(result.Matches, m =>
            m.Source == EntityRenameService.MatchSource.Chronicle &&
            m.MatchedText == "thornwood");
    }

    // -------------------------------------------------------------------------
    // Test 6: Finds name in narrative event description
    // -------------------------------------------------------------------------

    [Fact]
    public void ScanReferences_FindsNameInNarrativeEventDescription()
    {
        var events = new[]
        {
            RenameFactory.MakeEvent(301,
                description: "Thornwood was sacked during the third era.",
                action:      "sack"),
        };

        var result = EntityRenameService.ScanReferences(
            entityId:         "e1",
            oldName:          "Thornwood",
            entities:         [],
            chronicles:       [],
            narrativeEvents:  events);

        Assert.Contains(result.Matches, m =>
            m.Source == EntityRenameService.MatchSource.NarrativeEvent &&
            m.Field  == "Description");
    }

    // -------------------------------------------------------------------------
    // Test 7: Empty name returns no matches
    // -------------------------------------------------------------------------

    [Fact]
    public void ScanReferences_EmptyName_ReturnsNoMatches()
    {
        var entities = new[]
        {
            RenameFactory.MakeEntity("e1", "Thornwood", description: "Something here."),
        };

        var result = EntityRenameService.ScanReferences(
            entityId:   "e1",
            oldName:    "",
            entities:   entities,
            chronicles: []);

        Assert.Empty(result.Matches);
    }
}

// ─── BuildPatches Tests ───────────────────────────────────────────────────────

public sealed class EntityRenamePatchBuildTests
{
    // -------------------------------------------------------------------------
    // Test 8: BuildPatches creates correct replacement — mixed case uses replacement as-is
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildPatches_MixedCase_UsesReplacementAsIs()
    {
        var scanResult = new EntityRenameService.RenameScanResult(
            EntityId: "e1",
            OldName:  "Thornwood",
            Matches:
            [
                new EntityRenameService.RenameMatch(
                    SourceId:      "c1",
                    SourceName:    "Chronicle 1",
                    Source:        EntityRenameService.MatchSource.Chronicle,
                    Field:         "Content",
                    Type:          EntityRenameService.MatchType.FullName,
                    MatchedText:   "Thornwood",
                    Position:      10,
                    ContextBefore: "The great ",
                    ContextAfter:  " keep"),
            ]);

        var patches = EntityRenameService.BuildPatches(scanResult, "Ironvale");

        var patch = Assert.Single(patches);
        Assert.Equal("Ironvale", patch.Replacement);
        Assert.Equal(10, patch.Position);
        Assert.Equal("Thornwood".Length, patch.OriginalLength);
    }

    // -------------------------------------------------------------------------
    // Test 9: BuildPatches preserves all-uppercase casing
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildPatches_AllUppercase_UppercasesReplacement()
    {
        var scanResult = new EntityRenameService.RenameScanResult(
            EntityId: "e1",
            OldName:  "THORNWOOD",
            Matches:
            [
                new EntityRenameService.RenameMatch(
                    SourceId:      "c1",
                    SourceName:    "Chronicle 1",
                    Source:        EntityRenameService.MatchSource.Chronicle,
                    Field:         "Content",
                    Type:          EntityRenameService.MatchType.FullName,
                    MatchedText:   "THORNWOOD",
                    Position:      0,
                    ContextBefore: "",
                    ContextAfter:  " ruled all"),
            ]);

        var patches = EntityRenameService.BuildPatches(scanResult, "Ironvale");

        Assert.Equal("IRONVALE", Assert.Single(patches).Replacement);
    }

    // -------------------------------------------------------------------------
    // Test 10: BuildPatches preserves all-lowercase casing
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildPatches_AllLowercase_LowercasesReplacement()
    {
        var scanResult = new EntityRenameService.RenameScanResult(
            EntityId: "e1",
            OldName:  "thornwood",
            Matches:
            [
                new EntityRenameService.RenameMatch(
                    SourceId:      "c1",
                    SourceName:    "Chronicle 1",
                    Source:        EntityRenameService.MatchSource.Chronicle,
                    Field:         "Content",
                    Type:          EntityRenameService.MatchType.FullName,
                    MatchedText:   "thornwood",
                    Position:      5,
                    ContextBefore: "the  ",
                    ContextAfter:  " keep"),
            ]);

        var patches = EntityRenameService.BuildPatches(scanResult, "Ironvale");

        Assert.Equal("ironvale", Assert.Single(patches).Replacement);
    }
}

// ─── ApplyPatches Tests ───────────────────────────────────────────────────────

public sealed class EntityRenameApplyPatchesTests
{
    // -------------------------------------------------------------------------
    // Test 11: Applies single patch correctly
    // -------------------------------------------------------------------------

    [Fact]
    public void ApplyPatches_SinglePatch_ReplacesCorrectly()
    {
        const string text = "The Thornwood keep was ancient.";
        var patches = new[]
        {
            new EntityRenameService.RenamePatch(
                SourceId:       "c1",
                Source:         EntityRenameService.MatchSource.Chronicle,
                Field:          "Content",
                Position:       4,
                OriginalLength: "Thornwood".Length,
                Replacement:    "Ironvale"),
        };

        var result = EntityRenameService.ApplyPatches(text, patches);

        Assert.Equal("The Ironvale keep was ancient.", result);
    }

    // -------------------------------------------------------------------------
    // Test 12: Applies multiple patches in reverse position order
    // -------------------------------------------------------------------------

    [Fact]
    public void ApplyPatches_MultiplePatches_AppliedInReverseOrder()
    {
        const string text = "Thornwood stood near Thornwood Lake.";
        // Two occurrences of "Thornwood" at positions 0 and 21
        var patches = new[]
        {
            new EntityRenameService.RenamePatch(
                SourceId:       "c1",
                Source:         EntityRenameService.MatchSource.Chronicle,
                Field:          "Content",
                Position:       0,
                OriginalLength: 9,
                Replacement:    "Ironvale"),
            new EntityRenameService.RenamePatch(
                SourceId:       "c1",
                Source:         EntityRenameService.MatchSource.Chronicle,
                Field:          "Content",
                Position:       21,
                OriginalLength: 9,
                Replacement:    "Ironvale"),
        };

        var result = EntityRenameService.ApplyPatches(text, patches);

        Assert.Equal("Ironvale stood near Ironvale Lake.", result);
    }

    // -------------------------------------------------------------------------
    // Test 13: Empty patches returns original text
    // -------------------------------------------------------------------------

    [Fact]
    public void ApplyPatches_EmptyPatches_ReturnsOriginalText()
    {
        const string text = "Nothing changes here.";
        var result = EntityRenameService.ApplyPatches(text, []);
        Assert.Equal(text, result);
    }
}
