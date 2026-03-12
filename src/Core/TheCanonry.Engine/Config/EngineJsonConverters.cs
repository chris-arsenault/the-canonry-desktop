namespace TheCanonry.Engine.Config;

using System.Text.Json;
using System.Text.Json.Serialization;
using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Pressures;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Engine.Templates;

// =============================================================================
// CONDITION CONVERTER
// =============================================================================

/// <summary>
/// Deserializes the Condition sealed hierarchy using the "type" discriminator field.
/// </summary>
public class ConditionConverter : JsonConverter<Condition>
{
    public override Condition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        return ReadCondition(doc.RootElement, options);
    }

    internal static Condition ReadCondition(JsonElement root, JsonSerializerOptions options)
    {
        var type = root.GetProperty("type").GetString()!;

        return type switch
        {
            "pressure" => ReadPressureCondition(root),
            "pressure_compare" => ReadPressureCompare(root),
            "pressure_any_above" => ReadPressureAnyAbove(root),
            "entity_count" => ReadEntityCount(root),
            "relationship_count" => ReadRelationshipCount(root),
            "relationship_exists" => ReadRelationshipExists(root),
            "tag_exists" => ReadTagExists(root),
            "lacks_tag" => ReadLacksTag(root),
            "status" => ReadStatus(root),
            "prominence" => ReadProminence(root),
            "time_elapsed" => ReadTimeElapsed(root),
            "cooldown_elapsed" => ReadCooldownElapsed(root),
            "creations_per_epoch" => ReadCreationsPerEpoch(root),
            "growth_phases_complete" => ReadGrowthPhasesComplete(root),
            "era_match" => ReadEraMatch(root),
            "random_chance" => ReadRandomChance(root),
            "graph_path" => ReadGraphPath(root, options),
            "component_size" => ReadComponentSize(root),
            "entity_exists" => ReadEntityExists(root),
            "entity_has_relationship" => ReadEntityHasRelationship(root),
            "and" => ReadAnd(root, options),
            "or" => ReadOr(root, options),
            "always" => new AlwaysCondition(),
            _ => throw new JsonException($"Unknown Condition type: {type}")
        };
    }

    private static PressureCondition ReadPressureCondition(JsonElement e) =>
        new(
            e.GetProperty("pressureId").GetString()!,
            e.TryGetProperty("min", out var min) ? min.GetDouble() : double.MinValue,
            e.TryGetProperty("max", out var max) ? max.GetDouble() : double.MaxValue);

    private static PressureCompareCondition ReadPressureCompare(JsonElement e) =>
        new(
            e.GetProperty("pressureA").GetString()!,
            e.GetProperty("pressureB").GetString()!,
            e.TryGetProperty("operator", out var op)
                ? ParseComparisonOperator(op.GetString()!)
                : ComparisonOperator.GreaterThanOrEqual);

    private static PressureAnyAboveCondition ReadPressureAnyAbove(JsonElement e) =>
        new(
            ReadStringArray(e.GetProperty("pressureIds")),
            e.GetProperty("threshold").GetDouble());

    private static EntityCountCondition ReadEntityCount(JsonElement e) =>
        new(
            e.TryGetProperty("kind", out var kind) ? kind.GetString()! : "",
            e.TryGetProperty("subtype", out var sub) ? sub.GetString()! : "",
            e.TryGetProperty("status", out var st) ? st.GetString()! : "",
            e.TryGetProperty("min", out var min) ? min.GetInt32() : 0,
            e.TryGetProperty("max", out var max) ? max.GetInt32() : int.MaxValue,
            e.TryGetProperty("overshootFactor", out var osf) ? osf.GetDouble() : 1.0);

    private static RelationshipCountCondition ReadRelationshipCount(JsonElement e) =>
        new(
            e.GetProperty("relationshipKind").GetString()!,
            e.TryGetProperty("direction", out var d) ? ParseDirection(d.GetString()!) : Direction.Both,
            e.TryGetProperty("min", out var min) ? min.GetInt32() : 0,
            e.TryGetProperty("max", out var max) ? max.GetInt32() : int.MaxValue);

    private static RelationshipExistsCondition ReadRelationshipExists(JsonElement e) =>
        new(
            e.GetProperty("relationshipKind").GetString()!,
            e.TryGetProperty("with", out var w) ? w.GetString()! : "",
            e.TryGetProperty("direction", out var d) ? ParseDirection(d.GetString()!) : Direction.Both,
            e.TryGetProperty("targetKind", out var tk) ? tk.GetString()! : "",
            e.TryGetProperty("targetSubtype", out var ts) ? ts.GetString()! : "",
            e.TryGetProperty("targetStatus", out var tst) ? tst.GetString()! : "");

    private static TagExistsCondition ReadTagExists(JsonElement e) =>
        new(
            e.TryGetProperty("entity", out var ent) ? ent.GetString()! : "",
            e.GetProperty("tag").GetString()!,
            e.TryGetProperty("value", out var v) ? v.GetString()! : "");

    private static LacksTagCondition ReadLacksTag(JsonElement e) =>
        new(
            e.TryGetProperty("entity", out var ent) ? ent.GetString()! : "",
            e.GetProperty("tag").GetString()!);

    private static StatusCondition ReadStatus(JsonElement e) =>
        new(
            e.GetProperty("status").GetString()!,
            e.TryGetProperty("not", out var n) && n.GetBoolean());

    private static ProminenceCondition ReadProminence(JsonElement e) =>
        new(
            e.TryGetProperty("minLabel", out var min) ? min.GetString()! : "",
            e.TryGetProperty("maxLabel", out var max) ? max.GetString()! : "");

    private static TimeElapsedCondition ReadTimeElapsed(JsonElement e) =>
        new(
            e.GetProperty("minTicks").GetInt32(),
            e.TryGetProperty("since", out var s) ? s.GetString()! : "created");

    private static CooldownElapsedCondition ReadCooldownElapsed(JsonElement e) =>
        new(e.GetProperty("cooldownTicks").GetInt32());

    private static CreationsPerEpochCondition ReadCreationsPerEpoch(JsonElement e) =>
        new(e.GetProperty("maxPerEpoch").GetInt32());

    private static GrowthPhasesCompleteCondition ReadGrowthPhasesComplete(JsonElement e) =>
        new(
            e.GetProperty("minPhases").GetInt32(),
            e.TryGetProperty("eraId", out var era) ? era.GetString()! : "");

    private static EraMatchCondition ReadEraMatch(JsonElement e) =>
        new(ReadStringArray(e.GetProperty("eras")));

    private static RandomChanceCondition ReadRandomChance(JsonElement e) =>
        new(e.GetProperty("chance").GetDouble());

    private static GraphPathCondition ReadGraphPath(JsonElement e, JsonSerializerOptions options) =>
        new(GraphPathAssertionReader.Read(e.GetProperty("assert"), options));

    private static ComponentSizeCondition ReadComponentSize(JsonElement e) =>
        new(
            ReadStringArray(e.GetProperty("relationshipKinds")),
            e.TryGetProperty("min", out var min) ? min.GetInt32() : 0,
            e.TryGetProperty("max", out var max) ? max.GetInt32() : int.MaxValue,
            e.TryGetProperty("minStrength", out var ms) ? ms.GetDouble() : 0.0);

    private static EntityExistsCondition ReadEntityExists(JsonElement e) =>
        new(e.GetProperty("entity").GetString()!);

    private static EntityHasRelationshipCondition ReadEntityHasRelationship(JsonElement e) =>
        new(
            e.GetProperty("entity").GetString()!,
            e.GetProperty("relationshipKind").GetString()!,
            e.TryGetProperty("direction", out var d) ? ParseDirection(d.GetString()!) : Direction.Both);

    private static AndCondition ReadAnd(JsonElement e, JsonSerializerOptions options) =>
        new(ReadConditionArray(e.GetProperty("conditions"), options));

    private static OrCondition ReadOr(JsonElement e, JsonSerializerOptions options) =>
        new(ReadConditionArray(e.GetProperty("conditions"), options));

    internal static IReadOnlyList<Condition> ReadConditionArray(JsonElement arr, JsonSerializerOptions options)
    {
        var list = new List<Condition>();
        foreach (var item in arr.EnumerateArray())
            list.Add(ReadCondition(item, options));
        return list;
    }

    public override void Write(Utf8JsonWriter writer, Condition value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Condition serialization is not supported");

    internal static Direction ParseDirection(string value) => value switch
    {
        "src" or "out" or "source" => Direction.Source,
        "dst" or "in" or "target" => Direction.Target,
        "both" or "any" => Direction.Both,
        _ => throw new JsonException($"Unknown direction: {value}")
    };

    internal static ComparisonOperator ParseComparisonOperator(string value) => value switch
    {
        ">=" => ComparisonOperator.GreaterThanOrEqual,
        "<=" => ComparisonOperator.LessThanOrEqual,
        ">" => ComparisonOperator.GreaterThan,
        "<" => ComparisonOperator.LessThan,
        "==" => ComparisonOperator.Equal,
        "!=" => ComparisonOperator.NotEqual,
        _ => throw new JsonException($"Unknown comparison operator: {value}")
    };

    internal static IReadOnlyList<string> ReadStringArray(JsonElement arr)
    {
        var list = new List<string>();
        foreach (var item in arr.EnumerateArray())
            list.Add(item.GetString()!);
        return list;
    }
}

