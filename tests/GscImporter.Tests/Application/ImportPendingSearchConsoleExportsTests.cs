using GscImporter.Application;
using GscImporter.Application.Ports;
using GscImporter.Domain;

namespace GscImporter.Tests.Application;

public sealed class ImportPendingSearchConsoleExportsTests
{
    [Fact]
    public async Task ExecuteAsync_ArchivesOnlyAfterPersistenceSucceeds()
    {
        var events = new List<string>();
        var searchConsoleExport = CreateExport();
        var useCase = new ImportPendingSearchConsoleExports(
            new IncomingCatalog(["input.zip"]),
            new ExportReader(searchConsoleExport),
            new Repository(() => events.Add("persisted")),
            new Archiver(() => events.Add("archived")));

        var result = await useCase.ExecuteAsync();

        Assert.False(result.HasFailures);
        Assert.Equal(["persisted", "archived"], events);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotArchiveWhenPersistenceFails()
    {
        var archiveWasCalled = false;
        var useCase = new ImportPendingSearchConsoleExports(
            new IncomingCatalog(["input.zip"]),
            new ExportReader(CreateExport()),
            new Repository(() => throw new InvalidOperationException("Database unavailable")),
            new Archiver(() => archiveWasCalled = true));

        var result = await useCase.ExecuteAsync();

        Assert.True(result.HasFailures);
        Assert.False(archiveWasCalled);
    }

    private static SearchConsoleExport CreateExport() => new(
        "input.zip",
        new SiteUrl("https://cyril.cophignon.net"),
        new ReportingMonth(2026, 1),
        []);

    private sealed class IncomingCatalog(IReadOnlyCollection<string> files) : IIncomingExportCatalog
    {
        public Task<IReadOnlyCollection<string>> FindPendingZipFilesAsync() => Task.FromResult(files);
    }

    private sealed class ExportReader(SearchConsoleExport value) : ISearchConsoleExportReader
    {
        public Task<SearchConsoleExport> ReadAsync(string zipFilePath) => Task.FromResult(value);
    }

    private sealed class Repository(Action action) : ISearchMetricRepository
    {
        public Task ReplaceMonthAsync(SearchConsoleExport searchConsoleExport)
        {
            action();
            return Task.CompletedTask;
        }
    }

    private sealed class Archiver(Action action) : IProcessedExportArchiver
    {
        public Task<string> ArchiveAsync(string zipFilePath, ReportingMonth month)
        {
            action();
            return Task.FromResult("archive.zip");
        }
    }
}
