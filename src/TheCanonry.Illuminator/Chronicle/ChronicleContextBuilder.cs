using TheCanonry.Schema.World;

namespace TheCanonry.Illuminator.Chronicle;

/// <summary>
/// Entity context snapshot for chronicle generation prompts.
/// </summary>
public sealed record EntityContext
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Kind { get; init; }
    public string? Subtype { get; init; }
    public required string Prominence { get; init; }
    public string? Culture { get; init; }
    public required string Status { get; init; }
    public string? Summary { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Relationship context for chronicle generation prompts.
/// </summary>
public sealed record RelationshipContext
{
    public required string SourceId { get; init; }
    public required string TargetId { get; init; }
    public required string Kind { get; init; }
    public required string SourceName { get; init; }
    public required string TargetName { get; init; }
}

/// <summary>
/// Assembled generation context for a chronicle.
/// Groups entity snapshots, relationships, events, and world data
/// needed to build chronicle generation prompts.
/// </summary>
public sealed record ChronicleGenerationContext
{
    public required string WorldName { get; init; }
    public required string WorldDescription { get; init; }
    public required string Tone { get; init; }
    public required IReadOnlyList<string> CanonFacts { get; init; }
    public required IReadOnlyList<EntityContext> Entities { get; init; }
    public required IReadOnlyList<RelationshipContext> Relationships { get; init; }
    public string? FocalEraName { get; init; }
    public string? NarrativeDirection { get; init; }
}

/// <summary>
/// Builds ChronicleGenerationContext from world state and entity data.
/// </summary>
public static class ChronicleContextBuilder
{
    /// <summary>
    /// Build entity context snapshot from a world entity.
    /// </summary>
    public static EntityContext FromEntity(Entity entity) => new()
    {
        Id = entity.Id.Value,
        Name = entity.Name,
        Kind = entity.Kind.Value,
        Subtype = entity.Subtype,
        Prominence = entity.Prominence.ToString(),
        Culture = entity.Culture.Value,
        Status = entity.Status.Value,
        Summary = entity.Summary,
        Description = entity.Description,
    };
}