// =============================================================================
// SELECTION FILTER CONVERTER
// =============================================================================

/// <summary>
/// Deserializes the SelectionFilter sealed hierarchy using the "type" discriminator field.
/// </summary>
public class SelectionFilterConverter : JsonConverter<SelectionFilter>
{
    public override SelectionFilter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        return ReadFilter(doc.RootElement, options);
    }

    internal static SelectionFilter ReadFilter(JsonElement root, JsonSerializerOptions options)
    {
        var type = root.GetProperty("type").GetString()!;

        return type switch
        {
            "exclude" => new ExcludeEntitiesFilter(
                ConditionConverter.ReadStringArray(root.GetProperty("entities"))),
            "has_relationship" => new HasRelationshipFilter(
                root.GetProperty("kind").GetString()!,
                root.TryGetProperty("with", out var w) ? w.GetString()! : "",
                root.TryGetProperty("direction", out var d) ? ConditionConverter.ParseDirection(d.GetString()!) : Direction.Both),
            "lacks_relationship" => new LacksRelationshipFilter(
                root.GetProperty("kind").GetString()!,
                root.TryGetProperty("with", out var lw) ? lw.GetString()! : ""),
            "shares_related" => new SharesRelatedFilter(
                root.GetProperty("relationshipKind").GetString()!,
                root.GetProperty("with").GetString()!),
            "has_tag" => new HasTagFilter(
                root.GetProperty("tag").GetString()!,
                root.TryGetProperty("value", out var htv) ? htv.GetString()! : ""),
            "has_tags" => new HasTagsFilter(
                ConditionConverter.ReadStringArray(root.GetProperty("tags"))),
            "has_any_tag" => new HasAnyTagFilter(
                ConditionConverter.ReadStringArray(root.GetProperty("tags"))),
            "lacks_tag" => new LacksTagFilter(
                root.GetProperty("tag").GetString()!,
                root.TryGetProperty("value", out var ltv) ? ltv.GetString()! : ""),
            "lacks_any_tag" => new LacksAnyTagFilter(
                ConditionConverter.ReadStringArray(root.GetProperty("tags"))),
            "has_culture" => new HasCultureFilter(
                root.GetProperty("culture").GetString()!),
            "not_has_culture" => new NotHasCultureFilter(
                root.GetProperty("culture").GetString()!),
            "matches_culture" => new MatchesCultureFilter(
                root.GetProperty("with").GetString()!),
            "not_matches_culture" => new NotMatchesCultureFilter(
                root.GetProperty("with").GetString()!),
            "has_status" => new HasStatusFilter(
                root.GetProperty("status").GetString()!),
            "has_prominence" => new HasProminenceFilter(
                root.GetProperty("minProminence").GetString()!),
            "graph_path" => new GraphPathFilter(
                GraphPathAssertionReader.Read(root.GetProperty("assert"), options)),
            "component_size" => new ComponentSizeFilter(
                ConditionConverter.ReadStringArray(root.GetProperty("relationshipKinds")),
                root.TryGetProperty("min", out var csMin) ? csMin.GetInt32() : 0,
                root.TryGetProperty("max", out var csMax) ? csMax.GetInt32() : int.MaxValue,
                root.TryGetProperty("minStrength", out var csMs) ? csMs.GetDouble() : 0.0),
            _ => throw new JsonException($"Unknown SelectionFilter type: {type}")
        };
    }

    internal static IReadOnlyList<SelectionFilter> ReadFilterArray(JsonElement arr, JsonSerializerOptions options)
    {
        var list = new List<SelectionFilter>();
        foreach (var item in arr.EnumerateArray())
            list.Add(ReadFilter(item, options));
        return list;
    }

    public override void Write(Utf8JsonWriter writer, SelectionFilter value, JsonSerializerOptions options) =>
        throw new NotSupportedException("SelectionFilter serialization is not supported");
}

