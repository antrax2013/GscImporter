namespace GscImporter.Domain;

public sealed record SearchConsoleExport(
    string SourceFilePath,
    SiteUrl Site,
    ReportingMonth Month,
    IReadOnlyCollection<SearchMetricMeasurement> Measurements);
