using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Graph;
using TheCanonry.Engine.Rules;
using TheCanonry.Engine.Rules.Types;
using TheCanonry.Engine.Runtime;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.Primitives;
using TheCanonry.Schema.World;
using ExecutionContext = TheCanonry.Schema.World.ExecutionContext;

namespace TheCanonry.Engine.Templates;

/// <summary>
/// Interprets and executes declarative templates.
/// Converts DeclarativeTemplate configs + a target entity into concrete
/// entities and relationships. This is the core of the growth phase.
///
/// The 7-phase execution pipeline:
/// 1. Applicability check
/// 2. Variable resolution
/// 3. Selection (handled externally; target is passed in)
/// 4. Entity creation
/// 5. Relationship creation
/// 6. State updates
/// 7. Variant processing
/// </summary>
public static class TemplateInterpreter
{
    /// <summary>
    /// Execute a template against a target entity and return the result.
    /// The target entity should already be selected by the caller (growth system).
    /// </summary>
    public static TemplateResult Execute(
        DeclarativeTemplate template,
        Entity target,
        WorldRuntime runtime)
    {
        // Need the underlying graph for RuleContext creation.
        // WorldRuntime wraps an IGraph; for now we create a GraphAdapter
        // that delegates to the runtime. This is needed because RuleContext
        // requires an IGraph reference.
        var graph = new RuntimeGraphAdapter(runtime);
        var context = new TemplateExecutionContext(runtime, graph)
        {
            Target = target,
            TemplateId = template.Id
        };
        context.SetVariable("$target", target);

        // Phase 1: Applicability check
        if (!EvaluateApplicability(template.Applicability, context))
            return TemplateResult.Empty;

        // Phase 2: Variable resolution
        if (!ResolveVariables(template.Variables, context))
            return TemplateResult.Empty;

        // Phase 3: Selection is handled externally (target already selected)

        // Phase 4: Entity creation
        var entities = new List<Entity>();
        var placementStrategies = new List<string>();
        var derivedTagsList = new List<IReadOnlyDictionary<string, object>>();
        var placementDebugList = new List<PlacementDebug>();

        foreach (var rule in template.Creation)
        {
            var startIndex = entities.Count;
            var created = ExecuteCreation(rule, context, startIndex, template.Id);
            entities.AddRange(created);

            // Track in context for relationship resolution
            for (var i = 0; i < created.Count; i++)
            {
                context.TrackCreatedEntity(rule.EntityRef, created[i], startIndex + i);
                placementStrategies.Add(GetPlacementStrategy(rule.Placement));
                derivedTagsList.Add(new Dictionary<string, object>());
                placementDebugList.Add(CreateEmptyPlacementDebug());
            }
        }

        // Phase 5: Relationship creation
        var relationships = new List<Relationship>();
        foreach (var rule in template.Relationships)
        {
            var created = ExecuteRelationship(rule, context);
            relationships.AddRange(created);
        }

        // Phase 6: State updates
        foreach (var mutation in template.StateUpdates)
        {
            ExecuteStateUpdate(mutation, context);
        }

        // Phase 7: Variant processing
        var matchingVariants = GetMatchingVariants(template.Variants, context);
        foreach (var variant in matchingVariants)
        {
            ApplyVariantEffects(variant.Apply, entities, relationships, context);
        }

        // Build resolved variables for narration
        var resolvedVariables = new Dictionary<string, object?>();
        foreach (var (key, entity) in context.Variables)
        {
            resolvedVariables[$"${key}"] = entity;
        }
        resolvedVariables["$target"] = target;

        return new TemplateResult(
            Entities: entities,
            Relationships: relationships,
            Description: $"{template.Name} executed",
            PlacementStrategies: placementStrategies,
            DerivedTagsList: derivedTagsList,
            PlacementDebugList: placementDebugList,
            ResolvedVariables: resolvedVariables,
            EntityRefToIndex: new Dictionary<string, int>(context.EntityRefToIndex));
    }

    // =========================================================================
    // PHASE 1: APPLICABILITY
    // =========================================================================

