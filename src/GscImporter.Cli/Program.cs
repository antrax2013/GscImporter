using GscImporter.Application;
using GscImporter.Infrastructure.Configuration;
using GscImporter.Infrastructure.Exports;
using GscImporter.Infrastructure.Files;
using GscImporter.Infrastructure.Persistence;
using System.Text.Json;

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
};

try
{
    var configurationPath = ReadConfigurationPath(args);
    var settings = await LoadSettingsAsync(configurationPath);
    var resolvedSettings = settings.ResolveRelativePaths(Path.GetDirectoryName(configurationPath)!);

    var repository = new SqliteSearchMetricRepository(resolvedSettings.Database);
    await repository.InitializeAsync();

    var importPendingExports = new ImportPendingSearchConsoleExports(
        new FileSystemIncomingExportCatalog(resolvedSettings.ImportDirectory),
        new GscZipExportReader(),
        repository,
        new FileSystemProcessedExportArchiver(resolvedSettings.ArchiveDirectory));

    var result = await importPendingExports.ExecuteAsync();
    foreach (var imported in result.ImportedFiles)
        Console.WriteLine($"Imported {Path.GetFileName(imported.SourceFile)}: {imported.MeasurementCount} measurements -> {imported.ArchiveFile}");
    foreach (var failed in result.FailedFiles)
        Console.Error.WriteLine($"Failed {Path.GetFileName(failed.SourceFile)}: {failed.ErrorMessage}");

    if (result.ImportedFiles.Count == 0 && result.FailedFiles.Count == 0)
        Console.WriteLine("No ZIP file found in the import directory.");

    return result.HasFailures ? 1 : 0;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Import cancelled.");
    return 2;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Fatal error: {exception.Message}");
    return 2;
}

static string ReadConfigurationPath(string[] arguments)
{
    if (arguments.Length == 0) return Path.GetFullPath("appsettings.json");
    if (arguments.Length == 2 && arguments[0] == "--config") return Path.GetFullPath(arguments[1]);
    throw new ArgumentException("Usage: GscImporter.Cli [--config <appsettings.json>]");
}

static async Task<ImporterSettings> LoadSettingsAsync(string configurationPath)
{
    if (!File.Exists(configurationPath)) throw new FileNotFoundException("Configuration file not found.", configurationPath);
    await using var stream = File.OpenRead(configurationPath);
    return await JsonSerializer.DeserializeAsync<ImporterSettings>(stream, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    }) ?? throw new InvalidDataException("The configuration file is empty or invalid.");
}
