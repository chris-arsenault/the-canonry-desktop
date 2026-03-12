using TheCanonry.Illuminator.Types;

namespace TheCanonry.Illuminator.Enrichment.Tasks;

/// <summary>
/// Static registry mapping each EnrichmentType to a factory function that creates its executor.
/// </summary>
public static class TaskRegistry
{
    /// <summary>
    /// Factory delegate that creates an IEnrichmentTaskExecutor from a TaskContext.
    /// </summary>
    public delegate IEnrichmentTaskExecutor TaskFactory(TaskContext context);

    private static readonly Dictionary<EnrichmentType, TaskFactory> Factories = new()
    {
        [EnrichmentType.SummaryRevision] = ctx => new SummaryRevisionTask(ctx),
        [EnrichmentType.ChronicleBackport] = ctx => new ChronicleBackportTask(ctx),
        [EnrichmentType.DynamicsGeneration] = ctx => new DynamicsGenerationTask(ctx),
        [EnrichmentType.PaletteExpansion] = ctx => new PaletteExpansionTask(ctx),
        [EnrichmentType.HistorianEdition] = ctx => new HistorianEditionTask(ctx),
        [EnrichmentType.HistorianReview] = ctx => new HistorianReviewTask(ctx),
        [EnrichmentType.HistorianChronology] = ctx => new HistorianChronologyTask(ctx),
        [EnrichmentType.HistorianPrep] = ctx => new HistorianPrepTask(ctx),
        [EnrichmentType.EraNarrative] = ctx => new EraNarrativeTask(ctx),
        [EnrichmentType.MotifVariation] = ctx => new MotifVariationTask(ctx),
        [EnrichmentType.ToneRanking] = ctx => new ToneRankingTask(ctx),
        [EnrichmentType.FactCoverage] = ctx => new FactCoverageTask(ctx),
        [EnrichmentType.EntityTagImageStyles] = ctx => new EntityTagImageStylesTask(ctx),
    };

    /// <summary>
    /// Get the factory for a given enrichment type.
    /// Returns null if the type requires additional dependencies (Description, Image, Chronicle, etc.).
    /// </summary>
    public static TaskFactory? GetFactory(EnrichmentType type)
    {
        return Factories.GetValueOrDefault(type);
    }

    /// <summary>
    /// Create an executor for the given enrichment type using the context.
    /// Returns null for types that need additional dependencies.
    /// </summary>
    public static IEnrichmentTaskExecutor? Create(EnrichmentType type, TaskContext context)
    {
        var factory = GetFactory(type);
        return factory?.Invoke(context);
    }

    /// <summary>
    /// Get all enrichment types that have simple (context-only) factories registered.
    /// </summary>
    public static IReadOnlyList<EnrichmentType> SimpleTaskTypes => Factories.Keys.ToList();
}