// =============================================================================
// MUTATION CONVERTER
// =============================================================================

/// <summary>
/// Deserializes the Mutation sealed hierarchy using the "type" discriminator field.
/// </summary>
public class MutationConverter : JsonConverter<Mutation>
{
    public override Mutation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        return ReadMutation(doc.RootElement, options);
    }

    internal static Mutation ReadMutation(JsonElement root, JsonSerializerOptions options)
    {
        var type = root.GetProperty("type").GetString()!;

        return type switch
        {
            "set_tag" => new SetTagMutation(
                root.TryGetProperty("entity", out var ste) ? ste.GetString()! : "",
                root.GetProperty("tag").GetString()!,
                root.TryGetProperty("value", out var stv) ? stv.ToString() : "",
                root.TryGetProperty("valueFrom", out var stvf) ? stvf.GetString()! : ""),
            "remove_tag" => new RemoveTagMutation(
                root.TryGetProperty("entity", out var rte) ? rte.GetString()! : "",
                root.GetProperty("tag").GetString()!),
            "create_relationship" => new CreateRelationshipMutation(
                root.TryGetProperty("src", out var crs) ? crs.GetString()! : "",
                root.TryGetProperty("dst", out var crd) ? crd.GetString()! : "",
                root.GetProperty("kind").GetString()!,
                root.TryGetProperty("strength", out var crsStr) ? crsStr.GetDouble() : 0.5,
                root.TryGetProperty("bidirectional", out var crbi) && crbi.GetBoolean(),
                root.TryGetProperty("category", out var crc) ? crc.GetString()! : ""),
            "archive_relationship" => new ArchiveRelationshipMutation(
                root.TryGetProperty("entity", out var are) ? are.GetString()! : "",
                root.GetProperty("relationshipKind").GetString()!,
                root.TryGetProperty("with", out var arw) ? arw.GetString()! : "",
                root.TryGetProperty("direction", out var ard) ? ConditionConverter.ParseDirection(ard.GetString()!) : Direction.Both),
            "archive_all_relationships" => new ArchiveAllRelationshipsMutation(
                root.GetProperty("entity").GetString()!,
                root.GetProperty("relationshipKind").GetString()!,
                root.TryGetProperty("direction", out var aard) ? ConditionConverter.ParseDirection(aard.GetString()!) : Direction.Both),
            "adjust_relationship_strength" => new AdjustRelationshipStrengthMutation(
                root.GetProperty("src").GetString()!,
                root.GetProperty("dst").GetString()!,
                root.GetProperty("kind").GetString()!,
                root.GetProperty("delta").GetDouble(),
                root.TryGetProperty("bidirectional", out var arsbi) && arsbi.GetBoolean()),
            "transfer_relationship" => new TransferRelationshipMutation(
                root.GetProperty("entity").GetString()!,
                root.GetProperty("relationshipKind").GetString()!,
                root.GetProperty("from").GetString()!,
                root.GetProperty("to").GetString()!,
                root.TryGetProperty("condition", out var trc)
                    ? ConditionConverter.ReadCondition(trc, options)
                    : new AlwaysCondition()),
            "change_status" => new ChangeStatusMutation(
                root.GetProperty("entity").GetString()!,
                root.GetProperty("newStatus").GetString()!),
            "adjust_prominence" => new AdjustProminenceMutation(
                root.TryGetProperty("entity", out var ape) ? ape.GetString()! : "",
                root.GetProperty("delta").GetDouble()),
            "modify_pressure" => new ModifyPressureMutation(
                root.GetProperty("pressureId").GetString()!,
                root.GetProperty("delta").GetDouble()),
            "update_rate_limit" => new UpdateRateLimitMutation(),
            "for_each_related" => new ForEachRelatedAction(
                root.GetProperty("relationship").GetString()!,
                root.TryGetProperty("direction", out var ferd) ? ConditionConverter.ParseDirection(ferd.GetString()!) : Direction.Both,
                root.TryGetProperty("targetKind", out var fertk) ? fertk.GetString()! : "",
                root.TryGetProperty("targetSubtype", out var ferts) ? ferts.GetString()! : "",
                ReadMutationArray(root.GetProperty("actions"), options)),
            "conditional" => new ConditionalAction(
                ConditionConverter.ReadCondition(root.GetProperty("condition"), options),
                root.TryGetProperty("thenActions", out var caThen) ? ReadMutationArray(caThen, options) : [],
                root.TryGetProperty("elseActions", out var caElse) ? ReadMutationArray(caElse, options) : []),
            _ => throw new JsonException($"Unknown Mutation type: {type}")
        };
    }

    internal static IReadOnlyList<Mutation> ReadMutationArray(JsonElement arr, JsonSerializerOptions options)
    {
        var list = new List<Mutation>();
        foreach (var item in arr.EnumerateArray())
            list.Add(ReadMutation(item, options));
        return list;
    }

    public override void Write(Utf8JsonWriter writer, Mutation value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Mutation serialization is not supported");
}

