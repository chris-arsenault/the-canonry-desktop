namespace TheCanonry.Illuminator.BulkOps;

// =============================================================================
// Input types — lightweight projections the caller builds from their data layer
// =============================================================================

/// <summary>
/// Chronicle input for interleaved annotation work list construction.
/// Callers project from their full chronicle records into this shape.
/// </summary>
public sealed record InterleavedChronicleInput
{
    public required string ChronicleId { get; init; }
    public required string Title { get; init; }
    public required string Status { get; init; }
    public required int HistorianNoteCount { get; init; }
    public string? AssignedTone { get; init; }
    public int? EraYear { get; init; }
    public int? FocalEraOrder { get; init; }
    public string? FocalEraName { get; init; }

    /// <summary>
    /// Role assignments from the chronicle, used to schedule entity work items.
    /// Primary roles are scheduled before supporting roles.
    /// </summary>
    public IReadOnlyList<InterleavedRoleAssignment>? RoleAssignments { get; init; }
}

/// <summary>
/// Minimal role assignment projection for interleaved scheduling.
/// </summary>
public sealed record InterleavedRoleAssignment(string EntityId, bool IsPrimary);

/// <summary>
/// Entity input for interleaved annotation work list construction.
/// </summary>
public sealed record InterleavedEntityInput
{
    public required string EntityId { get; init; }
    public required string Name { get; init; }
    public required string Kind { get; init; }
    public required bool HasDescription { get; init; }
    public required bool HasHistorianNotes { get; init; }
}

// =============================================================================
// Output work item
// =============================================================================

/// <summary>
/// A single work item in the interleaved annotation queue.
/// Either a chronicle or an entity, with the tone to use for annotation.
/// </summary>
public abstract record InterleavedWorkItem(string Tone)
{
    public sealed record Chronicle(
        string ChronicleId, string Title, string Tone) : InterleavedWorkItem(Tone);

    public sealed record Entity(
        string EntityId, string EntityName, string EntityKind, string Tone) : InterleavedWorkItem(Tone);
}

// =============================================================================
// Builder
// =============================================================================

/// <summary>
/// Builds the interleaved annotation work list: chronicles in chronological order,
/// each followed by its referenced entities that haven't been annotated yet.
/// Orphan entities (with descriptions but not referenced by any chronicle) are
/// appended at the end with cycling tones.
///
/// Ported from apps/illuminator/webui/src/lib/db/interleavedAnnotationStore.ts
/// </summary>
public static class InterleavedAnnotationBuilder
{
    /// <summary>
    /// Tones cycled for orphan entities (entities not referenced by any chronicle).
    /// </summary>
    private static readonly string[] OrphanToneCycle =
        ["witty", "weary", "forensic", "elegiac", "cantankerous"];

