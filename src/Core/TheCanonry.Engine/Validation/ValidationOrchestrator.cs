using TheCanonry.Engine.Engine;

namespace TheCanonry.Engine.Validation;

// =============================================================================
// VALIDATION REPORT
// =============================================================================

/// <summary>
/// Result of running all validation passes against an EngineConfig.
/// </summary>
public sealed record ValidationReport(
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings)
{
    public bool IsValid => Errors.Count == 0;
}

// =============================================================================
// VALIDATION ORCHESTRATOR
// =============================================================================

/// <summary>
/// Orchestrates all validation passes against an EngineConfig.
/// Runs FrameworkValidator and ContractEnforcer, aggregates results
/// into a single ValidationReport.
/// </summary>
public static class ValidationOrchestrator
{
    /// <summary>
    /// Run all validators and return a combined report.
    /// </summary>
    public static ValidationReport Validate(EngineConfig config)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        errors.AddRange(FrameworkValidator.Validate(config));
        errors.AddRange(ContractEnforcer.Validate(config));

        return new ValidationReport(errors, warnings);
    }
}
