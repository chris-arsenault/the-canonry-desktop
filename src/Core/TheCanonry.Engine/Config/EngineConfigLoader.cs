namespace TheCanonry.Engine.Config;

using System.Text.Json;
using System.Text.Json.Serialization;
using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Pressures;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Engine.Templates;
using TheCanonry.Schema.Json;

/// <summary>
/// Loads engine configuration files (eras, generators, systems, pressures) from a
/// domain directory. Each JSON file is deserialized into the corresponding C# types.
/// </summary>
public static class EngineConfigLoader
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        // Re-use nominal value converters from DomainJsonOptions
        options.Converters.Add(new EraIdConverter());

        // Engine-specific polymorphic converters
        options.Converters.Add(new ConditionConverter());
        options.Converters.Add(new SelectionFilterConverter());
        options.Converters.Add(new MutationConverter());
        options.Converters.Add(new MetricConverter());
        options.Converters.Add(new SubtypeSpecConverter());
        options.Converters.Add(new PlacementAnchorConverter());
        options.Converters.Add(new CultureSpecConverter());
        options.Converters.Add(new DescriptionSpecConverter());
        options.Converters.Add(new VariableSelectionSourceConverter());
        options.Converters.Add(new PathConstraintConverter());
        options.Converters.Add(new EraTransitionEffectsConverter());
        options.Converters.Add(new SystemTypeConverter());

        // Enum converters — JSON uses camelCase strings
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }

    // =========================================================================
    // ERAS
    // =========================================================================

    public static IReadOnlyList<Era> LoadEras(string domainDir)
    {
        var json = ReadFile(domainDir, "eras.json");
        using var doc = JsonDocument.Parse(json);

        var eras = new List<Era>();
        foreach (var e in doc.RootElement.EnumerateArray())
        {
            eras.Add(ReadEra(e));
        }
        return eras;
    }

    private static Era ReadEra(JsonElement e)
    {
        var templateWeights = ReadStringDoubleDict(e, "templateWeights");
        var systemModifiers = ReadStringDoubleDict(e, "systemModifiers");
        var pressureModifiers = ReadStringDoubleDict(e, "pressureModifiers");

        var exitConditions = e.TryGetProperty("exitConditions", out var ec)
            ? ConditionConverter.ReadConditionArray(ec, Options)
            : [];

        var entryConditions = e.TryGetProperty("entryConditions", out var enc)
            ? ConditionConverter.ReadConditionArray(enc, Options)
            : [];

        var exitEffects = e.TryGetProperty("exitEffects", out var ee)
            ? ReadTransitionEffects(ee)
            : EraTransitionEffects.Empty;

        var entryEffects = e.TryGetProperty("entryEffects", out var ene)
            ? ReadTransitionEffects(ene)
            : EraTransitionEffects.Empty;

        var nextEra = e.TryGetProperty("nextEra", out var ne)
            ? new Schema.Ids.EraId(ne.GetString()!)
            : (Schema.Ids.EraId?)null;

        return new Era(
            new Schema.Ids.EraId(e.GetProperty("id").GetString()!),
            e.GetProperty("name").GetString()!,
            e.TryGetProperty("summary", out var sum) ? sum.GetString()! : "",
            templateWeights,
            systemModifiers,
            pressureModifiers,
            exitConditions,
            entryConditions,
            nextEra,
            exitEffects,
            entryEffects);
    }

    private static EraTransitionEffects ReadTransitionEffects(JsonElement e)
    {
        if (!e.TryGetProperty("mutations", out var mutations))
            return EraTransitionEffects.Empty;

        var list = new List<ModifyPressureMutation>();
        foreach (var item in mutations.EnumerateArray())
        {
            var mut = MutationConverter.ReadMutation(item, Options);
            if (mut is ModifyPressureMutation mp)
                list.Add(mp);
        }
        return new EraTransitionEffects(list);
    }

    // =========================================================================
    // TEMPLATES (generators.json)
    // =========================================================================

    public static IReadOnlyList<DeclarativeTemplate> LoadTemplates(string domainDir)
    {
        var json = ReadFile(domainDir, "generators.json");
        using var doc = JsonDocument.Parse(json);

        var templates = new List<DeclarativeTemplate>();
        foreach (var e in doc.RootElement.EnumerateArray())
        {
            templates.Add(ReadTemplate(e));
        }
        return templates;
    }

    private static DeclarativeTemplate ReadTemplate(JsonElement e)
    {
        var applicability = e.TryGetProperty("applicability", out var app)
            ? ConditionConverter.ReadConditionArray(app, Options)
            : [];

        var selection = e.TryGetProperty("selection", out var sel)
            ? ReadSelectionRule(sel)
            : new SelectionRule(
                SelectionStrategy.ByKind, "", [], [], [], "", [], "", "", false,
                Direction.Both, [], "", 0, "", [], [], SelectionPickStrategy.Random, 0);

        var creation = e.TryGetProperty("creation", out var cr)
            ? ReadCreationRules(cr)
            : [];

        var relationships = e.TryGetProperty("relationships", out var rels)
            ? ReadRelationshipRules(rels)
            : [];

        var stateUpdates = e.TryGetProperty("stateUpdates", out var su)
            ? MutationConverter.ReadMutationArray(su, Options)
            : [];

        var variables = e.TryGetProperty("variables", out var vars)
            ? ReadVariables(vars)
            : new Dictionary<string, VariableDefinition>();

        var variants = e.TryGetProperty("variants", out var v)
            ? ReadVariants(v)
            : new TemplateVariants(VariantSelectionMode.FirstMatch, []);

        return new DeclarativeTemplate(
            e.GetProperty("id").GetString()!,
            e.GetProperty("name").GetString()!,
            !e.TryGetProperty("enabled", out var en) || en.GetBoolean(),
            applicability,
            selection,
            creation,
            relationships,
            stateUpdates,
            variables,
            variants,
            e.TryGetProperty("narrationTemplate", out var nt) ? nt.GetString()! : "");
    }

    // =========================================================================
    // SYSTEMS
    // =========================================================================

    public static IReadOnlyList<DeclarativeSystem> LoadSystems(string domainDir)
    {
        var json = ReadFile(domainDir, "systems.json");
        using var doc = JsonDocument.Parse(json);

        var systems = new List<DeclarativeSystem>();
        foreach (var e in doc.RootElement.EnumerateArray())
        {
            systems.Add(ReadSystem(e));
        }
        return systems;
    }

    private static DeclarativeSystem ReadSystem(JsonElement e)
    {
        var systemTypeStr = e.GetProperty("systemType").GetString()!;
        var systemType = systemTypeStr switch
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
            _ => throw new JsonException($"Unknown SystemType: {systemTypeStr}")
        };

        var enabled = !e.TryGetProperty("enabled", out var en) || en.GetBoolean();

        // Config is stored as a raw dictionary — the system interpreter
        // reads type-specific fields from it.
        var config = e.TryGetProperty("config", out var cfg)
            ? ReadObjectDict(cfg)
            : new Dictionary<string, object?>();

        return new DeclarativeSystem(systemType, enabled, config);
    }

    // =========================================================================
    // PRESSURES
    // =========================================================================

    public static IReadOnlyList<DeclarativePressure> LoadPressures(string domainDir)
    {
        var json = ReadFile(domainDir, "pressures.json");
        using var doc = JsonDocument.Parse(json);

        var pressures = new List<DeclarativePressure>();
        foreach (var e in doc.RootElement.EnumerateArray())
        {
            pressures.Add(ReadPressure(e));
        }
        return pressures;
    }

    private static DeclarativePressure ReadPressure(JsonElement e)
    {
        var growth = e.TryGetProperty("growth", out var g)
            ? ReadPressureGrowth(g)
            : new PressureGrowth([], []);

        var contract = e.TryGetProperty("contract", out var c)
            ? ReadPressureContract(c)
            : new PressureContract([], [], [], new PressureEquilibrium(0, 0, 0, 0));

        return new DeclarativePressure(
            e.GetProperty("id").GetString()!,
            e.GetProperty("name").GetString()!,
            e.TryGetProperty("description", out var desc) ? desc.GetString()! : "",
            e.TryGetProperty("initialValue", out var iv) ? iv.GetDouble() : 0,
            e.TryGetProperty("homeostasis", out var h) ? h.GetDouble() : 0,
            growth,
            contract);
    }

    private static PressureGrowth ReadPressureGrowth(JsonElement g)
    {
        var positive = g.TryGetProperty("positiveFeedback", out var pf)
            ? ReadMetricArray(pf)
            : [];

        var negative = g.TryGetProperty("negativeFeedback", out var nf)
            ? ReadMetricArray(nf)
            : [];

        return new PressureGrowth(positive, negative);
    }

    private static IReadOnlyList<Metric> ReadMetricArray(JsonElement arr)
    {
        var list = new List<Metric>();
        foreach (var item in arr.EnumerateArray())
            list.Add(MetricConverter.ReadMetric(item, Options));
        return list;
    }

    private static PressureContract ReadPressureContract(JsonElement c)
    {
        var sources = new List<PressureSource>();
        if (c.TryGetProperty("sources", out var s))
        {
            foreach (var item in s.EnumerateArray())
                sources.Add(new PressureSource(
                    item.GetProperty("component").GetString()!,
                    item.GetProperty("delta").GetDouble(),
                    item.TryGetProperty("formula", out var f) ? f.GetString()! : ""));
        }

        var sinks = new List<PressureSink>();
        if (c.TryGetProperty("sinks", out var sk))
        {
            foreach (var item in sk.EnumerateArray())
                sinks.Add(new PressureSink(
                    item.GetProperty("component").GetString()!,
                    item.GetProperty("delta").GetDouble(),
                    item.TryGetProperty("formula", out var f) ? f.GetString()! : ""));
        }

        var affects = new List<PressureAffect>();
        if (c.TryGetProperty("affects", out var af))
        {
            foreach (var item in af.EnumerateArray())
                affects.Add(new PressureAffect(
                    item.GetProperty("component").GetString()!,
                    item.GetProperty("effect").GetString()!,
                    item.GetProperty("threshold").GetDouble(),
                    item.GetProperty("factor").GetDouble()));
        }

        var eq = c.TryGetProperty("equilibrium", out var eqProp)
            ? new PressureEquilibrium(
                eqProp.GetProperty("expectedMin").GetDouble(),
                eqProp.GetProperty("expectedMax").GetDouble(),
                eqProp.GetProperty("restingPoint").GetDouble(),
                eqProp.GetProperty("oscillationPeriod").GetInt32())
            : new PressureEquilibrium(0, 0, 0, 0);

        return new PressureContract(sources, sinks, affects, eq);
    }

    // =========================================================================
    // SELECTION RULE READER
    // =========================================================================

    private static SelectionRule ReadSelectionRule(JsonElement e)
    {
        var strategy = e.TryGetProperty("strategy", out var strat)
            ? strat.GetString()! switch
            {
                "by_kind" => SelectionStrategy.ByKind,
                "by_preference_order" => SelectionStrategy.ByPreferenceOrder,
                "by_relationship" => SelectionStrategy.ByRelationship,
                "by_proximity" => SelectionStrategy.ByProximity,
                "by_prominence" => SelectionStrategy.ByProminence,
                var s => throw new JsonException($"Unknown SelectionStrategy: {s}")
            }
            : SelectionStrategy.ByKind;

        var kind = e.TryGetProperty("kind", out var k) ? k.GetString()! : "";
        var kinds = e.TryGetProperty("kinds", out var ks) ? ConditionConverter.ReadStringArray(ks) : [];
        var subtypes = e.TryGetProperty("subtypes", out var st) ? ConditionConverter.ReadStringArray(st) : [];
        var excludeSubtypes = e.TryGetProperty("excludeSubtypes", out var es) ? ConditionConverter.ReadStringArray(es) : [];
        var status = e.TryGetProperty("status", out var s2) ? s2.GetString()! : "";
        var statuses = e.TryGetProperty("statuses", out var sts) ? ConditionConverter.ReadStringArray(sts) : [];
        var notStatus = e.TryGetProperty("notStatus", out var ns) ? ns.GetString()! : "";
        var relationshipKind = e.TryGetProperty("relationshipKind", out var rk) ? rk.GetString()! : "";
        var mustHave = e.TryGetProperty("mustHave", out var mh) && mh.GetBoolean();
        var direction = e.TryGetProperty("direction", out var d) ? ConditionConverter.ParseDirection(d.GetString()!) : Direction.Both;
        var subtypePreferences = e.TryGetProperty("subtypePreferences", out var sp) ? ConditionConverter.ReadStringArray(sp) : [];
        var referenceEntity = e.TryGetProperty("referenceEntity", out var re) ? re.GetString()! : "";
        var maxDistance = e.TryGetProperty("maxDistance", out var md) ? md.GetDouble() : 0;
        var minProminence = e.TryGetProperty("minProminence", out var mp) ? mp.GetString()! : "";
        var filters = e.TryGetProperty("filters", out var f)
            ? SelectionFilterConverter.ReadFilterArray(f, Options)
            : [];
        var saturationLimits = e.TryGetProperty("saturationLimits", out var sl)
            ? ReadSaturationLimits(sl)
            : [];

        var pickStrategy = e.TryGetProperty("pickStrategy", out var ps)
            ? ps.GetString()! switch
            {
                "random" => SelectionPickStrategy.Random,
                "first" => SelectionPickStrategy.First,
                "all" => SelectionPickStrategy.All,
                "weighted" => SelectionPickStrategy.Weighted,
                var p => throw new JsonException($"Unknown SelectionPickStrategy: {p}")
            }
            : SelectionPickStrategy.Random;

        var maxResults = e.TryGetProperty("maxResults", out var mr) ? mr.GetInt32() : 0;

        return new SelectionRule(
            strategy, kind, kinds, subtypes, excludeSubtypes,
            status, statuses, notStatus, relationshipKind, mustHave,
            direction, subtypePreferences, referenceEntity, maxDistance,
            minProminence, filters, saturationLimits, pickStrategy, maxResults);
    }

    private static IReadOnlyList<SaturationLimit> ReadSaturationLimits(JsonElement arr)
    {
        var list = new List<SaturationLimit>();
        foreach (var item in arr.EnumerateArray())
        {
            list.Add(new SaturationLimit(
                item.GetProperty("relationshipKind").GetString()!,
                item.TryGetProperty("direction", out var d) ? ConditionConverter.ParseDirection(d.GetString()!) : Direction.Both,
                item.TryGetProperty("fromKind", out var fk) ? fk.GetString()! : "",
                item.TryGetProperty("fromSubtype", out var fs) ? fs.GetString()! : "",
                item.TryGetProperty("maxCount", out var mc) ? mc.GetInt32() : int.MaxValue));
        }
        return list;
    }

    // =========================================================================
    // CREATION RULE READER
    // =========================================================================

    private static IReadOnlyList<CreationRule> ReadCreationRules(JsonElement arr)
    {
        var list = new List<CreationRule>();
        foreach (var item in arr.EnumerateArray())
            list.Add(ReadCreationRule(item));
        return list;
    }

    private static CreationRule ReadCreationRule(JsonElement e)
    {
        var subtype = e.TryGetProperty("subtype", out var st)
            ? SubtypeSpecConverter.ReadSubtypeSpec(st, Options)
            : new FixedSubtype("");

        var culture = e.TryGetProperty("culture", out var cu)
            ? CultureSpecConverter.ReadCultureSpec(cu)
            : new InheritCulture("$target");

        var description = e.TryGetProperty("description", out var desc)
            ? DescriptionSpecConverter.ReadDescriptionSpec(desc)
            : new FixedDescription("");

        var tags = e.TryGetProperty("tags", out var t)
            ? ReadBoolDict(t)
            : new Dictionary<string, bool>();

        var placement = e.TryGetProperty("placement", out var pl)
            ? ReadPlacementSpec(pl)
            : new PlacementSpec(new SparseAnchor(false), new PlacementSpacing(0, []), new PlacementRegionPolicy(false, false, false), []);

        var count = e.TryGetProperty("count", out var c)
            ? ReadCountRange(c)
            : new CountRange(1, 1);

        var createChance = e.TryGetProperty("createChance", out var cc) ? cc.GetDouble() : 1.0;

        return new CreationRule(
            e.TryGetProperty("entityRef", out var er) ? er.GetString()! : "",
            e.TryGetProperty("kind", out var k) ? k.GetString()! : "",
            subtype,
            e.TryGetProperty("status", out var s) ? s.GetString()! : "",
            e.TryGetProperty("prominence", out var p) ? p.GetString()! : "",
            culture,
            description,
            tags,
            placement,
            count,
            createChance);
    }

    private static PlacementSpec ReadPlacementSpec(JsonElement e)
    {
        var anchor = e.TryGetProperty("anchor", out var a)
            ? PlacementAnchorConverter.ReadAnchor(a)
            : new SparseAnchor(false);

        var spacing = e.TryGetProperty("spacing", out var sp)
            ? new PlacementSpacing(
                sp.TryGetProperty("minDistance", out var md) ? md.GetDouble() : 0,
                sp.TryGetProperty("avoidRefs", out var ar) ? ConditionConverter.ReadStringArray(ar) : [])
            : new PlacementSpacing(0, []);

        var regionPolicy = e.TryGetProperty("regionPolicy", out var rp)
            ? new PlacementRegionPolicy(
                rp.TryGetProperty("allowEmergent", out var ae) && ae.GetBoolean(),
                rp.TryGetProperty("createRegion", out var cr) && cr.GetBoolean(),
                rp.TryGetProperty("preferSparse", out var ps) && ps.GetBoolean())
            : new PlacementRegionPolicy(false, false, false);

        var steps = e.TryGetProperty("steps", out var st)
            ? ReadPlacementSteps(st)
            : [];

        return new PlacementSpec(anchor, spacing, regionPolicy, steps);
    }

    private static IReadOnlyList<PlacementStep> ReadPlacementSteps(JsonElement arr)
    {
        var list = new List<PlacementStep>();
        foreach (var item in arr.EnumerateArray())
        {
            var step = item.GetString()! switch
            {
                "anchor_region" => PlacementStep.AnchorRegion,
                "seed_region" => PlacementStep.SeedRegion,
                "sparse" => PlacementStep.Sparse,
                "random" => PlacementStep.Random,
                var s => throw new JsonException($"Unknown PlacementStep: {s}")
            };
            list.Add(step);
        }
        return list;
    }

    private static CountRange ReadCountRange(JsonElement e)
    {
        if (e.ValueKind == JsonValueKind.Number)
        {
            var n = e.GetInt32();
            return new CountRange(n, n);
        }
        return new CountRange(
            e.GetProperty("min").GetInt32(),
            e.GetProperty("max").GetInt32());
    }

    // =========================================================================
    // RELATIONSHIP RULE READER
    // =========================================================================

    private static IReadOnlyList<RelationshipRule> ReadRelationshipRules(JsonElement arr)
    {
        var list = new List<RelationshipRule>();
        foreach (var item in arr.EnumerateArray())
        {
            list.Add(new RelationshipRule(
                item.GetProperty("kind").GetString()!,
                item.TryGetProperty("src", out var s) ? s.GetString()! : "",
                item.TryGetProperty("dst", out var d) ? d.GetString()! : "",
                item.TryGetProperty("strength", out var str) ? str.GetDouble() : 0.5,
                item.TryGetProperty("bidirectional", out var bi) && bi.GetBoolean(),
                item.TryGetProperty("catalyzedBy", out var cb) ? cb.GetString()! : "",
                item.TryGetProperty("condition", out var c) ? ConditionConverter.ReadCondition(c, Options) : null));
        }
        return list;
    }

    // =========================================================================
    // VARIABLE DEFINITION READER
    // =========================================================================

    private static IReadOnlyDictionary<string, VariableDefinition> ReadVariables(JsonElement e)
    {
        var dict = new Dictionary<string, VariableDefinition>();
        foreach (var prop in e.EnumerateObject())
        {
            var varDef = prop.Value;
            var selectEl = varDef.GetProperty("select");
            var from = selectEl.TryGetProperty("from", out var fromEl)
                ? VariableSelectionSourceConverter.ReadSource(fromEl)
                : new GraphSource();

            var kind = selectEl.TryGetProperty("kind", out var k) ? k.GetString()! : "";
            var kinds = selectEl.TryGetProperty("kinds", out var ks) ? ConditionConverter.ReadStringArray(ks) : [];
            var subtypes = selectEl.TryGetProperty("subtypes", out var st) ? ConditionConverter.ReadStringArray(st) : [];
            var status = selectEl.TryGetProperty("status", out var s) ? s.GetString()! : "";
            var statuses = selectEl.TryGetProperty("statuses", out var sts) ? ConditionConverter.ReadStringArray(sts) : [];
            var notStatus = selectEl.TryGetProperty("notStatus", out var ns) ? ns.GetString()! : "";
            var filters = selectEl.TryGetProperty("filters", out var f) ? SelectionFilterConverter.ReadFilterArray(f, Options) : [];
            var preferFilters = selectEl.TryGetProperty("preferFilters", out var pf) ? SelectionFilterConverter.ReadFilterArray(pf, Options) : [];

            var pickStrategy = selectEl.TryGetProperty("pickStrategy", out var ps)
                ? ps.GetString()! switch
                {
                    "random" => SelectionPickStrategy.Random,
                    "first" => SelectionPickStrategy.First,
                    "all" => SelectionPickStrategy.All,
                    "weighted" => SelectionPickStrategy.Weighted,
                    var p => throw new JsonException($"Unknown SelectionPickStrategy: {p}")
                }
                : SelectionPickStrategy.Random;

            var maxResults = selectEl.TryGetProperty("maxResults", out var mr) ? mr.GetInt32() : 0;

            var selectionRule = new VariableSelectionRule(
                from, kind, kinds, subtypes, status, statuses, notStatus,
                filters, preferFilters, pickStrategy, maxResults);

            var required = varDef.TryGetProperty("required", out var req) && req.GetBoolean();
            dict[prop.Name] = new VariableDefinition(selectionRule, required);
        }
        return dict;
    }

    // =========================================================================
    // VARIANT READER
    // =========================================================================

    private static TemplateVariants ReadVariants(JsonElement e)
    {
        var selection = e.TryGetProperty("selection", out var sel)
            ? sel.GetString()! switch
            {
                "first_match" => VariantSelectionMode.FirstMatch,
                "all_matching" => VariantSelectionMode.AllMatching,
                var s => throw new JsonException($"Unknown VariantSelectionMode: {s}")
            }
            : VariantSelectionMode.FirstMatch;

        var options = new List<TemplateVariant>();
        if (e.TryGetProperty("options", out var opts))
        {
            foreach (var opt in opts.EnumerateArray())
            {
                var when = ConditionConverter.ReadCondition(opt.GetProperty("when"), Options);
                var apply = ReadVariantEffects(opt.GetProperty("apply"));
                options.Add(new TemplateVariant(
                    opt.TryGetProperty("name", out var n) ? n.GetString()! : "",
                    when,
                    apply));
            }
        }

        return new TemplateVariants(selection, options);
    }

    private static VariantEffects ReadVariantEffects(JsonElement e)
    {
        var subtype = new Dictionary<string, string>();
        if (e.TryGetProperty("subtype", out var st))
        {
            foreach (var prop in st.EnumerateObject())
                subtype[prop.Name] = prop.Value.GetString()!;
        }

        var tags = new Dictionary<string, IReadOnlyDictionary<string, bool>>();
        if (e.TryGetProperty("tags", out var t))
        {
            foreach (var prop in t.EnumerateObject())
                tags[prop.Name] = ReadBoolDict(prop.Value);
        }

        var relationships = e.TryGetProperty("relationships", out var rels)
            ? ReadRelationshipRules(rels)
            : [];

        var stateUpdates = e.TryGetProperty("stateUpdates", out var su)
            ? MutationConverter.ReadMutationArray(su, Options)
            : [];

        return new VariantEffects(subtype, tags, relationships, stateUpdates);
    }

    // =========================================================================
    // UTILITY READERS
    // =========================================================================

    private static string ReadFile(string dir, string filename)
    {
        var path = Path.Combine(dir, filename);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Domain config file not found: {path}", path);
        return File.ReadAllText(path);
    }

    private static IReadOnlyDictionary<string, double> ReadStringDoubleDict(JsonElement parent, string propertyName)
    {
        var dict = new Dictionary<string, double>();
        if (!parent.TryGetProperty(propertyName, out var prop))
            return dict;

        foreach (var item in prop.EnumerateObject())
            dict[item.Name] = item.Value.GetDouble();
        return dict;
    }

    private static IReadOnlyDictionary<string, bool> ReadBoolDict(JsonElement e)
    {
        var dict = new Dictionary<string, bool>();
        foreach (var prop in e.EnumerateObject())
            dict[prop.Name] = prop.Value.GetBoolean();
        return dict;
    }

    /// <summary>
    /// Reads a JSON object into a dictionary. Nested objects and arrays are
    /// preserved as JsonElement values so the system interpreter can drill in.
    /// </summary>
    private static IReadOnlyDictionary<string, object?> ReadObjectDict(JsonElement e)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in e.EnumerateObject())
        {
            dict[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString(),
                JsonValueKind.Number => prop.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                // Preserve complex values as cloned JsonElement for downstream reading
                _ => prop.Value.Clone()
            };
        }
        return dict;
    }
}
