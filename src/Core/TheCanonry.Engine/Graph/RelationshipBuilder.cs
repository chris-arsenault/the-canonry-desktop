using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;
using ExecutionContext = TheCanonry.Schema.World.ExecutionContext;

namespace TheCanonry.Engine.Graph;

/// <summary>
/// Fluent builder for constructing batches of relationships.
/// Reduces boilerplate in templates and systems.
/// </summary>
public sealed class RelationshipBuilder
{
    private readonly List<Relationship> _relationships = [];
    private readonly int _tick;
    private readonly ExecutionContext _context;

    public RelationshipBuilder(int tick, ExecutionContext context)
    {
        _tick = tick;
        _context = context;
    }

    /// <summary>Add a directed relationship.</summary>
    public RelationshipBuilder Add(RelationshipKind kind, EntityId src, EntityId dst, double strength = 0.5)
    {
        _relationships.Add(new Relationship(src, dst, kind, strength, 0.0, "", _context, _tick));
        return this;
    }

    /// <summary>Add two relationships forming a bidirectional link.</summary>
    public RelationshipBuilder AddBidirectional(RelationshipKind kind, EntityId entity1, EntityId entity2, double strength = 0.5)
    {
        Add(kind, entity1, entity2, strength);
        Add(kind, entity2, entity1, strength);
        return this;
    }

    /// <summary>Return all built relationships and clear the internal list.</summary>
    public IReadOnlyList<Relationship> Build()
    {
        var result = new List<Relationship>(_relationships);
        _relationships.Clear();
        return result;
    }

    /// <summary>Number of relationships built so far.</summary>
    public int Count => _relationships.Count;
}