    private static bool EvaluateApplicability(
        IReadOnlyList<Condition> rules,
        TemplateExecutionContext context)
    {
        if (rules.Count == 0) return true;

        var ruleCtx = context.CreateRuleContext();
        foreach (var condition in rules)
        {
            if (!ConditionEvaluator.Evaluate(condition, ruleCtx))
                return false;
        }
        return true;
    }

    // =========================================================================
    // PHASE 2: VARIABLE RESOLUTION
    // =========================================================================

    /// <summary>
    /// Resolve all template variables. Returns false if any required variable
    /// fails to resolve (template should not execute).
    /// </summary>
    private static bool ResolveVariables(
        IReadOnlyDictionary<string, VariableDefinition> variables,
        TemplateExecutionContext context)
    {
        if (variables.Count == 0) return true;

        var ruleCtx = context.CreateRuleContext();

        foreach (var (name, definition) in variables)
        {
            var resolved = ResolveVariable(definition, ruleCtx);

            if (resolved is not null)
            {
                context.SetVariable(name, resolved);
            }
            else if (definition.Required)
            {
                // Required variable could not be resolved; template cannot run
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Resolve a single variable definition to an entity.
    /// Uses the variable's selection rule to find matching entities.
    /// </summary>
    private static Entity? ResolveVariable(
        VariableDefinition definition,
        RuleContext ruleCtx)
    {
        var selectRule = definition.Select;

        // Determine base candidates based on source
        IReadOnlyList<Entity> candidates = GetVariableCandidates(selectRule, ruleCtx);
        if (candidates.Count == 0) return null;

        // Apply kind/subtype/status filters
        candidates = ApplyBasicFilters(candidates, selectRule);
        if (candidates.Count == 0) return null;

        // Apply selection filters
        if (selectRule.Filters.Count > 0)
        {
            candidates = FilterEvaluator.ApplyFilters(candidates, selectRule.Filters, ruleCtx);
            if (candidates.Count == 0) return null;
        }

        // Apply prefer filters (try preferred first, fall back to all candidates)
        if (selectRule.PreferFilters.Count > 0)
        {
            var preferred = FilterEvaluator.ApplyFilters(candidates, selectRule.PreferFilters, ruleCtx);
            if (preferred.Count > 0) candidates = preferred;
        }

        // Pick based on strategy
        return selectRule.PickStrategy switch
        {
            SelectionPickStrategy.First => candidates[0],
            SelectionPickStrategy.Random => candidates[Random.Shared.Next(candidates.Count)],
            _ => candidates[0]
        };
    }

    private static List<Entity> GetVariableCandidates(
        VariableSelectionRule selectRule,
        RuleContext ruleCtx)
    {
        switch (selectRule.From)
        {
            case GraphSource:
            {
                // Select from the full graph
                EntityKind? kind = string.IsNullOrEmpty(selectRule.Kind) ? null : new EntityKind(selectRule.Kind);
                var criteria = EntityCriteria.Create(kind: kind);
                var entities = ruleCtx.Graph.FindEntities(criteria);
                return new List<Entity>(entities);
            }

            case RelatedSource related:
            {
                // Select from entities related to a reference entity
                var refEntity = ConditionEvaluator.ResolveEntityRef(related.Spec.RelatedTo, ruleCtx, ruleCtx.Self);
                if (refEntity is null) return [];

                var relKind = new RelationshipKind(related.Spec.RelationshipKind);
                var connected = ruleCtx.Graph.GetConnectedEntities(
                    refEntity.Id, relKind, related.Spec.Direction);
                return new List<Entity>(connected);
            }

            case PathSource path:
            {
                // Multi-hop path traversal
                return TraversePath(path.Spec, ruleCtx);
            }

            default:
                return [];
        }
    }

    private static List<Entity> TraversePath(PathBasedSpec spec, RuleContext ruleCtx)
    {
        var current = new List<Entity>();

        foreach (var step in spec.Path)
        {
            List<Entity> stepCandidates;

            if (current.Count == 0)
            {
                // First step: resolve From reference
                var fromEntity = ConditionEvaluator.ResolveEntityRef(step.From, ruleCtx, ruleCtx.Self);
                if (fromEntity is null) return [];

                var relKind = new RelationshipKind(step.Via);
                var connected = ruleCtx.Graph.GetConnectedEntities(fromEntity.Id, relKind, step.Direction);
                stepCandidates = new List<Entity>(connected);
            }
            else
            {
                // Subsequent steps: traverse from all current entities
                stepCandidates = [];
                var relKind = new RelationshipKind(step.Via);
                foreach (var entity in current)
                {
                    var connected = ruleCtx.Graph.GetConnectedEntities(entity.Id, relKind, step.Direction);
                    stepCandidates.AddRange(connected);
                }
            }

            // Filter by kind/subtype/status
            if (!string.IsNullOrEmpty(step.TargetKind))
            {
                var targetKind = new EntityKind(step.TargetKind);
                stepCandidates = stepCandidates.Where(e => e.Kind == targetKind).ToList();
            }
            if (!string.IsNullOrEmpty(step.TargetSubtype))
                stepCandidates = stepCandidates.Where(e => e.Subtype == step.TargetSubtype).ToList();
            if (!string.IsNullOrEmpty(step.TargetStatus))
            {
                var targetStatus = new EntityStatus(step.TargetStatus);
                stepCandidates = stepCandidates.Where(e => e.Status == targetStatus).ToList();
            }

            current = stepCandidates;
            if (current.Count == 0) return [];
        }

        return current;
    }

    private static IReadOnlyList<Entity> ApplyBasicFilters(
        IReadOnlyList<Entity> candidates,
        VariableSelectionRule selectRule)
    {
        var result = candidates;

        // Kind filter (primary)
        if (!string.IsNullOrEmpty(selectRule.Kind))
        {
            var kind = new EntityKind(selectRule.Kind);
            result = result.Where(e => e.Kind == kind).ToList();
        }

        // Kinds filter (any of)
        if (selectRule.Kinds.Count > 0)
        {
            var kindSet = new HashSet<EntityKind>(selectRule.Kinds.Select(k => new EntityKind(k)));
            result = result.Where(e => kindSet.Contains(e.Kind)).ToList();
        }

        // Subtypes filter
        if (selectRule.Subtypes.Count > 0)
        {
            var subtypeSet = new HashSet<string>(selectRule.Subtypes);
            result = result.Where(e => subtypeSet.Contains(e.Subtype)).ToList();
        }

        // Status filter
        if (!string.IsNullOrEmpty(selectRule.Status))
        {
            var status = new EntityStatus(selectRule.Status);
            result = result.Where(e => e.Status == status).ToList();
        }

        // Statuses filter
        if (selectRule.Statuses.Count > 0)
        {
            var statusSet = new HashSet<EntityStatus>(selectRule.Statuses.Select(s => new EntityStatus(s)));
            result = result.Where(e => statusSet.Contains(e.Status)).ToList();
        }

        // NotStatus filter
        if (!string.IsNullOrEmpty(selectRule.NotStatus))
        {
            var notStatus = new EntityStatus(selectRule.NotStatus);
            result = result.Where(e => e.Status != notStatus).ToList();
        }

        return result;
    }

    // =========================================================================
    // PHASE 4: ENTITY CREATION
    // =========================================================================

    private static List<Entity> ExecuteCreation(
        CreationRule rule,
        TemplateExecutionContext context,
        int startIndex,
        string templateId)
    {
        var entities = new List<Entity>();

        // Check createChance
        if (rule.CreateChance < 1.0 && Random.Shared.NextDouble() > rule.CreateChance)
            return entities;

        // Resolve count
        var count = ResolveCount(rule.Count);

        for (var i = 0; i < count; i++)
        {
            var entity = CreateSingleEntity(rule, context, startIndex + i, templateId);
            entities.Add(entity);
        }

        return entities;
    }

    private static Entity CreateSingleEntity(
        CreationRule rule,
        TemplateExecutionContext context,
        int index,
        string templateId)
    {
        var tick = context.Runtime.CurrentTick;
        var subtype = ResolveSubtype(rule.Subtype, context);
        var culture = ResolveCulture(rule.Culture, context);
        var description = ResolveDescription(rule.Description, context);
        var prominenceValue = ProminenceHelper.LabelToThreshold(rule.Prominence) + 0.5;
        var status = string.IsNullOrEmpty(rule.Status)
            ? FrameworkPrimitives.Statuses.Active
            : new EntityStatus(rule.Status);

        var entityId = new EntityId($"{templateId}_{tick}_{index}");
        var executionCtx = new ExecutionContext(
            tick, ExecutionSource.Template, templateId, true, "");

        // Coordinates: use a basic default. Actual placement is handled by the
        // growth system after template expansion; the interpreter creates the
        // entity shell with placeholder coordinates.
        var coordinates = ResolveCoordinates(rule.Placement, context);

        var entity = new Entity(
            id: entityId,
            kind: new EntityKind(rule.Kind),
            subtype: subtype,
            name: "[unnamed]",
            culture: new CultureId(culture),
            eraId: context.Runtime.CurrentEra?.Id ?? new EraId(""),
            coordinates: coordinates,
            createdBy: executionCtx,
            tick: tick);

        // Set status if different from default
        if (status != FrameworkPrimitives.Statuses.Active)
            entity.UpdateStatus(status, tick);

        // Set prominence
        entity.SetProminence(new Prominence(prominenceValue), tick);

        // Set description
        if (!string.IsNullOrEmpty(description))
            entity.SetDescription(description, tick);

        // Apply tags
        foreach (var (tag, value) in rule.Tags)
        {
            entity.Tags.Set(tag, value);
        }

        return entity;
    }

    private static int ResolveCount(CountRange count)
    {
        if (count.Min == count.Max) return count.Min;
        return count.Min + Random.Shared.Next(count.Max - count.Min + 1);
    }

    private static string ResolveSubtype(
        SubtypeSpec spec,
        TemplateExecutionContext context)
    {
        switch (spec)
        {
            case FixedSubtype f:
                return f.Value;

            case InheritSubtype i:
            {
                var refEntity = context.ResolveEntity(i.Ref);
                if (refEntity is not null && (i.Chance >= 1.0 || Random.Shared.NextDouble() < i.Chance))
                    return refEntity.Subtype;
                throw new InvalidOperationException(
                    $"Subtype inheritance failed: could not resolve reference '{i.Ref}'.");
            }

            case FromPressureSubtype fp:
            {
                var maxPressure = double.MinValue;
                var selectedSubtype = "";
                foreach (var (pressureId, subtype) in fp.PressureMapping)
                {
                    var value = context.Runtime.GetPressure(pressureId);
                    if (value > maxPressure)
                    {
                        maxPressure = value;
                        selectedSubtype = subtype;
                    }
                }
                return selectedSubtype;
            }

            case RandomSubtype r:
                return r.Options[Random.Shared.Next(r.Options.Count)];

            case ConditionalSubtype c:
            {
                foreach (var when in c.When)
                {
                    if (EvaluateSubtypeCondition(when.Condition, context))
                        return when.Then;
                }
                return c.Otherwise;
            }

            default:
                throw new InvalidOperationException($"Unknown SubtypeSpec: {spec.GetType().Name}");
        }
    }

    private static bool EvaluateSubtypeCondition(
        SubtypeCondition condition,
        TemplateExecutionContext context)
    {
        switch (condition)
        {
            case TargetSubtypeCondition c:
                return context.Target?.Subtype == c.EqualsSubtype;

            case PressureCheckCondition c:
            {
                var pressure = context.Runtime.GetPressure(c.PressureId);
                return pressure >= c.Min && pressure <= c.Max;
            }

            default:
                return false;
        }
    }

    private static string ResolveCulture(
        CultureSpec spec,
        TemplateExecutionContext context)
    {
        switch (spec)
        {
            case InheritCulture i:
            {
                var refEntity = context.ResolveEntity(i.Ref);
                if (refEntity is null)
                    throw new InvalidOperationException(
                        $"Culture inherit reference '{i.Ref}' could not be resolved.");
                return refEntity.Culture.Value;
            }

            case FixedCulture f:
                return f.Id;

            default:
                throw new InvalidOperationException($"Unknown CultureSpec: {spec.GetType().Name}");
        }
    }

    private static string ResolveDescription(
        DescriptionSpec spec,
        TemplateExecutionContext context)
    {
        switch (spec)
        {
            case FixedDescription f:
                return f.Value;

            case TemplateDescription t:
            {
                var result = t.Template;
                foreach (var (key, reference) in t.Replacements)
                {
                    var value = ResolveStringReference(reference, context);
                    result = result.Replace($"{{{key}}}", value);
                }
                return result;
            }

            default:
                return "";
        }
    }

    private static string ResolveStringReference(
        string reference,
        TemplateExecutionContext context)
    {
        if (!reference.StartsWith('$')) return reference;

        var entity = context.ResolveEntity(reference);
        return entity?.Name ?? reference;
    }

    private static SemanticCoordinates ResolveCoordinates(
        PlacementSpec placement,
        TemplateExecutionContext context)
    {
        // The interpreter provides initial coordinates as a placeholder.
        // The growth system handles full placement (anchor resolution,
        // region assignment, spacing) after template expansion.
        switch (placement.Anchor)
        {
            case EntityAnchor ea:
            {
                var refEntity = context.ResolveEntity(ea.Ref);
                if (refEntity is not null)
                    return AddJitter(refEntity.Coordinates);
                break;
            }

            case RefsCentroidAnchor ra:
            {
                double sumX = 0, sumY = 0, sumZ = 0;
                var count = 0;
                foreach (var r in ra.Refs)
                {
                    var refEntity = context.ResolveEntity(r);
                    if (refEntity is not null)
                    {
                        sumX += refEntity.Coordinates.X;
                        sumY += refEntity.Coordinates.Y;
                        sumZ += refEntity.Coordinates.Z;
                        count++;
                    }
                }
                if (count > 0)
                {
                    return AddJitter(new SemanticCoordinates(
                        sumX / count, sumY / count, sumZ / count), ra.Jitter);
                }
                break;
            }

            case BoundsAnchor ba:
            {
                return new SemanticCoordinates(
                    ba.X.Min + Random.Shared.NextDouble() * (ba.X.Max - ba.X.Min),
                    ba.Y.Min + Random.Shared.NextDouble() * (ba.Y.Max - ba.Y.Min),
                    ba.Z.Min + Random.Shared.NextDouble() * (ba.Z.Max - ba.Z.Min));
            }
        }

        // Default: random coordinates in [0, 100]
        return new SemanticCoordinates(
            Random.Shared.NextDouble() * 100,
            Random.Shared.NextDouble() * 100,
            Random.Shared.NextDouble() * 100);
    }

    private static SemanticCoordinates AddJitter(SemanticCoordinates coords, double jitter = 5.0)
    {
        return new SemanticCoordinates(
            Math.Clamp(coords.X + (Random.Shared.NextDouble() - 0.5) * jitter, 0, 100),
            Math.Clamp(coords.Y + (Random.Shared.NextDouble() - 0.5) * jitter, 0, 100),
            Math.Clamp(coords.Z + (Random.Shared.NextDouble() - 0.5) * jitter, 0, 100));
    }

    // =========================================================================
    // PHASE 5: RELATIONSHIP CREATION
    // =========================================================================

    private static List<Relationship> ExecuteRelationship(
        RelationshipRule rule,
        TemplateExecutionContext context)
    {
        var relationships = new List<Relationship>();

        // Check condition
        if (rule.Condition is not null)
        {
            var ruleCtx = context.CreateRuleContext();
            if (!ConditionEvaluator.Evaluate(rule.Condition, ruleCtx))
                return relationships;
        }

        // Resolve source and destination entity IDs
        var srcEntities = ResolveRelationshipEntities(rule.Src, context);
        var dstEntities = ResolveRelationshipEntities(rule.Dst, context);

        var tick = context.Runtime.CurrentTick;
        var executionCtx = new ExecutionContext(
            tick, ExecutionSource.Template, context.TemplateId, true, "");

        foreach (var src in srcEntities)
        {
            foreach (var dst in dstEntities)
            {
                if (src.Id == dst.Id) continue;

                var distance = WorldRuntime.GetDistance(src.Coordinates, dst.Coordinates);

                // Resolve catalyzed by
                var catalyzedBy = "";
                if (!string.IsNullOrEmpty(rule.CatalyzedBy))
                {
                    var catalyst = context.ResolveEntity(rule.CatalyzedBy);
                    if (catalyst is not null) catalyzedBy = catalyst.Id.Value;
                }

                var rel = new Relationship(
                    sourceId: src.Id,
                    targetId: dst.Id,
                    kind: new RelationshipKind(rule.Kind),
                    strength: rule.Strength,
                    distance: distance,
                    category: "",
                    createdBy: executionCtx,
                    tick: tick,
                    catalyzedBy: catalyzedBy);
                relationships.Add(rel);

                if (rule.Bidirectional)
                {
                    var reverseRel = new Relationship(
                        sourceId: dst.Id,
                        targetId: src.Id,
                        kind: new RelationshipKind(rule.Kind),
                        strength: rule.Strength,
                        distance: distance,
                        category: "",
                        createdBy: executionCtx,
                        tick: tick,
                        catalyzedBy: catalyzedBy);
                    relationships.Add(reverseRel);
                }
            }
        }

        return relationships;
    }

    private static List<Entity> ResolveRelationshipEntities(
        string reference,
        TemplateExecutionContext context)
    {
        // Check created entities first (e.g., "$newFaction")
        var created = context.GetCreatedEntities(reference);
        if (created is not null && created.Count > 0)
            return new List<Entity>(created);

        // Resolve as variable or literal reference
        var entity = context.ResolveEntity(reference);
        if (entity is not null)
            return [entity];

        return [];
    }

    // =========================================================================
    // PHASE 6: STATE UPDATES
    // =========================================================================

    private static void ExecuteStateUpdate(
        Mutation mutation,
        TemplateExecutionContext context)
    {
        var ruleCtx = context.CreateRuleContext();
        MutationApplier.Apply(mutation, ruleCtx);
    }

    // =========================================================================
    // PHASE 7: VARIANT PROCESSING
    // =========================================================================

    private static List<TemplateVariant> GetMatchingVariants(
        TemplateVariants variants,
        TemplateExecutionContext context)
    {
        if (variants.Options.Count == 0) return [];

        var ruleCtx = context.CreateRuleContext();
        var matching = new List<TemplateVariant>();

        foreach (var variant in variants.Options)
        {
            if (ConditionEvaluator.Evaluate(variant.When, ruleCtx))
            {
                matching.Add(variant);
                if (variants.Selection == VariantSelectionMode.FirstMatch)
                    break;
            }
        }

        return matching;
    }

    private static void ApplyVariantEffects(
        VariantEffects effects,
        List<Entity> entities,
        List<Relationship> relationships,
        TemplateExecutionContext context)
    {
        // Apply subtype overrides
        foreach (var (entityRef, newSubtype) in effects.Subtype)
        {
            var created = context.GetCreatedEntities(entityRef);
            if (created is null) continue;
            foreach (var entity in created)
            {
                // Entity.Subtype is init-only; for variants that override subtype,
                // we would need a mutable path. Since Entity.Subtype is read-only
                // after construction, variant subtype overrides are a design limitation
                // in the C# port. Log and skip.
                context.Runtime.Log(
                    LogLevel.Debug,
                    $"Variant subtype override for '{entityRef}' to '{newSubtype}' — Entity.Subtype is immutable post-construction.");
            }
        }

        // Apply additional tags
        foreach (var (entityRef, tagMap) in effects.Tags)
        {
            var created = context.GetCreatedEntities(entityRef);
            if (created is null) continue;
            foreach (var entity in created)
            {
                foreach (var (tag, value) in tagMap)
                {
                    entity.Tags.Set(tag, value);
                }
            }
        }

        // Apply additional relationships
        foreach (var rule in effects.Relationships)
        {
            var created = ExecuteRelationship(rule, context);
            relationships.AddRange(created);
        }

        // Apply additional state updates
        foreach (var mutation in effects.StateUpdates)
        {
            ExecuteStateUpdate(mutation, context);
        }
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private static string GetPlacementStrategy(PlacementSpec placement)
    {
        return placement.Anchor switch
        {
            EntityAnchor => "near_entity",
            CultureAnchor => "within_culture",
            RefsCentroidAnchor => "near_centroid",
            BoundsAnchor => "within_bounds",
            SparseAnchor sa => sa.PreferPeriphery ? "sparse_periphery" : "sparse_area",
            _ => "unknown"
        };
    }

    private static PlacementDebug CreateEmptyPlacementDebug()
    {
        return new PlacementDebug(
            AnchorType: "",
            AnchorEntityId: null,
            AnchorEntityName: "",
            AnchorEntityKind: "",
            AnchorCulture: "",
            ResolvedVia: "",
            SeedRegionsAvailable: [],
            EmergentRegionId: null,
            EmergentRegionLabel: null,
            RegionId: null,
            AllRegionIds: []);
    }

    /// <summary>
    /// Lightweight IGraph adapter that delegates to WorldRuntime.
    /// Needed because RuleContext requires an IGraph, but the WorldRuntime
    /// encapsulates its graph as a private field.
    /// </summary>
    private sealed class RuntimeGraphAdapter : IGraph
    {
        private readonly WorldRuntime _runtime;

        public RuntimeGraphAdapter(WorldRuntime runtime) => _runtime = runtime;

        public int Tick
        {
            get => _runtime.CurrentTick;
            set => _runtime.CurrentTick = value;
        }

        public Dictionary<string, double> Pressures
        {
            get
            {
                var dict = new Dictionary<string, double>();
                foreach (var kvp in _runtime.AllPressures)
                    dict[kvp.Key] = kvp.Value;
                return dict;
            }
        }

        public Entity? GetEntity(EntityId id) => _runtime.GetEntity(id);
        public bool HasEntity(EntityId id) => _runtime.HasEntity(id);
        public int GetEntityCount(bool includeHistorical = false) => _runtime.GetEntityCount(includeHistorical);
        public IReadOnlyList<Entity> GetEntities(bool includeHistorical = false) => _runtime.GetEntities(includeHistorical);
        public IReadOnlyList<Entity> FindEntities(EntityCriteria criteria) => _runtime.FindEntities(criteria);
        public IReadOnlyList<Entity> GetEntitiesByKind(EntityKind kind, bool includeHistorical = false) => _runtime.GetEntitiesByKind(kind, includeHistorical);

        public IReadOnlyList<Entity> GetConnectedEntities(
            EntityId entityId, RelationshipKind? relationKind = null,
            Direction direction = Direction.Both, bool includeHistorical = false)
            => _runtime.GetConnectedEntities(entityId, relationKind, direction, includeHistorical);

        public EntityId CreateEntity(Entity entity) => _runtime.CreateEntity(entity);
        public bool UpdateEntity(EntityId id, Action<Entity> mutator) => _runtime.UpdateEntity(id, mutator);
        public bool DeleteEntity(EntityId id) => _runtime.DeleteEntity(id);

        public IReadOnlyList<Relationship> GetRelationships(bool includeHistorical = false) => _runtime.GetRelationships(includeHistorical);
        public IReadOnlyList<Relationship> FindRelationships(RelationshipCriteria criteria) => _runtime.FindRelationships(criteria);

        public IReadOnlyList<Relationship> GetEntityRelationships(
            EntityId entityId, Direction direction = Direction.Both, bool includeHistorical = false)
            => _runtime.GetEntityRelationships(entityId, direction, includeHistorical);

        public bool HasRelationship(EntityId srcId, EntityId dstId, RelationshipKind? kind = null)
            => _runtime.HasRelationship(srcId, dstId, kind);

        public bool AddRelationship(Relationship relationship) => _runtime.AddRelationship(relationship);
        public bool RemoveRelationship(EntityId srcId, EntityId dstId, RelationshipKind kind) => _runtime.RemoveRelationship(srcId, dstId, kind);
    }
}
