namespace GscImporter.Application.Ports;

public interface IIncomingExportCatalog
{
    Task<IReadOnlyCollection<string>> FindPendingZipFilesAsync();
}
