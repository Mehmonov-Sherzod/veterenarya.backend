namespace VeterinaryBackend.Business.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string UploadsRelativePath { get; set; } = "uploads";

    public string PublicBaseUrl { get; set; } = "/uploads";

    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024;

    /// <summary>
    /// SVG/JSON/XML/HTML excluded — they can carry script payloads and would execute
    /// when served from the same origin as the admin panel. If you genuinely need
    /// them, serve those uploads with Content-Disposition: attachment and a
    /// dedicated subdomain.
    /// </summary>
    public HashSet<string> AllowedExtensions { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".ico",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".txt", ".csv", ".md", ".rtf",
        ".mp4", ".webm", ".mp3", ".wav", ".ogg",
        ".zip", ".rar", ".7z"
    };
}
