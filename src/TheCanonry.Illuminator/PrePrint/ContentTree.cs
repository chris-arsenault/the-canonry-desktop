using System.Text.RegularExpressions;

namespace TheCanonry.Illuminator.PrePrint;

/// <summary>
/// Pure functions for manipulating the content ordering tree.
///
/// Ported from apps/illuminator/webui/src/lib/preprint/contentTree.ts.
/// </summary>
public static class ContentTree
{
    private static int _nextId = 1;

    private static string GenerateId() =>
        $"node_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{_nextId++}";

    // =========================================================================
    // Scaffold
    // =========================================================================

    /// <summary>
    /// Creates the default scaffold: Front Matter, Body, and Back Matter folders,
    /// each pre-populated with standard sub-folders.
    /// </summary>
    public static List<ContentTreeNode> CreateScaffold() =>
    [
        new ContentTreeNode
        {
            Id = GenerateId(),
            Name = "Front Matter",
            Type = ContentNodeType.Folder,
            Children =
            [
                new ContentTreeNode { Id = GenerateId(), Name = "Title Page",          Type = ContentNodeType.Folder, Children = [] },
                new ContentTreeNode { Id = GenerateId(), Name = "Copyright",           Type = ContentNodeType.Folder, Children = [] },
                new ContentTreeNode { Id = GenerateId(), Name = "Table of Contents",   Type = ContentNodeType.Folder, Children = [] },
            ],
        },
        new ContentTreeNode
        {
            Id = GenerateId(),
            Name = "Body",
            Type = ContentNodeType.Folder,
            Children = [],
        },
        new ContentTreeNode
        {
            Id = GenerateId(),
            Name = "Back Matter",
            Type = ContentNodeType.Folder,
            Children =
            [
                new ContentTreeNode { Id = GenerateId(), Name = "Appendix",  Type = ContentNodeType.Folder, Children = [] },
                new ContentTreeNode { Id = GenerateId(), Name = "Glossary",  Type = ContentNodeType.Folder, Children = [] },
                new ContentTreeNode { Id = GenerateId(), Name = "Index",     Type = ContentNodeType.Folder, Children = [] },
                new ContentTreeNode { Id = GenerateId(), Name = "Colophon",  Type = ContentNodeType.Folder, Children = [] },
            ],
        },
    ];

    // =========================================================================
    // Tree Traversal
    // =========================================================================

    /// <summary>
    /// Depth-first search for a node by ID.
    /// </summary>
    public static ContentTreeNode? FindNode(IReadOnlyList<ContentTreeNode> tree, string id)
    {
        foreach (var node in tree)
        {
            if (node.Id == id) return node;
            if (node.Children is not null)
            {
                var found = FindNode(node.Children, id);
                if (found is not null) return found;
            }
        }
        return null;
    }

    // =========================================================================
    // Mutations
    // =========================================================================

