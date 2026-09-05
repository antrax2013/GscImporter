using GscImporter.Domain;
using GscImporter.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;

namespace GscImporter.Tests.Infrastructure;

public sealed class SqliteSearchMetricRepositoryTests : IDisposable
{
    private readonly string temporaryDirectory = Path.Combine(Path.GetTempPath(), $"gsc-importer-db-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task ReplaceMonthAsync_RemovesMeasurementsMissingFromTheNewExport()
    {
        Directory.CreateDirectory(temporaryDirectory);
        var databasePath = Path.Combine(temporaryDirectory, "gsc.db");
        var repository = new SqliteSearchMetricRepository(databasePath);
        await repository.InitializeAsync();
        var site = new SiteUrl("https://cyril.cophignon.net");
        var month = new ReportingMonth(2026, 1);

        await repository.ReplaceMonthAsync(new SearchConsoleExport("first.zip", site, month,
        [
            new(site, month, SearchDimensionType.Query, "old query", SearchMetricName.Clicks, 1),
            new(site, month, SearchDimensionType.Query, "kept query", SearchMetricName.Clicks, 2)
        ]));

        await repository.ReplaceMonthAsync(new SearchConsoleExport("second.zip", site, month,
        [
            new(site, month, SearchDimensionType.Query, "kept query", SearchMetricName.Clicks, 3)
        ]));

        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Element, Value FROM Measurements;";
        await using var reader = await command.ExecuteReaderAsync(CancellationToken.None);
        Assert.True(await reader.ReadAsync(CancellationToken.None));
        Assert.Equal("kept query", reader.GetString(0));
        Assert.Equal(3m, reader.GetDecimal(1));
        Assert.False(await reader.ReadAsync(CancellationToken.None));
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(temporaryDirectory)) Directory.Delete(temporaryDirectory, recursive: true);
    }
}
