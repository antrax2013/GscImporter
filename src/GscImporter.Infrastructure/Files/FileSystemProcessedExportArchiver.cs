using GscImporter.Application.Ports;
using GscImporter.Domain;

namespace GscImporter.Infrastructure.Files;

public sealed class FileSystemProcessedExportArchiver(string archiveDirectory) : IProcessedExportArchiver
{
    public Task<string> ArchiveAsync(string zipFilePath, ReportingMonth month)
    {
        var monthlyArchiveDirectory = Path.Combine(archiveDirectory, month.ToString());
        Directory.CreateDirectory(monthlyArchiveDirectory);
        var destination = FindAvailableDestination(monthlyArchiveDirectory, Path.GetFileName(zipFilePath));
        File.Move(zipFilePath, destination);
        return Task.FromResult(destination);
    }

    private static string FindAvailableDestination(string directory, string fileName)
    {
        var proposedPath = Path.Combine(directory, fileName);
        if (!File.Exists(proposedPath)) return proposedPath;

        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        for (var suffix = 2; ; suffix++)
        {
            proposedPath = Path.Combine(directory, $"{baseName}_{suffix}{extension}");
            if (!File.Exists(proposedPath)) return proposedPath;
        }
    }
}