// =============================================================================
// METRIC CONVERTER
// =============================================================================

/// <summary>
/// Deserializes the Metric sealed hierarchy using the "type" discriminator field.
/// Used by pressures.json for positive/negative feedback metrics.
/// </summary>
public class MetricConverter : JsonConverter<Metric>
{
    public override Metric Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        return ReadMetric(doc.RootElement, options);
    }

    internal static Metric ReadMetric(JsonElement root, JsonSerializerOptions options)
    {
        var type = root.GetProperty("type").GetString()!;

        return type switch
        {
            "entity_count" => new EntityCountMetric(
                root.TryGetProperty("kind", out var eck) ? eck.GetString()! : "",
                root.TryGetProperty("subtype", out var ecs) ? ecs.GetString()! : "",
                root.TryGetProperty("status", out var ecst) ? ecst.GetString()! : "",
                root.TryGetProperty("coefficient", out var ecc) ? ecc.GetDouble() : 1.0,
                root.TryGetProperty("cap", out var ecCap) ? ecCap.GetDouble() : double.MaxValue),
            "relationship_count" => new RelationshipCountMetric(
                ConditionConverter.ReadStringArray(root.GetProperty("relationshipKinds")),
                root.TryGetProperty("direction", out var rcd) ? ConditionConverter.ParseDirection(rcd.GetString()!) : Direction.Both,
                root.TryGetProperty("minStrength", out var rcms) ? rcms.GetDouble() : 0.0,
                root.TryGetProperty("coefficient", out var rcc) ? rcc.GetDouble() : 1.0,
                root.TryGetProperty("cap", out var rcCap) ? rcCap.GetDouble() : double.MaxValue),
            "tag_count" => new TagCountMetric(
                ConditionConverter.ReadStringArray(root.GetProperty("tags")),
                root.TryGetProperty("coefficient", out var tcc) ? tcc.GetDouble() : 1.0,
                root.TryGetProperty("cap", out var tcCap) ? tcCap.GetDouble() : double.MaxValue),
            "total_entities" => new TotalEntitiesMetric(
                root.TryGetProperty("coefficient", out var tec) ? tec.GetDouble() : 1.0,
                root.TryGetProperty("cap", out var teCap) ? teCap.GetDouble() : double.MaxValue),
            "constant" => new ConstantMetric(
                root.GetProperty("value").GetDouble(),
                root.TryGetProperty("coefficient", out var cc) ? cc.GetDouble() : 1.0),
            "connection_count" => new ConnectionCountMetric(
                root.TryGetProperty("relationshipKinds", out var ccrk) ? ConditionConverter.ReadStringArray(ccrk) : [],
                root.TryGetProperty("direction", out var ccd) ? ConditionConverter.ParseDirection(ccd.GetString()!) : Direction.Both,
                root.TryGetProperty("minStrength", out var ccms) ? ccms.GetDouble() : 0.0,
                root.TryGetProperty("coefficient", out var ccc) ? ccc.GetDouble() : 1.0,
                root.TryGetProperty("cap", out var ccCap) ? ccCap.GetDouble() : double.MaxValue),
            "ratio" => ReadRatio(root, options),
            "status_ratio" => new StatusRatioMetric(
                root.GetProperty("kind").GetString()!,
                root.TryGetProperty("subtype", out var srs) ? srs.GetString()! : "",
                root.GetProperty("aliveStatus").GetString()!,
                root.TryGetProperty("coefficient", out var src) ? src.GetDouble() : 1.0,
                root.TryGetProperty("cap", out var srCap) ? srCap.GetDouble() : double.MaxValue),
            "cross_culture_ratio" => new CrossCultureRatioMetric(
                ConditionConverter.ReadStringArray(root.GetProperty("relationshipKinds")),
                root.TryGetProperty("coefficient", out var ccrc) ? ccrc.GetDouble() : 1.0,
                root.TryGetProperty("cap", out var ccrCap) ? ccrCap.GetDouble() : double.MaxValue),
            "shared_relationship" => ReadSharedRelationship(root),
            "prominence_multiplier" => new ProminenceMultiplierMetric(
                root.TryGetProperty("mode", out var pm) ? pm.GetString()! : "linear"),
            "neighbor_prominence" => new NeighborProminenceMetric(
                root.TryGetProperty("relationshipKinds", out var nprk) ? ConditionConverter.ReadStringArray(nprk) : [],
                root.TryGetProperty("direction", out var npd) ? ConditionConverter.ParseDirection(npd.GetString()!) : Direction.Both,
                root.TryGetProperty("minStrength", out var npms) ? npms.GetDouble() : 0.0,
                root.TryGetProperty("coefficient", out var npc) ? npc.GetDouble() : 1.0,
                root.TryGetProperty("cap", out var npCap) ? npCap.GetDouble() : double.MaxValue),
            "neighbor_kind_count" => ReadNeighborKindCount(root),
            "component_size" => new ComponentSizeMetric(
                ConditionConverter.ReadStringArray(root.GetProperty("relationshipKinds")),
                root.TryGetProperty("minStrength", out var csms) ? csms.GetDouble() : 0.0,
                root.TryGetProperty("coefficient", out var csc) ? csc.GetDouble() : 1.0,
                root.TryGetProperty("cap", out var csCap) ? csCap.GetDouble() : double.MaxValue),
            "decay_rate" => new DecayRateMetric(
                root.GetProperty("rate").GetString()!),
            "falloff" => new FalloffMetric(
                root.GetProperty("falloffType").GetString()!,
                root.GetProperty("distance").GetDouble(),
                root.GetProperty("maxDistance").GetDouble()),
            _ => throw new JsonException($"Unknown Metric type: {type}")
        };
    }

    private static RatioMetric ReadRatio(JsonElement root, JsonSerializerOptions options) =>
        new(
            ReadSimpleCount(root.GetProperty("numerator")),
            ReadSimpleCount(root.GetProperty("denominator")),
            root.TryGetProperty("fallbackValue", out var fv) ? fv.GetDouble() : 0.0,
            root.TryGetProperty("coefficient", out var c) ? c.GetDouble() : 1.0,
            root.TryGetProperty("cap", out var cap) ? cap.GetDouble() : double.MaxValue);

    internal static SimpleCountMetric ReadSimpleCount(JsonElement e)
    {
        var type = e.GetProperty("type").GetString()!;
        return type switch
        {
            "entity_count" => new SimpleEntityCount(
                e.TryGetProperty("kind", out var k) ? k.GetString()! : "",
                e.TryGetProperty("subtype", out var s) ? s.GetString()! : "",
                e.TryGetProperty("status", out var st) ? st.GetString()! : ""),
            "relationship_count" => new SimpleRelationshipCount(
                ConditionConverter.ReadStringArray(e.GetProperty("relationshipKinds"))),
            "tag_count" => new SimpleTagCount(
                ConditionConverter.ReadStringArray(e.GetProperty("tags"))),
            "total_entities" => new SimpleTotalEntities(),
            "constant" => new SimpleConstant(e.GetProperty("value").GetDouble()),
            _ => throw new JsonException($"Unknown SimpleCountMetric type: {type}")
        };
    }

    private static SharedRelationshipMetric ReadSharedRelationship(JsonElement root) =>
        new(
            root.TryGetProperty("sharedRelationshipKinds", out var srks)
                ? ConditionConverter.ReadStringArray(srks)
                : root.TryGetProperty("sharedRelationshipKind", out var srk)
                    ? [srk.GetString()!]
                    : [],
            root.TryGetProperty("sharedDirection", out var sd) ? ConditionConverter.ParseDirection(sd.GetString()!) : Direction.Both,
            root.TryGetProperty("via", out var via) ? ReadSharedVia(via) : null,
            root.TryGetProperty("minStrength", out var ms) ? ms.GetDouble() : 0.0,
            root.TryGetProperty("coefficient", out var c) ? c.GetDouble() : 1.0,
            root.TryGetProperty("cap", out var cap) ? cap.GetDouble() : double.MaxValue);

    private static SharedRelationshipVia ReadSharedVia(JsonElement e) =>
        new(
            e.GetProperty("relationshipKind").GetString()!,
            e.TryGetProperty("direction", out var d) ? ConditionConverter.ParseDirection(d.GetString()!) : Direction.Both,
            e.TryGetProperty("intermediateKind", out var ik) ? ik.GetString()! : "");

    private static NeighborKindCountMetric ReadNeighborKindCount(JsonElement root) =>
        new(
            root.TryGetProperty("via", out var via) ? ConditionConverter.ReadStringArray(via) : [],
            root.TryGetProperty("viaDirection", out var vd) ? ConditionConverter.ParseDirection(vd.GetString()!) : Direction.Both,
            root.TryGetProperty("then", out var then) ? then.GetString()! : "",
            root.TryGetProperty("thenDirection", out var td) ? ConditionConverter.ParseDirection(td.GetString()!) : Direction.Both,
            root.TryGetProperty("kind", out var k) ? k.GetString()! : "",
            root.TryGetProperty("subtype", out var s) ? s.GetString()! : "",
            root.TryGetProperty("status", out var st) ? st.GetString()! : "",
            root.TryGetProperty("hasTag", out var ht) ? ht.GetString()! : "",
            root.TryGetProperty("minStrength", out var ms) ? ms.GetDouble() : 0.0,
            root.TryGetProperty("coefficient", out var c) ? c.GetDouble() : 1.0,
            root.TryGetProperty("cap", out var cap) ? cap.GetDouble() : double.MaxValue);

    public override void Write(Utf8JsonWriter writer, Metric value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Metric serialization is not supported");
}

