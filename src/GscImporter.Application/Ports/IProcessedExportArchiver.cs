using GscImporter.Domain;

namespace GscImporter.Application.Ports;

public interface IProcessedExportArchiver
{
    Task<string> ArchiveAsync(string zipFilePath, ReportingMonth month);
}
