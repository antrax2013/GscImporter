using GscImporter.Application.Ports;
using GscImporter.Domain;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace GscImporter.Infrastructure.Exports;

public sealed partial class GscZipExportReader : ISearchConsoleExportReader
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    public Task<SearchConsoleExport> ReadAsync(string zipFilePath)
    {
        var site = ExtractSiteFromFileName(Path.GetFileName(zipFilePath));

        using var archive = ZipFile.OpenRead(zipFilePath);
        var reportingMonth = DetectReportingMonth(ReadRows(FindEntry(archive, "Graphique.csv")));
        var measurements = new List<SearchMetricMeasurement>();
        measurements.AddRange(ReadMeasurements(ReadRows(FindEntry(archive, "Pages.csv")), site, reportingMonth, SearchDimensionType.Page));
        measurements.AddRange(ReadMeasurements(ReadRows(FindQueryEntry(archive)), site, reportingMonth, SearchDimensionType.Query));

        ValidatePageUrls(site, measurements);
        return Task.FromResult(new SearchConsoleExport(zipFilePath, site, reportingMonth, measurements));
    }

    public static SiteUrl ExtractSiteFromFileName(string fileName)
    {
        var match = ExportFileNamePattern().Match(fileName);
        if (!match.Success) throw new InvalidDataException($"The ZIP filename does not identify a GSC site: {fileName}");
        return new SiteUrl($"{match.Groups["scheme"].Value}://{match.Groups["host"].Value}");
    }

    private static ZipArchiveEntry FindEntry(ZipArchive archive, string expectedName) =>
        archive.Entries.SingleOrDefault(entry => string.Equals(entry.Name, expectedName, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidDataException($"The ZIP archive does not contain {expectedName}.");

    private static ZipArchiveEntry FindQueryEntry(ZipArchive archive) =>
        archive.Entries.SingleOrDefault(entry => entry.Name.StartsWith("Requ", StringComparison.OrdinalIgnoreCase) && entry.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidDataException("The ZIP archive does not contain Requêtes.csv.");

    private static IReadOnlyList<IReadOnlyList<string>> ReadRows(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true);
        return SimpleCsvReader.Read(reader);
    }

    private static ReportingMonth DetectReportingMonth(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        if (rows.Count < 2 || !string.Equals(rows[0][0], "Date", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Graphique.csv has an unexpected format.");

        var dates = rows.Skip(1).Select(row => DateOnly.ParseExact(row[0], "yyyy-MM-dd", InvariantCulture)).ToArray();
        return ReportingMonth.FromDates(dates);
    }

    private static IEnumerable<SearchMetricMeasurement> ReadMeasurements(
        IReadOnlyList<IReadOnlyList<string>> rows,
        SiteUrl site,
        ReportingMonth reportingMonth,
        SearchDimensionType dimensionType)
    {
        if (rows.Count == 0) throw new InvalidDataException($"The {dimensionType} CSV file is empty.");
        var header = rows[0];
        var clicksIndex = FindColumn(header, "Clics", "Clicks");
        var impressionsIndex = FindColumn(header, "Impressions");
        var ctrIndex = FindColumn(header, "CTR");
        var positionIndex = FindColumn(header, "Position");
        var lastRequiredIndex = new[] { clicksIndex, impressionsIndex, ctrIndex, positionIndex }.Max();

        foreach (var row in rows.Skip(1))
        {
            if (row.Count <= lastRequiredIndex || string.IsNullOrWhiteSpace(row[0])) continue;
            var element = row[0].Trim();
            yield return NewMeasurement(SearchMetricName.Clicks, ParseDecimal(row[clicksIndex]));
            yield return NewMeasurement(SearchMetricName.Impressions, ParseDecimal(row[impressionsIndex]));
            yield return NewMeasurement(SearchMetricName.ClickThroughRate, ParsePercentage(row[ctrIndex]));
            yield return NewMeasurement(SearchMetricName.Position, ParseDecimal(row[positionIndex]));

            SearchMetricMeasurement NewMeasurement(SearchMetricName metric, decimal value) =>
                new(site, reportingMonth, dimensionType, element, metric, value);
        }
    }

    private static int FindColumn(IReadOnlyList<string> headers, params string[] acceptedNames)
    {
        for (var index = 0; index < headers.Count; index++)
            if (acceptedNames.Any(name => string.Equals(headers[index].Trim(), name, StringComparison.OrdinalIgnoreCase))) return index;
        throw new InvalidDataException($"Missing CSV column: {string.Join(" or ", acceptedNames)}.");
    }

    private static decimal ParseDecimal(string value) => decimal.Parse(value.Trim(), NumberStyles.Number, InvariantCulture);
    private static decimal ParsePercentage(string value) => ParseDecimal(value.Trim().TrimEnd('%')) / 100m;

    private static void ValidatePageUrls(SiteUrl site, IEnumerable<SearchMetricMeasurement> measurements)
    {
        foreach (var page in measurements.Where(item => item.DimensionType == SearchDimensionType.Page))
        {
            if (!Uri.TryCreate(page.Element, UriKind.Absolute, out var uri) || !site.Contains(uri))
                throw new InvalidDataException($"Page URL '{page.Element}' does not belong to site '{site}'.");
        }
    }

    [GeneratedRegex("^(?<scheme>https?)___(?<host>.+?)_-Performance-on-Search-.*\\.zip$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExportFileNamePattern();
}
