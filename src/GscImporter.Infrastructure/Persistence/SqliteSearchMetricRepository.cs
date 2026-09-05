using GscImporter.Application.Ports;
using GscImporter.Domain;
using Microsoft.Data.Sqlite;

namespace GscImporter.Infrastructure.Persistence;

public sealed class SqliteSearchMetricRepository(string databasePath) : ISearchMetricRepository
{
    private string ConnectionString => new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();

    public async Task InitializeAsync()
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = Schema;
        await command.ExecuteNonQueryAsync();
    }

    public async Task ReplaceMonthAsync(SearchConsoleExport searchConsoleExport)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        await using var transaction = connection.BeginTransaction();
        try
        {
            var siteId = await FindOrCreateSiteAsync(connection, transaction, searchConsoleExport.Site);
            await DeleteExistingMonthAsync(connection, transaction, siteId, searchConsoleExport.Month);
            foreach (var measurement in searchConsoleExport.Measurements)
                await InsertMeasurementAsync(connection, transaction, siteId, measurement);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task<long> FindOrCreateSiteAsync(SqliteConnection connection, SqliteTransaction transaction, SiteUrl site)
    {
        await using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = "INSERT INTO Sites (Url) VALUES ($url) ON CONFLICT(Url) DO NOTHING;";
        insert.Parameters.AddWithValue("$url", site.Value);
        await insert.ExecuteNonQueryAsync();

        await using var select = connection.CreateCommand();
        select.Transaction = transaction;
        select.CommandText = "SELECT Id FROM Sites WHERE Url = $url;";
        select.Parameters.AddWithValue("$url", site.Value);
        return (long)(await select.ExecuteScalarAsync() ?? throw new InvalidOperationException("The site could not be persisted."));
    }

    private static async Task DeleteExistingMonthAsync(SqliteConnection connection, SqliteTransaction transaction, long siteId, ReportingMonth month)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM Measurements WHERE SiteId = $siteId AND ReportingMonth = $month;";
        command.Parameters.AddWithValue("$siteId", siteId);
        command.Parameters.AddWithValue("$month", month.ToString());
        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertMeasurementAsync(SqliteConnection connection, SqliteTransaction transaction, long siteId, SearchMetricMeasurement measurement)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO Measurements (SiteId, ReportingMonth, DimensionType, Element, Metric, Value)
            VALUES ($siteId, $month, $dimensionType, $element, $metric, $value);
            """;
        command.Parameters.AddWithValue("$siteId", siteId);
        command.Parameters.AddWithValue("$month", measurement.Month.ToString());
        command.Parameters.AddWithValue("$dimensionType", measurement.DimensionType.ToString());
        command.Parameters.AddWithValue("$element", measurement.Element);
        command.Parameters.AddWithValue("$metric", measurement.Metric.ToString());
        command.Parameters.AddWithValue("$value", measurement.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await command.ExecuteNonQueryAsync();
    }

    private const string Schema = """
        PRAGMA foreign_keys = ON;
        CREATE TABLE IF NOT EXISTS Sites (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Url TEXT NOT NULL COLLATE NOCASE UNIQUE
        );
        CREATE TABLE IF NOT EXISTS Measurements (
            SiteId INTEGER NOT NULL,
            ReportingMonth TEXT NOT NULL,
            DimensionType TEXT NOT NULL,
            Element TEXT NOT NULL,
            Metric TEXT NOT NULL,
            Value NUMERIC NOT NULL,
            PRIMARY KEY (SiteId, ReportingMonth, DimensionType, Element, Metric),
            FOREIGN KEY (SiteId) REFERENCES Sites(Id)
        );
        CREATE INDEX IF NOT EXISTS IX_Measurements_Month ON Measurements(ReportingMonth);
        """;
}
