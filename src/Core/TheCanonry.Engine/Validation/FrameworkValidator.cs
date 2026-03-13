using TheCanonry.Engine.Engine;
using TheCanonry.Schema.Primitives;

namespace TheCanonry.Engine.Validation;

/// <summary>
/// Validates that framework primitives are present in the domain configuration.
///
/// Checks:
/// - DomainSchema includes all FrameworkPrimitives entity kinds (era, occurrence)
/// - DomainSchema includes all FrameworkPrimitives relationship kinds
///   (supersedes, part_of, active_during, participant_in, epicenter_of, triggered_by, created_during)
/// - At least one era is defined
/// - At least one template is defined
/// </summary>
public static class FrameworkValidator
{
    public static IReadOnlyList<string> Validate(EngineConfig config)
    {
        var errors = new List<string>();

        ValidateEntityKinds(config, errors);
        ValidateRelationshipKinds(config, errors);
        ValidateEras(config, errors);
        ValidateTemplates(config, errors);

        return errors;
    }

    private static void ValidateEntityKinds(EngineConfig config, List<string> errors)
    {
        var schemaKinds = new HashSet<string>(
            config.Schema.EntityKinds.Select(ek => ek.Kind.Value));

        foreach (var required in FrameworkPrimitives.EntityKinds.All)
        {
            if (!schemaKinds.Contains(required.Value))
            {
                errors.Add(
                    $"DomainSchema is missing required framework entity kind: '{required.Value}'");
            }
        }
    }

    private static void ValidateRelationshipKinds(EngineConfig config, List<string> errors)
    {
        var schemaKinds = new HashSet<string>(
            config.Schema.RelationshipKinds.Select(rk => rk.Kind.Value));

        foreach (var required in FrameworkPrimitives.RelationshipKinds.All)
        {
            if (!schemaKinds.Contains(required.Value))
            {
                errors.Add(
                    $"DomainSchema is missing required framework relationship kind: '{required.Value}'");
            }
        }
    }

    private static void ValidateEras(EngineConfig config, List<string> errors)
    {
        if (config.Eras.Count == 0)
        {
            errors.Add("At least one era must be defined");
        }
    }

    private static void ValidateTemplates(EngineConfig config, List<string> errors)
    {
        if (config.Templates.Count == 0)
        {
            errors.Add("At least one template must be defined");
        }
    }
}