// =============================================================================
// SUBTYPE SPEC CONVERTER
// =============================================================================

/// <summary>
/// Deserializes SubtypeSpec from the JSON.
/// In the domain JSON, creation rule "subtype" can be:
///   - A plain string (e.g., "hero") → FixedSubtype
///   - An object with "conditional": {...} → ConditionalSubtype
///   - An object with "type": "fixed", "value": "..." → FixedSubtype
///   - An object with "type": "inherit", "ref": "..." → InheritSubtype
///   - An object with "type": "from_pressure", ... → FromPressureSubtype
///   - An object with "type": "random", ... → RandomSubtype
/// </summary>
public class SubtypeSpecConverter : JsonConverter<SubtypeSpec>
{
    public override SubtypeSpec Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        return ReadSubtypeSpec(doc.RootElement, options);
    }

    internal static SubtypeSpec ReadSubtypeSpec(JsonElement root, JsonSerializerOptions options)
    {
        // Plain string → FixedSubtype
        if (root.ValueKind == JsonValueKind.String)
            return new FixedSubtype(root.GetString()!);

        // Object with "conditional" key
        if (root.TryGetProperty("conditional", out var cond))
            return ReadConditional(cond, options);

        // Object with "random" shorthand: { "random": ["a", "b"] }
        if (root.TryGetProperty("random", out var randomArr))
            return new RandomSubtype(ConditionConverter.ReadStringArray(randomArr));

        // Object with "type" discriminator
        if (root.TryGetProperty("type", out var typeProp))
        {
            var type = typeProp.GetString()!;
            return type switch
            {
                "fixed" => new FixedSubtype(root.GetProperty("value").GetString()!),
                "inherit" => new InheritSubtype(
                    root.GetProperty("ref").GetString()!,
                    root.TryGetProperty("chance", out var ch) ? ch.GetDouble() : 1.0),
                "from_pressure" => ReadFromPressure(root),
                "random" => new RandomSubtype(
                    ConditionConverter.ReadStringArray(root.GetProperty("options"))),
                _ => throw new JsonException($"Unknown SubtypeSpec type: {type}")
            };
        }

        throw new JsonException("Cannot determine SubtypeSpec type from JSON");
    }

    private static ConditionalSubtype ReadConditional(JsonElement cond, JsonSerializerOptions options)
    {
        var whens = new List<SubtypeWhen>();
        foreach (var w in cond.GetProperty("when").EnumerateArray())
        {
            var condEl = w.GetProperty("condition");
            SubtypeCondition sc = condEl.GetProperty("type").GetString()! switch
            {
                "target_subtype" => new TargetSubtypeCondition(
                    condEl.GetProperty("equals").GetString()!),
                "pressure_check" => new PressureCheckCondition(
                    condEl.GetProperty("pressureId").GetString()!,
                    condEl.TryGetProperty("min", out var min) ? min.GetDouble() : double.MinValue,
                    condEl.TryGetProperty("max", out var max) ? max.GetDouble() : double.MaxValue),
                var t => throw new JsonException($"Unknown SubtypeCondition type: {t}")
            };
            whens.Add(new SubtypeWhen(sc, w.GetProperty("then").GetString()!));
        }
        return new ConditionalSubtype(
            whens,
            cond.TryGetProperty("otherwise", out var ow) ? ow.GetString()! : "");
    }

    private static FromPressureSubtype ReadFromPressure(JsonElement root)
    {
        var mapping = new Dictionary<string, string>();
        foreach (var prop in root.GetProperty("pressureMapping").EnumerateObject())
            mapping[prop.Name] = prop.Value.GetString()!;
        return new FromPressureSubtype(mapping);
    }

    public override void Write(Utf8JsonWriter writer, SubtypeSpec value, JsonSerializerOptions options) =>
        throw new NotSupportedException("SubtypeSpec serialization is not supported");
}

