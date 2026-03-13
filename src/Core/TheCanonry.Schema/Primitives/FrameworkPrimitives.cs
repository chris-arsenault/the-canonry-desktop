using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;

namespace TheCanonry.Schema.Primitives;

public static class FrameworkPrimitives
{
    public static class EntityKinds
    {
        public static readonly EntityKind Era = new("era");
        public static readonly EntityKind Occurrence = new("occurrence");
        public static readonly IReadOnlySet<EntityKind> All = new HashSet<EntityKind> { Era, Occurrence };
    }

    public static class RelationshipKinds
    {
        public static readonly RelationshipKind Supersedes = new("supersedes");
        public static readonly RelationshipKind PartOf = new("part_of");
        public static readonly RelationshipKind ActiveDuring = new("active_during");
        public static readonly RelationshipKind ParticipantIn = new("participant_in");
        public static readonly RelationshipKind EpicenterOf = new("epicenter_of");
        public static readonly RelationshipKind TriggeredBy = new("triggered_by");
        public static readonly RelationshipKind CreatedDuring = new("created_during");
        public static readonly IReadOnlySet<RelationshipKind> All = new HashSet<RelationshipKind>
        {
            Supersedes, PartOf, ActiveDuring, ParticipantIn, EpicenterOf, TriggeredBy, CreatedDuring
        };
    }

    public static class Statuses
    {
        public static readonly EntityStatus Active = new("active");
        public static readonly EntityStatus Historical = new("historical");
        public static readonly EntityStatus Current = new("current");
        public static readonly EntityStatus Future = new("future");
        public static readonly EntityStatus Subsumed = new("subsumed");
        public static readonly IReadOnlySet<EntityStatus> All = new HashSet<EntityStatus>
        {
            Active, Historical, Current, Future, Subsumed
        };
    }

    public static class Subtypes
    {
        public static readonly string Region = "region";
    }

    public static class Cultures
    {
        public static readonly CultureId World = new("world");
    }

    public static class Tags
    {
        public const string MetaEntity = "meta-entity";
        public const string Temporal = "temporal";
        public const string Era = "era";
        public const string EraId = "eraId";
        public const string ProminenceLocked = "prominence_locked";
    }

    private static readonly Dictionary<RelationshipKind, double> DefaultStrengths = new()
    {
        [RelationshipKinds.Supersedes] = 0.7,
        [RelationshipKinds.PartOf] = 0.5,
        [RelationshipKinds.ActiveDuring] = 0.3,
        [RelationshipKinds.ParticipantIn] = 1.0,
        [RelationshipKinds.EpicenterOf] = 1.0,
        [RelationshipKinds.TriggeredBy] = 0.8,
        [RelationshipKinds.CreatedDuring] = 0.5,
    };

    public static double GetDefaultRelationshipStrength(RelationshipKind kind)
    {
        return DefaultStrengths.TryGetValue(kind, out var strength)
            ? strength
            : throw new ArgumentException($"No default strength for relationship kind: {kind}");
    }

    public static bool IsFrameworkEntityKind(EntityKind kind) => EntityKinds.All.Contains(kind);
    public static bool IsFrameworkRelationshipKind(RelationshipKind kind) => RelationshipKinds.All.Contains(kind);
    public static bool IsFrameworkStatus(EntityStatus status) => Statuses.All.Contains(status);
}
