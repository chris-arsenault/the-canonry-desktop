using TheCanonry.Illuminator.PrePrint;

namespace TheCanonry.Illuminator.Tests.PrePrint;

public sealed class ContentTreeTests
{
    // =========================================================================
    // CreateScaffold
    // =========================================================================

    [Fact]
    public void CreateScaffold_ReturnsThreeTopLevelFolders()
    {
        var tree = ContentTree.CreateScaffold();

        Assert.Equal(3, tree.Count);
        Assert.All(tree, n => Assert.Equal(ContentNodeType.Folder, n.Type));
        Assert.Equal("Front Matter", tree[0].Name);
        Assert.Equal("Body",         tree[1].Name);
        Assert.Equal("Back Matter",  tree[2].Name);
    }

    [Fact]
    public void CreateScaffold_FrontMatterHasThreeSubFolders()
    {
        var tree = ContentTree.CreateScaffold();
        var frontMatter = tree[0];

        Assert.NotNull(frontMatter.Children);
        Assert.Equal(3, frontMatter.Children!.Count);
    }

    [Fact]
    public void CreateScaffold_BackMatterHasFourSubFolders()
    {
        var tree = ContentTree.CreateScaffold();
        var backMatter = tree[2];

        Assert.NotNull(backMatter.Children);
        Assert.Equal(4, backMatter.Children!.Count);
    }

    // =========================================================================
    // FindNode
    // =========================================================================

    [Fact]
    public void FindNode_FindsNestedNode()
    {
        var tree = ContentTree.CreateScaffold();

        // The "Title Page" folder is nested inside "Front Matter"
        var frontMatter = tree[0];
        var titlePageId = frontMatter.Children![0].Id;

        var found = ContentTree.FindNode(tree, titlePageId);

        Assert.NotNull(found);
        Assert.Equal("Title Page", found!.Name);
    }

    [Fact]
    public void FindNode_ReturnsNullForMissingId()
    {
        var tree = ContentTree.CreateScaffold();
        var found = ContentTree.FindNode(tree, "nonexistent-id");
        Assert.Null(found);
    }

    // =========================================================================
    // RenameNode
    // =========================================================================

    [Fact]
    public void RenameNode_ChangesName()
    {
        var tree = ContentTree.CreateScaffold();
        var bodyId = tree[1].Id;

        var result = ContentTree.RenameNode(tree, bodyId, "Main Content");

        Assert.True(result);
        Assert.Equal("Main Content", tree[1].Name);
    }

    [Fact]
    public void RenameNode_ReturnsFalseForMissingId()
    {
        var tree = ContentTree.CreateScaffold();
        var result = ContentTree.RenameNode(tree, "nonexistent", "New Name");
        Assert.False(result);
    }

    // =========================================================================
    // DeleteNode
    // =========================================================================

    [Fact]
    public void DeleteNode_RemovesNodeAndChildren()
    {
        var tree = ContentTree.CreateScaffold();
        var frontMatter = tree[0];
        var titlePageId = frontMatter.Children![0].Id;

        // Confirm it exists
        Assert.NotNull(ContentTree.FindNode(tree, titlePageId));

        var result = ContentTree.DeleteNode(tree, titlePageId);

        Assert.True(result);
        Assert.Null(ContentTree.FindNode(tree, titlePageId));
        Assert.Equal(2, frontMatter.Children!.Count);
    }

    [Fact]
    public void DeleteNode_ReturnsFalseForMissingId()
    {
        var tree = ContentTree.CreateScaffold();
        var result = ContentTree.DeleteNode(tree, "nonexistent");
        Assert.False(result);
        Assert.Equal(3, tree.Count); // unchanged
    }

    // =========================================================================
    // AddContentItem
    // =========================================================================

    [Fact]
    public void AddContentItem_CreatesChildUnderParent()
    {
        var tree = ContentTree.CreateScaffold();
        var bodyId = tree[1].Id;

        var newNode = ContentTree.AddContentItem(tree, bodyId, "The Chronicle of Aelthar", ContentNodeType.Chronicle, "chr-001");

        Assert.NotNull(newNode);
        Assert.Equal("The Chronicle of Aelthar", newNode.Name);
        Assert.Equal(ContentNodeType.Chronicle, newNode.Type);
        Assert.Equal("chr-001", newNode.ContentId);

        // Verify it appears in the tree
        var found = ContentTree.FindNode(tree, newNode.Id);
        Assert.NotNull(found);
    }

    // =========================================================================
    // MoveNode
    // =========================================================================

