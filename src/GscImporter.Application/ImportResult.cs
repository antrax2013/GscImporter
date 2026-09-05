namespace GscImporter.Application;

public sealed record ImportedFileResult(string SourceFile, string ArchiveFile, int MeasurementCount);
public sealed record FailedFileResult(string SourceFile, string ErrorMessage);
public sealed record ImportResult(IReadOnlyCollection<ImportedFileResult> ImportedFiles, IReadOnlyCollection<FailedFileResult> FailedFiles)
{
    public bool HasFailures => FailedFiles.Count > 0;
}