    /// <summary>
    /// Renames the node with the given ID. Returns true if found.
    /// </summary>
    public static bool RenameNode(IReadOnlyList<ContentTreeNode> tree, string id, string newName)
    {
        foreach (var node in tree)
        {
            if (node.Id == id)
            {
                node.Name = newName;
                return true;
            }
            if (node.Children is not null && RenameNode(node.Children, id, newName))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Deletes the node (and all its children) with the given ID. Returns true if found.
    /// </summary>
    public static bool DeleteNode(List<ContentTreeNode> tree, string id)
    {
        for (var i = 0; i < tree.Count; i++)
        {
            if (tree[i].Id == id)
            {
                tree.RemoveAt(i);
                return true;
            }
            if (tree[i].Children is not null && DeleteNode(tree[i].Children!, id))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Moves a node to a new parent at a specific index.
    /// Returns true if the node was found and moved.
    /// </summary>
    public static bool MoveNode(List<ContentTreeNode> tree, string nodeId, string targetParentId, int index)
    {
        var node = FindNode(tree, nodeId);
        if (node is null) return false;

        DeleteNode(tree, nodeId);

        return InsertIntoParent(tree, targetParentId, node, index);
    }

    private static bool InsertIntoParent(List<ContentTreeNode> tree, string parentId, ContentTreeNode item, int index)
    {
        foreach (var node in tree)
        {
            if (node.Id == parentId && node.Children is not null)
            {
                var clampedIndex = Math.Clamp(index, 0, node.Children.Count);
                node.Children.Insert(clampedIndex, item);
                return true;
            }
            if (node.Children is not null && InsertIntoParent(node.Children, parentId, item, index))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Adds a content item (entity, chronicle, etc.) under the given parent folder.
    /// Returns the new node.
    /// </summary>
    public static ContentTreeNode AddContentItem(
        List<ContentTreeNode> tree,
        string parentId,
        string name,
        ContentNodeType type,
        string contentId)
    {
        var newNode = new ContentTreeNode
        {
            Id = GenerateId(),
            Name = name,
            Type = type,
            ContentId = contentId,
        };

        AppendToParent(tree, parentId, newNode);
        return newNode;
    }

    private static bool AppendToParent(List<ContentTreeNode> tree, string parentId, ContentTreeNode item)
    {
        foreach (var node in tree)
        {
            if (node.Id == parentId)
            {
                node.Children ??= [];
                node.Children.Add(item);
                return true;
            }
            if (node.Children is not null && AppendToParent(node.Children, parentId, item))
                return true;
        }
        return false;
    }

    // =========================================================================
    // Export Flatten
    // =========================================================================

    /// <summary>
    /// Depth-first walk that produces a flat list with slugified paths.
    /// Each node's path segment is zero-padded index + slugified name, e.g. "01-front-matter/01-title-page".
    /// </summary>
    public static IReadOnlyList<FlattenedNode> FlattenForExport(IReadOnlyList<ContentTreeNode> tree)
    {
        var result = new List<FlattenedNode>();
        Walk(tree, "", 0, result);
        return result;
    }

    private static void Walk(IReadOnlyList<ContentTreeNode> nodes, string parentPath, int depth, List<FlattenedNode> result)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            var prefix = (i + 1).ToString().PadLeft(2, '0');
            var segment = $"{prefix}-{Slugify(node.Name)}";
            var path = parentPath.Length > 0 ? $"{parentPath}/{segment}" : segment;
            result.Add(new FlattenedNode(node.Id, node.Name, node.Type, node.ContentId, path, depth));
            if (node.Children is not null)
                Walk(node.Children, path, depth + 1, result);
        }
    }

    private static string Slugify(string name)
    {
        var lower = name.ToLowerInvariant();
        var slug = Regex.Replace(lower, @"[^a-z0-9]+", "-");
        return slug.Trim('-');
    }

    // =========================================================================
    // Auto-Populate
    // =========================================================================

    /// <summary>
    /// Populates the Body folder with entities grouped by kind, chronicles, era narratives,
    /// and static pages. Mirrors the simplified form of autoPopulateBody from contentTree.ts,
    /// adapted to the C# signature that receives plain value tuples instead of rich objects.
    /// </summary>
    public static void AutoPopulateBody(
        List<ContentTreeNode> tree,
        IReadOnlyList<(string Id, string Name, string Kind)> entities,
        IReadOnlyList<(string Id, string Title)> chronicles,
        IReadOnlyList<(string Id, string EraName)> eraNarratives,
        IReadOnlyList<(string Id, string Title)> staticPages)
    {
        var bodyNode = tree.FirstOrDefault(n => n.Name == "Body" && n.Type == ContentNodeType.Folder);
        if (bodyNode is null) return;

        bodyNode.Children ??= [];

        // Entity sub-folders grouped by kind
        var byKind = new Dictionary<string, List<(string Id, string Name)>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, name, kind) in entities)
        {
            if (!byKind.TryGetValue(kind, out var list))
            {
                list = [];
                byKind[kind] = list;
            }
            list.Add((id, name));
        }

        foreach (var kind in byKind.Keys.OrderBy(k => k))
        {
            var folderName = char.ToUpperInvariant(kind[0]) + kind[1..] + "s";
            var kindFolder = new ContentTreeNode
            {
                Id = GenerateId(),
                Name = folderName,
                Type = ContentNodeType.Folder,
                Children = byKind[kind]
                    .OrderBy(e => e.Name)
                    .Select(e => new ContentTreeNode
                    {
                        Id = GenerateId(),
                        Name = e.Name,
                        Type = ContentNodeType.Entity,
                        ContentId = e.Id,
                    })
                    .ToList(),
            };
            bodyNode.Children.Add(kindFolder);
        }

        // Chronicles
        foreach (var (id, title) in chronicles)
        {
            bodyNode.Children.Add(new ContentTreeNode
            {
                Id = GenerateId(),
                Name = string.IsNullOrEmpty(title) ? "Untitled Chronicle" : title,
                Type = ContentNodeType.Chronicle,
                ContentId = id,
            });
        }

        // Era narratives
        foreach (var (id, eraName) in eraNarratives)
        {
            bodyNode.Children.Add(new ContentTreeNode
            {
                Id = GenerateId(),
                Name = $"{eraName} — Era Narrative",
                Type = ContentNodeType.EraNarrative,
                ContentId = id,
            });
        }

        // Static pages
        foreach (var (id, title) in staticPages)
        {
            bodyNode.Children.Add(new ContentTreeNode
            {
                Id = GenerateId(),
                Name = string.IsNullOrEmpty(title) ? "Untitled Page" : title,
                Type = ContentNodeType.StaticPage,
                ContentId = id,
            });
        }
    }
}