    [Fact]
    public void MoveNode_RelocatesToDifferentParent()
    {
        var tree = ContentTree.CreateScaffold();

        // Add a node to Front Matter
        var frontMatterId = tree[0].Id;
        var bodyId = tree[1].Id;
        var added = ContentTree.AddContentItem(tree, frontMatterId, "Intro Page", ContentNodeType.StaticPage, "page-001");

        // Move it to Body
        var result = ContentTree.MoveNode(tree, added.Id, bodyId, 0);

        Assert.True(result);
        // Should not be in Front Matter any more (its direct children are the 3 sub-folders + nothing else)
        Assert.DoesNotContain(tree[0].Children!, n => n.Id == added.Id);
        // Should be in Body
        Assert.Contains(tree[1].Children!, n => n.Id == added.Id);
    }

    [Fact]
    public void MoveNode_ReturnsFalseForMissingNode()
    {
        var tree = ContentTree.CreateScaffold();
        var bodyId = tree[1].Id;

        var result = ContentTree.MoveNode(tree, "nonexistent", bodyId, 0);

        Assert.False(result);
    }

    // =========================================================================
    // FlattenForExport
    // =========================================================================

    [Fact]
    public void FlattenForExport_ProducesCorrectPaths()
    {
        var tree = ContentTree.CreateScaffold();
        var flattened = ContentTree.FlattenForExport(tree);

        // Top-level nodes should have single-segment paths
        var frontMatterEntry = flattened.First(n => n.Name == "Front Matter");
        Assert.Equal("01-front-matter", frontMatterEntry.Path);
        Assert.Equal(0, frontMatterEntry.Depth);

        var bodyEntry = flattened.First(n => n.Name == "Body");
        Assert.Equal("02-body", bodyEntry.Path);

        var backMatterEntry = flattened.First(n => n.Name == "Back Matter");
        Assert.Equal("03-back-matter", backMatterEntry.Path);
    }

    [Fact]
    public void FlattenForExport_NestedNodesHaveCompoundPaths()
    {
        var tree = ContentTree.CreateScaffold();
        var flattened = ContentTree.FlattenForExport(tree);

        // "Title Page" is the first child of "Front Matter"
        var titlePage = flattened.First(n => n.Name == "Title Page");
        Assert.Equal("01-front-matter/01-title-page", titlePage.Path);
        Assert.Equal(1, titlePage.Depth);
    }

    [Fact]
    public void FlattenForExport_SlugifiesSpecialCharacters()
    {
        var tree = new List<ContentTreeNode>
        {
            new ContentTreeNode
            {
                Id = "root",
                Name = "Table of Contents",
                Type = ContentNodeType.Folder,
                Children = [],
            },
        };
        var flattened = ContentTree.FlattenForExport(tree);
        Assert.Equal("01-table-of-contents", flattened[0].Path);
    }

    // =========================================================================
    // AutoPopulateBody
    // =========================================================================

    [Fact]
    public void AutoPopulateBody_CreatesKindBasedFolders()
    {
        var tree = ContentTree.CreateScaffold();

        var entities = new List<(string Id, string Name, string Kind)>
        {
            ("e1", "Aldric",       "npc"),
            ("e2", "Mira",         "npc"),
            ("e3", "Iron Citadel", "place"),
        };

        ContentTree.AutoPopulateBody(tree, entities, [], [], []);

        var body = tree.First(n => n.Name == "Body");
        Assert.NotNull(body.Children);
        Assert.Equal(2, body.Children!.Count); // "Npcs" and "Places" folders

        var npcFolder = body.Children.First(n => n.Name == "Npcs");
        Assert.Equal(2, npcFolder.Children!.Count);

        var placeFolder = body.Children.First(n => n.Name == "Places");
        Assert.Single(placeFolder.Children!);
    }

    [Fact]
    public void AutoPopulateBody_AddsChroniclesEraNarrativesAndStaticPages()
    {
        var tree = ContentTree.CreateScaffold();

        var chronicles = new List<(string Id, string Title)>
        {
            ("chr-1", "The First Age"),
        };
        var eraNarratives = new List<(string Id, string EraName)>
        {
            ("era-1", "The Dawn Era"),
        };
        var staticPages = new List<(string Id, string Title)>
        {
            ("page-1", "World Overview"),
        };

        ContentTree.AutoPopulateBody(tree, [], chronicles, eraNarratives, staticPages);

        var body = tree.First(n => n.Name == "Body");
        Assert.Contains(body.Children!, n => n.Type == ContentNodeType.Chronicle && n.ContentId == "chr-1");
        Assert.Contains(body.Children!, n => n.Type == ContentNodeType.EraNarrative && n.ContentId == "era-1");
        Assert.Contains(body.Children!, n => n.Type == ContentNodeType.StaticPage && n.ContentId == "page-1");
    }
}
