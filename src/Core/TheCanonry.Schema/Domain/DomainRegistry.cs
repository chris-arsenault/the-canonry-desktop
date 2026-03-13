using TheCanonry.Schema.Config;
using TheCanonry.Schema.Primitives;

namespace TheCanonry.Schema.Domain;

public class DomainRegistry
{
    private readonly HashSet<EntityKind> _validEntityKinds = [];
    private readonly HashSet<RelationshipKind> _validRelationshipKinds = [];
    private readonly Dictionary<EntityKind, HashSet<EntityStatus>> _validStatuses = [];

    public DomainRegistry(DomainSchema schema)
    {
        // Register framework primitives
        foreach (var kind in FrameworkPrimitives.EntityKinds.All)
            _validEntityKinds.Add(kind);

        foreach (var kind in FrameworkPrimitives.RelationshipKinds.All)
            _validRelationshipKinds.Add(kind);

        // Framework entity kinds get framework statuses
        foreach (var fwKind in FrameworkPrimitives.EntityKinds.All)
        {
            _validStatuses[fwKind] = [..FrameworkPrimitives.Statuses.All];
        }

        // Register domain-defined values from schema
        foreach (var kindDef in schema.EntityKinds)
        {
            _validEntityKinds.Add(kindDef.Kind);

            var statuses = new HashSet<EntityStatus>();
            foreach (var status in kindDef.Statuses)
                statuses.Add(new EntityStatus(status.Id));
            // Also include framework statuses for domain kinds
            foreach (var status in FrameworkPrimitives.Statuses.All)
                statuses.Add(status);
            _validStatuses[kindDef.Kind] = statuses;
        }

        foreach (var relDef in schema.RelationshipKinds)
            _validRelationshipKinds.Add(relDef.Kind);
    }

    public void ValidateEntityKind(EntityKind kind)
    {
        if (!_validEntityKinds.Contains(kind))
            throw new InvalidDomainValueException($"Unknown entity kind: {kind}");
    }

    public void ValidateRelationshipKind(RelationshipKind kind)
    {
        if (!_validRelationshipKinds.Contains(kind))
            throw new InvalidDomainValueException($"Unknown relationship kind: {kind}");
    }

    public void ValidateStatus(EntityKind kind, EntityStatus status)
    {
        if (!_validStatuses.TryGetValue(kind, out var valid) || !valid.Contains(status))
            throw new InvalidDomainValueException(
                $"Status '{status}' is not valid for entity kind '{kind}'");
    }

    public IReadOnlySet<EntityKind> EntityKinds => _validEntityKinds;
    public IReadOnlySet<RelationshipKind> RelationshipKinds => _validRelationshipKinds;
}