// =============================================================================
// PLACEMENT ANCHOR CONVERTER
// =============================================================================

/// <summary>
/// Deserializes PlacementAnchor from the "anchor" JSON property.
/// JSON uses "type" discriminator: "entity", "culture", "refs_centroid", "sparse", "bounds".
/// </summary>
public class PlacementAnchorConverter : JsonConverter<PlacementAnchor>
{
    public override PlacementAnchor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        return ReadAnchor(doc.RootElement);
    }

    internal static PlacementAnchor ReadAnchor(JsonElement root)
    {
        var type = root.GetProperty("type").GetString()!;

        return type switch
        {
            "entity" => new EntityAnchor(
                root.GetProperty("ref").GetString()!,
                root.TryGetProperty("stickToRegion", out var str) && str.GetBoolean()),
            "culture" => new CultureAnchor(
                root.GetProperty("id").GetString()!),
            "refs_centroid" => new RefsCentroidAnchor(
                ConditionConverter.ReadStringArray(root.GetProperty("refs")),
                root.TryGetProperty("jitter", out var j) ? j.GetDouble() : 0.0),
            "sparse" => new SparseAnchor(
                root.TryGetProperty("preferPeriphery", out var pp) && pp.GetBoolean()),
            "bounds" => ReadBounds(root),
            _ => throw new JsonException($"Unknown PlacementAnchor type: {type}")
        };
    }

    private static BoundsAnchor ReadBounds(JsonElement root)
    {
        var x = root.GetProperty("x");
        var y = root.GetProperty("y");
        var z = root.TryGetProperty("z", out var zProp) ? zProp : default;

        return new BoundsAnchor(
            (x.GetProperty("min").GetDouble(), x.GetProperty("max").GetDouble()),
            (y.GetProperty("min").GetDouble(), y.GetProperty("max").GetDouble()),
            z.ValueKind == JsonValueKind.Object
                ? (z.GetProperty("min").GetDouble(), z.GetProperty("max").GetDouble())
                : (0.0, 100.0));
    }

    public override void Write(Utf8JsonWriter writer, PlacementAnchor value, JsonSerializerOptions options) =>
        throw new NotSupportedException("PlacementAnchor serialization is not supported");
}

// =============================================================================
// CULTURE SPEC CONVERTER
// =============================================================================