    /// <summary>
    /// Build the ordered work list for interleaved annotation.
    /// Chronicles are sorted chronologically; after each chronicle, its cast
    /// entities (primary first) are scheduled if they have descriptions and
    /// haven't been annotated. Orphan entities are appended at the end.
    /// </summary>
    public static IReadOnlyList<InterleavedWorkItem> BuildWorkList(
        IReadOnlyList<InterleavedChronicleInput> chronicles,
        IReadOnlyDictionary<string, InterleavedEntityInput> entities)
    {
        var workItems = new List<InterleavedWorkItem>();

        // Filter: only complete chronicles with no historian notes
        var eligible = chronicles
            .Where(c => string.Equals(c.Status, "complete", StringComparison.OrdinalIgnoreCase)
                     && c.HistorianNoteCount == 0)
            .ToList();

        // Sort chronologically: year-0 (mythology) pushed to end
        eligible.Sort(CompareChronologically);

        // Track which entities are already scheduled (pre-seed with already-annotated ones)
        var scheduledEntityIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, entity) in entities)
        {
            if (entity.HasHistorianNotes)
                scheduledEntityIds.Add(id);
        }

        // Interleave: chronicle → its cast entities
        foreach (var chron in eligible)
        {
            var tone = chron.AssignedTone ?? "weary";

            workItems.Add(new InterleavedWorkItem.Chronicle(
                chron.ChronicleId, chron.Title, tone));

            ScheduleChronicleEntities(chron, tone, entities, scheduledEntityIds, workItems);
        }

        // Append orphan entities — have descriptions but weren't referenced by any chronicle
        var orphanIndex = 0;
        foreach (var (id, entity) in entities)
        {
            if (scheduledEntityIds.Contains(id)) continue;
            if (!entity.HasDescription) continue;

            workItems.Add(new InterleavedWorkItem.Entity(
                id, entity.Name, entity.Kind,
                OrphanToneCycle[orphanIndex % OrphanToneCycle.Length]));

            orphanIndex++;
            scheduledEntityIds.Add(id);
        }

        return workItems;
    }

    /// <summary>
    /// Compute previous-notes cap based on position in the interleaved run.
    /// Early items get fewer/no previous notes to avoid forced cross-references.
    /// Returns null to use the default cap (typically 15).
    /// </summary>
    public static int? ComputeNotesMaxOverride(int itemIndex)
    {
        if (itemIndex <= 2) return 0;
        if (itemIndex <= 5) return 5;
        if (itemIndex <= 10) return 10;
        return null;
    }

    /// <summary>
    /// Count chronicles and entities in a work list.
    /// </summary>
    public static (int ChronicleCount, int EntityCount) CountWorkItemTypes(
        IReadOnlyList<InterleavedWorkItem> workItems)
    {
        var chronicles = 0;
        var entities = 0;
        foreach (var item in workItems)
        {
            if (item is InterleavedWorkItem.Chronicle) chronicles++;
            else entities++;
        }
        return (chronicles, entities);
    }

    // =========================================================================
    // Sorting
    // =========================================================================

    /// <summary>
    /// Chronological sort: year-0 chronicles pushed to end, then by era order,
    /// then by era year, then by era name.
    /// </summary>
    private static int CompareChronologically(
        InterleavedChronicleInput a, InterleavedChronicleInput b)
    {
        var (aZero, aOrder, aYear, aName) = SortKey(a);
        var (bZero, bOrder, bYear, bName) = SortKey(b);

        var cmp = aZero.CompareTo(bZero);
        if (cmp != 0) return cmp;

        cmp = aOrder.CompareTo(bOrder);
        if (cmp != 0) return cmp;

        cmp = aYear.CompareTo(bYear);
        if (cmp != 0) return cmp;

        return string.Compare(aName, bName, StringComparison.Ordinal);
    }

    private static (int ZeroFlag, int EraOrder, int EraYear, string EraName) SortKey(
        InterleavedChronicleInput c)
    {
        var year = c.EraYear ?? int.MaxValue;
        var zeroFlag = year == 0 ? 1 : 0;
        var order = c.FocalEraOrder ?? int.MaxValue;
        return (zeroFlag, order, year, c.FocalEraName ?? "");
    }

    // =========================================================================
    // Entity scheduling
    // =========================================================================

    /// <summary>
    /// Schedule entity work items for a single chronicle's referenced entities.
    /// Primary roles are scheduled before supporting roles.
    /// </summary>
    private static void ScheduleChronicleEntities(
        InterleavedChronicleInput chronicle,
        string chronicleTone,
        IReadOnlyDictionary<string, InterleavedEntityInput> entities,
        HashSet<string> scheduledEntityIds,
        List<InterleavedWorkItem> workItems)
    {
        var roles = chronicle.RoleAssignments;
        if (roles is null || roles.Count == 0) return;

        // Primary roles first, then supporting
        var ordered = roles
            .OrderByDescending(r => r.IsPrimary)
            .ToList();

        foreach (var role in ordered)
        {
            if (scheduledEntityIds.Contains(role.EntityId)) continue;
            if (!entities.TryGetValue(role.EntityId, out var entity)) continue;
            if (!entity.HasDescription) continue;

            scheduledEntityIds.Add(role.EntityId);
            workItems.Add(new InterleavedWorkItem.Entity(
                role.EntityId, entity.Name, entity.Kind, chronicleTone));
        }
    }
}
