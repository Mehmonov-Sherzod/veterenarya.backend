using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using VeterinaryBackend.Business.Options;
using VeterinaryBackend.Domain.Exceptions;

namespace VeterinaryBackend.Business.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly StorageOptions _options;
    private readonly IWebHostEnvironment _env;

    public LocalFileStorageService(IOptions<StorageOptions> options, IWebHostEnvironment env)
    {
        _options = options.Value;
        _env = env;
    }

    public async Task<StoredFileResult> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        long sizeBytes,
        CancellationToken ct = default)
    {
        if (sizeBytes <= 0)
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["file"] = new[] { "Empty file is not allowed." }
            });

        if (sizeBytes > _options.MaxFileSizeBytes)
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["file"] = new[] { $"File exceeds the maximum allowed size of {_options.MaxFileSizeBytes / (1024 * 1024)} MB." }
            });

        var webRoot = _env.WebRootPath;
        if (string.IsNullOrEmpty(webRoot))
        {
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
            Directory.CreateDirectory(webRoot);
        }

        var now = DateTime.UtcNow;
        var subDir = Path.Combine(_options.UploadsRelativePath, now.Year.ToString("D4"), now.Month.ToString("D2"));
        var absoluteDir = Path.Combine(webRoot, subDir);
        Directory.CreateDirectory(absoluteDir);

        var safeOriginal = Path.GetFileName(originalFileName);
        var extension = Path.GetExtension(safeOriginal);
        if (extension.Length > 20)
            extension = extension[..20];

        if (string.IsNullOrEmpty(extension))
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["file"] = new[] { "File must have an extension." }
            });

        if (_options.AllowedExtensions.Count > 0 && !_options.AllowedExtensions.Contains(extension))
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["file"] = new[] { $"File extension '{extension}' is not allowed. Allowed: {string.Join(", ", _options.AllowedExtensions)}" }
            });

        var storedName = $"{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(absoluteDir, storedName);

        await using (var fileStream = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(fileStream, ct);
        }

        var relativePath = Path.Combine(subDir, storedName).Replace('\\', '/');
        var url = $"{_options.PublicBaseUrl.TrimEnd('/')}/{now.Year:D4}/{now.Month:D2}/{storedName}";

        return new StoredFileResult(
            StoredFileName: storedName,
            RelativePath: relativePath,
            Url: url,
            Extension: extension,
            ContentType: string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            SizeBytes: sizeBytes);
    }

    public bool DeletePhysical(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return false;

        var webRoot = _env.WebRootPath;
        if (string.IsNullOrEmpty(webRoot))
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

        var absolutePath = Path.Combine(webRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var fullWebRoot = Path.GetFullPath(webRoot);
        var fullTarget = Path.GetFullPath(absolutePath);

        if (!fullTarget.StartsWith(fullWebRoot, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!File.Exists(fullTarget))
            return false;

        File.Delete(fullTarget);
        return true;
    }
}
