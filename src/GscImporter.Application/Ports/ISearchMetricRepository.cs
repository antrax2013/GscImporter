using GscImporter.Domain;

namespace GscImporter.Application.Ports;

public interface ISearchMetricRepository
{
    Task ReplaceMonthAsync(SearchConsoleExport searchConsoleExport);
}
