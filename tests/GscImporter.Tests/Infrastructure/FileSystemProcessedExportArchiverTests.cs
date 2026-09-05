using GscImporter.Domain;
using GscImporter.Infrastructure.Files;

namespace GscImporter.Tests.Infrastructure;

public sealed class FileSystemProcessedExportArchiverTests : IDisposable
{
    private readonly string temporaryDirectory = Path.Combine(Path.GetTempPath(), $"gsc-importer-archive-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task ArchiveAsync_StoresTheFileInItsReportingMonthDirectory()
    {
        var importDirectory = Directory.CreateDirectory(Path.Combine(temporaryDirectory, "imports")).FullName;
        var archiveDirectory = Directory.CreateDirectory(Path.Combine(temporaryDirectory, "archives")).FullName;
        var source = Path.Combine(importDirectory, "export.zip");
        await File.WriteAllTextAsync(source, "content", CancellationToken.None);

        var archivedFile = await new FileSystemProcessedExportArchiver(archiveDirectory)
            .ArchiveAsync(source, new ReportingMonth(2026, 1));

        Assert.False(File.Exists(source));
        Assert.Equal(Path.Combine(archiveDirectory, "2026-01", "export.zip"), archivedFile);
        Assert.True(File.Exists(archivedFile));
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory)) Directory.Delete(temporaryDirectory, recursive: true);
    }
}
