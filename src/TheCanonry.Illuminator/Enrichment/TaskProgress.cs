namespace TheCanonry.Illuminator.Enrichment;

/// <summary>
/// Progress report from an enrichment task executor.
/// </summary>
public sealed record TaskProgress(string Message, double? Fraction = null);
