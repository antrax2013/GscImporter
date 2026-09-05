namespace GscImporter.Domain;

public sealed record SearchMetricMeasurement
{
    public SearchMetricMeasurement(
        SiteUrl site,
        ReportingMonth month,
        SearchDimensionType dimensionType,
        string element,
        SearchMetricName metric,
        decimal value)
    {
        Site = site ?? throw new ArgumentNullException(nameof(site));
        Month = month;
        DimensionType = dimensionType;
        Element = string.IsNullOrWhiteSpace(element)
            ? throw new ArgumentException("The measured page or query cannot be empty.", nameof(element))
            : element.Trim();
        Metric = metric;
        Value = value;
    }

    public SiteUrl Site { get; }
    public ReportingMonth Month { get; }
    public SearchDimensionType DimensionType { get; }
    public string Element { get; }
    public SearchMetricName Metric { get; }
    public decimal Value { get; }
}
