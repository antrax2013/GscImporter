namespace GscImporter.Infrastructure.Configuration;

public sealed record ImporterSettings
{
    public string Database { get; init; } = "data/gsc.db";
    public string ImportDirectory { get; init; } = "imports";
    public string ArchiveDirectory { get; init; } = "archives";

    public ImporterSettings ResolveRelativePaths(string baseDirectory) => this with
    {
        Database = ResolvePath(baseDirectory, Database),
        ImportDirectory = ResolvePath(baseDirectory, ImportDirectory),
        ArchiveDirectory = ResolvePath(baseDirectory, ArchiveDirectory)
    };

    private static string ResolvePath(string baseDirectory, string configuredPath) =>
        Path.GetFullPath(Path.IsPathRooted(configuredPath) ? configuredPath : Path.Combine(baseDirectory, configuredPath));
}
