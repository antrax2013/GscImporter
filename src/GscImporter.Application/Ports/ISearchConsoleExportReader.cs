using GscImporter.Domain;

namespace GscImporter.Application.Ports;

public interface ISearchConsoleExportReader
{
    Task<SearchConsoleExport> ReadAsync(string zipFilePath);
}
