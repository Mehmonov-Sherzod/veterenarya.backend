namespace VeterinaryBackend.Business.Services;

public interface IFileStorageService
{
    Task<StoredFileResult> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        long sizeBytes,
        CancellationToken ct = default);

    bool DeletePhysical(string relativePath);
}

public record StoredFileResult(
    string StoredFileName,
    string RelativePath,
    string Url,
    string Extension,
    string ContentType,
    long SizeBytes);
