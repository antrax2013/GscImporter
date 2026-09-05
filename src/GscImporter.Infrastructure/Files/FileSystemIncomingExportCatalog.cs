using GscImporter.Application.Ports;

namespace GscImporter.Infrastructure.Files;

public sealed class FileSystemIncomingExportCatalog(string importDirectory) : IIncomingExportCatalog
{
    public Task<IReadOnlyCollection<string>> FindPendingZipFilesAsync()
    {
        Directory.CreateDirectory(importDirectory);
        IReadOnlyCollection<string> files = Directory.GetFiles(importDirectory, "*.zip", SearchOption.TopDirectoryOnly);
        return Task.FromResult(files);
    }
}
