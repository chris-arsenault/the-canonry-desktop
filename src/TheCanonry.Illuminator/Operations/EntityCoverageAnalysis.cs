/**
 * EntityCoverageAnalysis — per-entity completeness scoring.
 *
 * Port of entityCoverageUtils.ts. Computes:
 *   - HasDescription
 *   - ChronicleBackrefs (chronicles whose EntityIds JSON contains the entity ID)
 *   - ExpectedBackrefs  (based on prominence tier)
 *   - EventCount        (narrative events where SubjectId matches)
 */

namespace TheCanonry.Illuminator.Operations;

using TheCanonry.Persistence.Entities;

public static class EntityCoverageAnalysis
{
    // -------------------------------------------------------------------------
    // Types
    // -------------------------------------------------------------------------

    public sealed record EntityCoverage(
        string EntityId,
        string EntityName,
        string Kind,
        string Culture,
        double Prominence,
        bool HasDescription,
        int ChronicleBackrefs,
        int ExpectedBackrefs,
        int EventCount,
        int RelationshipCount);

    // -------------------------------------------------------------------------
    // Analysis
    // -------------------------------------------------------------------------

    /// <summary>
    /// Computes per-entity coverage for every entity in the simulation run.
    /// </summary>
    public static IReadOnlyList<EntityCoverage> Analyze(
        IReadOnlyList<PersistedEntity> entities,
        IReadOnlyList<Chronicle> chronicles,
        IReadOnlyList<NarrativeEventEntity>? narrativeEvents = null)
    {
        // Build a lookup: entityId → chronicle backref count
        var backrefCounts = BuildBackrefCounts(chronicles);

        // Build a lookup: entityId → event count
        var eventCounts = BuildEventCounts(narrativeEvents);

        var result = new List<EntityCoverage>(entities.Count);

        foreach (var entity in entities)
        {
            backrefCounts.TryGetValue(entity.Id, out var backrefCount);
            eventCounts.TryGetValue(entity.Id, out var eventCount);

            result.Add(new EntityCoverage(
                EntityId:         entity.Id,
                EntityName:       entity.Name,
                Kind:             entity.Kind,
                Culture:          entity.Culture,
                Prominence:       entity.Prominence,
                HasDescription:   !string.IsNullOrWhiteSpace(entity.Description),
                ChronicleBackrefs: backrefCount,
                ExpectedBackrefs: ExpectedForProminence(entity.Prominence),
                EventCount:       eventCount,
                RelationshipCount: 0)); // placeholder — computed from graph elsewhere
        }

        return result;
    }

    // -------------------------------------------------------------------------
    // Prominence helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the expected number of chronicle backrefs for a given prominence value.
    /// Thresholds mirror the TS source:
    ///   mythic    (≥ 0.8) → 3
    ///   renowned  (≥ 0.6) → 2
    ///   recognized (≥ 0.3) → 1
    ///   below recognized  → 0
    /// </summary>
    public static int ExpectedForProminence(double prominence)
    {
        if (prominence >= 0.8) return 3;
        if (prominence >= 0.6) return 2;
        if (prominence >= 0.3) return 1;
        return 0;
    }

    /// <summary>
    /// Returns the human-readable label for a prominence value.
    /// </summary>
    public static string ProminenceLabel(double value)
    {
        if (value >= 0.8) return "mythic";
        if (value >= 0.6) return "renowned";
        if (value >= 0.3) return "recognized";
        if (value >= 0.1) return "marginal";
        return "forgotten";
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static Dictionary<string, int> BuildBackrefCounts(IReadOnlyList<Chronicle> chronicles)
    {
        var counts = new Dictionary<string, int>();

        foreach (var chronicle in chronicles)
        {
            if (string.IsNullOrWhiteSpace(chronicle.EntityIds)) continue;

            // EntityIds is a JSON array like ["id1","id2"]. We parse it simply
            // by checking if the string contains the entity ID as a JSON string value.
            var entityIds = ParseJsonStringArray(chronicle.EntityIds);
            foreach (var id in entityIds)
            {
                counts.TryGetValue(id, out var existing);
                counts[id] = existing + 1;
            }
        }

        return counts;
    }

    private static Dictionary<string, int> BuildEventCounts(
        IReadOnlyList<NarrativeEventEntity>? narrativeEvents)
    {
        var counts = new Dictionary<string, int>();
        if (narrativeEvents is null) return counts;

        foreach (var ev in narrativeEvents)
        {
            if (string.IsNullOrEmpty(ev.SubjectId)) continue;
            counts.TryGetValue(ev.SubjectId, out var existing);
            counts[ev.SubjectId] = existing + 1;
        }

        return counts;
    }

    /// <summary>
    /// Minimal JSON string-array parser. Handles the format produced by
    /// <c>System.Text.Json.JsonSerializer.Serialize(IReadOnlyList&lt;string&gt;)</c>.
    /// Returns an empty list for malformed input rather than throwing.
    /// </summary>
    private static IReadOnlyList<string> ParseJsonStringArray(string json)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
