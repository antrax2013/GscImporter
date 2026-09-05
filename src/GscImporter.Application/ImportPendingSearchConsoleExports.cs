using GscImporter.Application.Ports;

namespace GscImporter.Application;

public sealed class ImportPendingSearchConsoleExports(
    IIncomingExportCatalog incomingExportCatalog,
    ISearchConsoleExportReader exportReader,
    ISearchMetricRepository searchMetricRepository,
    IProcessedExportArchiver processedExportArchiver)
{
    public async Task<ImportResult> ExecuteAsync()
    {
        var importedFiles = new List<ImportedFileResult>();
        var failedFiles = new List<FailedFileResult>();
        var pendingFiles = await incomingExportCatalog.FindPendingZipFilesAsync();

        foreach (var zipFilePath in pendingFiles.Order(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var searchConsoleExport = await exportReader.ReadAsync(zipFilePath);
                await searchMetricRepository.ReplaceMonthAsync(searchConsoleExport);
                var archivePath = await processedExportArchiver.ArchiveAsync(zipFilePath, searchConsoleExport.Month);
                importedFiles.Add(new ImportedFileResult(zipFilePath, archivePath, searchConsoleExport.Measurements.Count));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failedFiles.Add(new FailedFileResult(zipFilePath, exception.Message));
            }
        }

        return new ImportResult(importedFiles, failedFiles);
    }
}
