using GscImporter.Domain;
using GscImporter.Infrastructure.Exports;
using System.IO.Compression;
using System.Text;

namespace GscImporter.Tests.Infrastructure;

public sealed class GscZipExportReaderTests : IDisposable
{
    private readonly string temporaryDirectory = Path.Combine(Path.GetTempPath(), $"gsc-importer-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task ReadAsync_TransformsPagesAndQueriesIntoNormalizedMeasurements()
    {
        Directory.CreateDirectory(temporaryDirectory);
        var zipPath = Path.Combine(temporaryDirectory, "https___cyril.cophignon.net_-Performance-on-Search-2026-09-05.zip");
        CreateExport(zipPath, "https://cyril.cophignon.net/geobiologie");

        var result = await new GscZipExportReader().ReadAsync(zipPath);

        Assert.Equal("https://cyril.cophignon.net", result.Site.Value);
        Assert.Equal(new ReportingMonth(2026, 1), result.Month);
        Assert.Equal(8, result.Measurements.Count);
        Assert.Contains(result.Measurements, item =>
            item.DimensionType == SearchDimensionType.Page &&
            item.Metric == SearchMetricName.ClickThroughRate &&
            item.Value == 0.125m);
        Assert.Contains(result.Measurements, item =>
            item.DimensionType == SearchDimensionType.Query &&
            item.Element == "géobiologie yvelines" &&
            item.Metric == SearchMetricName.Position &&
            item.Value == 8.4m);
    }

    [Fact]
    public async Task ReadAsync_RejectsPageFromAnotherSite()
    {
        Directory.CreateDirectory(temporaryDirectory);
        var zipPath = Path.Combine(temporaryDirectory, "https___cyril.cophignon.net_-Performance-on-Search-2026-09-05.zip");
        CreateExport(zipPath, "https://massage-reiki.fr/geobiologie");

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => new GscZipExportReader().ReadAsync(zipPath));
        Assert.Contains("does not belong", exception.Message);
    }

    private static void CreateExport(string zipPath, string pageUrl)
    {
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        WriteEntry(archive, "Pages.csv", $"Pages les plus populaires,Clics,Impressions,CTR,Position\n{pageUrl},2,16,12.5%,3.25\n");
        WriteEntry(archive, "Requêtes.csv", "Requêtes les plus fréquentes,Clics,Impressions,CTR,Position\ngéobiologie yvelines,1,10,10%,8.4\n");
        var graph = new StringBuilder("Date,Clics,Impressions,CTR,Position\n");
        foreach (var day in Enumerable.Range(1, 31)) graph.AppendLine($"2026-01-{day:D2},0,0,0%,0");
        WriteEntry(archive, "Graphique.csv", graph.ToString());
        WriteEntry(archive, "Filtres.csv", "Filtre,Valeur\nType de recherche,Web\nDate,1 janv. 2026-31 janv. 2026\n");
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory)) Directory.Delete(temporaryDirectory, recursive: true);
    }
}
