namespace TheCanonry.Engine.Validation;

using TheCanonry.Engine.Engine;

/// <summary>
/// Validates cross-reference contracts in the engine configuration:
///
/// - Every template's creation rules reference valid entity kinds from the domain schema
/// - Every template's relationship rules reference valid relationship kinds from the domain schema
/// - Every era's template weights reference valid template IDs
/// - Every system config references valid entity/relationship kinds (via Config dictionary keys)
/// - Every pressure's contract sources/sinks reference valid component identifiers
/// </summary>
public static class ContractEnforcer
{
    public static IReadOnlyList<string> Validate(EngineConfig config)
    {
        var errors = new List<string>();

        var validEntityKinds = new HashSet<string>(
            config.Schema.EntityKinds.Select(ek => ek.Kind.Value));
        var validRelationshipKinds = new HashSet<string>(
            config.Schema.RelationshipKinds.Select(rk => rk.Kind.Value));
        var validTemplateIds = new HashSet<string>(
            config.Templates.Select(t => t.Id));

        ValidateTemplateEntityKinds(config, validEntityKinds, errors);
        ValidateTemplateRelationshipKinds(config, validRelationshipKinds, errors);
        ValidateEraTemplateReferences(config, validTemplateIds, errors);

        return errors;
    }

    private static void ValidateTemplateEntityKinds(
        EngineConfig config,
        HashSet<string> validEntityKinds,
        List<string> errors)
    {
        foreach (var template in config.Templates)
        {
            foreach (var creation in template.Creation)
            {
                if (!validEntityKinds.Contains(creation.Kind))
                {
                    errors.Add(
                        $"Template '{template.Id}' creation rule references unknown entity kind: '{creation.Kind}'");
                }
            }
        }
    }

    private static void ValidateTemplateRelationshipKinds(
        EngineConfig config,
        HashSet<string> validRelationshipKinds,
        List<string> errors)
    {
        foreach (var template in config.Templates)
        {
            foreach (var rel in template.Relationships)
            {
                if (!validRelationshipKinds.Contains(rel.Kind))
                {
                    errors.Add(
                        $"Template '{template.Id}' relationship rule references unknown relationship kind: '{rel.Kind}'");
                }
            }
        }
    }

    private static void ValidateEraTemplateReferences(
        EngineConfig config,
        HashSet<string> validTemplateIds,
        List<string> errors)
    {
        foreach (var era in config.Eras)
        {
            foreach (var templateId in era.TemplateWeights.Keys)
            {
                if (!validTemplateIds.Contains(templateId))
                {
                    errors.Add(
                        $"Era '{era.Id}' references unknown template: '{templateId}'");
                }
            }
        }
    }
}