/// <summary>
/// Deserializes CultureSpec from the "culture" JSON property.
/// JSON is either { "inherit": "$ref" } or { "fixed": "id" }.
/// </summary>
public class CultureSpecConverter : JsonConverter<CultureSpec>
{
    public override CultureSpec Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        return ReadCultureSpec(doc.RootElement);
    }

    internal static CultureSpec ReadCultureSpec(JsonElement root)
    {
        if (root.TryGetProperty("inherit", out var inh))
            return new InheritCulture(inh.GetString()!);
        if (root.TryGetProperty("fixed", out var fix))
            return new FixedCulture(fix.GetString()!);

        throw new JsonException("Cannot determine CultureSpec type: expected 'inherit' or 'fixed' property");
    }

    public override void Write(Utf8JsonWriter writer, CultureSpec value, JsonSerializerOptions options) =>
        throw new NotSupportedException("CultureSpec serialization is not supported");
}

// =============================================================================
// DESCRIPTION SPEC CONVERTER
// =============================================================================

/// <summary>
/// Deserializes DescriptionSpec from the "description" JSON property.
/// JSON is either a string (FixedDescription) or { "template": "...", "replacements": {...} }.
/// </summary>
public class DescriptionSpecConverter : JsonConverter<DescriptionSpec>
{
    public override DescriptionSpec Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        return ReadDescriptionSpec(doc.RootElement);
    }

    internal static DescriptionSpec ReadDescriptionSpec(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.String)
            return new FixedDescription(root.GetString()!);

        if (root.TryGetProperty("template", out var tmpl))
        {
            var replacements = new Dictionary<string, string>();
            if (root.TryGetProperty("replacements", out var reps))
            {
                foreach (var prop in reps.EnumerateObject())
                    replacements[prop.Name] = prop.Value.GetString()!;
            }
            return new TemplateDescription(tmpl.GetString()!, replacements);
        }

        throw new JsonException("Cannot determine DescriptionSpec type");
    }

    public override void Write(Utf8JsonWriter writer, DescriptionSpec value, JsonSerializerOptions options) =>
        throw new NotSupportedException("DescriptionSpec serialization is not supported");
}

// =============================================================================
// VARIABLE SELECTION SOURCE CONVERTER
// =============================================================================

/// <summary>
/// Deserializes VariableSelectionSource from the "from" JSON property.
/// JSON "from" can be:
///   - The string "graph" → GraphSource
///   - An object with "relatedTo" → RelatedSource
///   - An object with "path" → PathSource
/// </summary>
public class VariableSelectionSourceConverter : JsonConverter<VariableSelectionSource>
{
    public override VariableSelectionSource Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        return ReadSource(doc.RootElement);
    }

    internal static VariableSelectionSource ReadSource(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.String)
        {
            if (root.GetString() == "graph")
                return new GraphSource();
            throw new JsonException($"Unknown VariableSelectionSource string: {root.GetString()}");
        }

        if (root.TryGetProperty("relatedTo", out var rt))
        {
            return new RelatedSource(new RelatedEntitiesSpec(
                rt.GetString()!,
                root.GetProperty("relationshipKind").GetString()!,
                root.TryGetProperty("direction", out var d) ? ConditionConverter.ParseDirection(d.GetString()!) : Direction.Both));
        }

        if (root.TryGetProperty("path", out var path))
        {
            var steps = new List<PathTraversalStep>();
            foreach (var step in path.EnumerateArray())
            {
                steps.Add(new PathTraversalStep(
                    step.TryGetProperty("from", out var f) ? f.GetString()! : "",
                    step.GetProperty("via").GetString()!,
                    step.TryGetProperty("direction", out var sd) ? ConditionConverter.ParseDirection(sd.GetString()!) : Direction.Both,
                    step.TryGetProperty("targetKind", out var tk) ? tk.GetString()! : "",
                    step.TryGetProperty("targetSubtype", out var ts) ? ts.GetString()! : "",
                    step.TryGetProperty("targetStatus", out var tst) ? tst.GetString()! : ""));
            }
            return new PathSource(new PathBasedSpec(steps));
        }

        throw new JsonException("Cannot determine VariableSelectionSource type");
    }

    public override void Write(Utf8JsonWriter writer, VariableSelectionSource value, JsonSerializerOptions options) =>
        throw new NotSupportedException("VariableSelectionSource serialization is not supported");
}

// =============================================================================
// PATH CONSTRAINT CONVERTER
// =============================================================================

/// <summary>
/// Deserializes PathConstraint hierarchy.
/// </summary>
public class PathConstraintConverter : JsonConverter<PathConstraint>
{
    public override PathConstraint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        return ReadConstraint(doc.RootElement);
    }

    internal static PathConstraint ReadConstraint(JsonElement root)
    {
        var type = root.GetProperty("type").GetString()!;
        return type switch
        {
            "not_in" => new NotInConstraint(root.GetProperty("set").GetString()!),
            "in" => new InConstraint(root.GetProperty("set").GetString()!),
            "lacks_relationship" => new LacksRelationshipConstraint(
                root.GetProperty("kind").GetString()!,
                root.TryGetProperty("with", out var w) ? w.GetString()! : "",
                root.TryGetProperty("direction", out var d) ? ParsePathDirection(d.GetString()!) : PathDirection.Any),
            "has_relationship" => new HasRelationshipConstraint(
                root.GetProperty("kind").GetString()!,
                root.TryGetProperty("with", out var hw) ? hw.GetString()! : "",
                root.TryGetProperty("direction", out var hd) ? ParsePathDirection(hd.GetString()!) : PathDirection.Any),
            "not_self" => new NotSelfConstraint(),
            "kind_equals" => new KindEqualsConstraint(root.GetProperty("kind").GetString()!),
            "subtype_equals" => new SubtypeEqualsConstraint(root.GetProperty("subtype").GetString()!),
            _ => throw new JsonException($"Unknown PathConstraint type: {type}")
        };
    }

    internal static PathDirection ParsePathDirection(string value) => value switch
    {
        "out" => PathDirection.Out,
        "in" => PathDirection.In,
        "any" => PathDirection.Any,
        _ => throw new JsonException($"Unknown PathDirection: {value}")
    };

    public override void Write(Utf8JsonWriter writer, PathConstraint value, JsonSerializerOptions options) =>
        throw new NotSupportedException("PathConstraint serialization is not supported");
}

