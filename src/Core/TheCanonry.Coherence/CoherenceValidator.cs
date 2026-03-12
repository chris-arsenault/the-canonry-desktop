namespace TheCanonry.Coherence;

using TheCanonry.Coherence.Rules;
using TheCanonry.Coherence.Types;
using TheCanonry.Engine.Engine;

/// <summary>
/// Main entry point for coherence validation.
/// Runs all rules against an EngineConfig and collects results into a CoherenceReport.
/// </summary>
public static class CoherenceValidator
{
    public static CoherenceReport Validate(EngineConfig config)
    {
        var issues = new List<CoherenceIssue>();

        // Pressure rules
        Collect(issues, PressureRules.PressureWithoutSources(config));
        Collect(issues, PressureRules.PressureWithoutSinks(config));

        // Orphan rules
        Collect(issues, OrphanRules.OrphanGenerators(config));
        Collect(issues, OrphanRules.OrphanSystems(config));
        Collect(issues, OrphanRules.ZeroWeightGenerators(config));

        // Cross-reference rules
        Collect(issues, CrossReferenceRules.InvalidSubtypeReferences(config));
        Collect(issues, CrossReferenceRules.InvalidStatusReferences(config));
        Collect(issues, CrossReferenceRules.InvalidCultureReferences(config));

        // Numeric range rules
        Collect(issues, NumericRangeRules.NumericRangeIssues(config));
        Collect(issues, NumericRangeRules.RelationshipCompatibility(config));

        return new CoherenceReport(issues);
    }

    private static void Collect(List<CoherenceIssue> issues, CoherenceIssue? issue)
    {
        if (issue is not null)
        {
            issues.Add(issue);
        }
    }
}