// =============================================================================
// GRAPH PATH ASSERTION READER (shared helper)
// =============================================================================

/// <summary>
/// Shared reader for GraphPathAssertion used by both conditions and filters.
/// </summary>
internal static class GraphPathAssertionReader
{
    internal static GraphPathAssertion Read(JsonElement root, JsonSerializerOptions options)
    {
        var check = root.GetProperty("check").GetString()! switch
        {
            "exists" => PathCheck.Exists,
            "not_exists" => PathCheck.NotExists,
            "count_min" => PathCheck.CountMin,
            "count_max" => PathCheck.CountMax,
            var c => throw new JsonException($"Unknown PathCheck: {c}")
        };

        var steps = new List<PathStep>();
        foreach (var step in root.GetProperty("path").EnumerateArray())
        {
            // "via" can be a string or array of strings
            IReadOnlyList<string> via;
            var viaProp = step.GetProperty("via");
            if (viaProp.ValueKind == JsonValueKind.Array)
                via = ConditionConverter.ReadStringArray(viaProp);
            else
                via = [viaProp.GetString()!];

            var direction = step.TryGetProperty("direction", out var d)
                ? PathConstraintConverter.ParsePathDirection(d.GetString()!)
                : PathDirection.Any;

            IReadOnlyList<SelectionFilter> filters = step.TryGetProperty("filters", out var f)
                ? SelectionFilterConverter.ReadFilterArray(f, options)
                : [];

            steps.Add(new PathStep(
                via,
                direction,
                step.TryGetProperty("targetKind", out var tk) ? tk.GetString()! : "",
                step.TryGetProperty("targetSubtype", out var ts) ? ts.GetString()! : "",
                step.TryGetProperty("targetStatus", out var tst) ? tst.GetString()! : "",
                filters,
                step.TryGetProperty("as", out var asVal) ? asVal.GetString()! : ""));
        }

        IReadOnlyList<PathConstraint> where = root.TryGetProperty("where", out var whereProp)
            ? ReadConstraintArray(whereProp)
            : [];

        return new GraphPathAssertion(
            check,
            steps,
            root.TryGetProperty("count", out var count) ? count.GetInt32() : 0,
            where);
    }

    private static IReadOnlyList<PathConstraint> ReadConstraintArray(JsonElement arr)
    {
        var list = new List<PathConstraint>();
        foreach (var item in arr.EnumerateArray())
            list.Add(PathConstraintConverter.ReadConstraint(item));
        return list;
    }
}

// =============================================================================
// ERA TRANSITION EFFECTS CONVERTER
// =============================================================================

/// <summary>
/// Deserializes EraTransitionEffects from JSON.
/// JSON shape: { "mutations": [ { "type": "modify_pressure", ... }, ... ] }
/// </summary>
public class EraTransitionEffectsConverter : JsonConverter<EraTransitionEffects>
{
    public override EraTransitionEffects Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("mutations", out var mutations))
            return EraTransitionEffects.Empty;

        var list = new List<ModifyPressureMutation>();
        foreach (var item in mutations.EnumerateArray())
        {
            var mut = MutationConverter.ReadMutation(item, options);
            if (mut is ModifyPressureMutation mp)
                list.Add(mp);
        }
        return new EraTransitionEffects(list);
    }

    public override void Write(Utf8JsonWriter writer, EraTransitionEffects value, JsonSerializerOptions options) =>
        throw new NotSupportedException("EraTransitionEffects serialization is not supported");
}

// =============================================================================
// SYSTEM TYPE CONVERTER
// =============================================================================

/// <summary>
/// Deserializes SystemType from camelCase JSON strings.
/// </summary>
public class SystemTypeConverter : JsonConverter<SystemType>
{
    public override SystemType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString()!;
        return value switch
        {
            "connectionEvolution" => SystemType.ConnectionEvolution,
            "graphContagion" => SystemType.GraphContagion,
            "thresholdTrigger" => SystemType.ThresholdTrigger,
            "clusterFormation" => SystemType.ClusterFormation,
            "tagDiffusion" => SystemType.TagDiffusion,
            "planeDiffusion" => SystemType.PlaneDiffusion,
            "eraSpawner" => SystemType.EraSpawner,
            "eraTransition" => SystemType.EraTransition,
            "universalCatalyst" => SystemType.UniversalCatalyst,
            "relationshipMaintenance" => SystemType.RelationshipMaintenance,
            "growth" => SystemType.Growth,
            _ => throw new JsonException($"Unknown SystemType: {value}")
        };
    }

    public override void Write(Utf8JsonWriter writer, SystemType value, JsonSerializerOptions options)
    {
        var str = value switch
        {
            SystemType.ConnectionEvolution => "connectionEvolution",
            SystemType.GraphContagion => "graphContagion",
            SystemType.ThresholdTrigger => "thresholdTrigger",
            SystemType.ClusterFormation => "clusterFormation",
            SystemType.TagDiffusion => "tagDiffusion",
            SystemType.PlaneDiffusion => "planeDiffusion",
            SystemType.EraSpawner => "eraSpawner",
            SystemType.EraTransition => "eraTransition",
            SystemType.UniversalCatalyst => "universalCatalyst",
            SystemType.RelationshipMaintenance => "relationshipMaintenance",
            SystemType.Growth => "growth",
            _ => throw new JsonException($"Cannot serialize SystemType: {value}")
        };
        writer.WriteStringValue(str);
    }
}
