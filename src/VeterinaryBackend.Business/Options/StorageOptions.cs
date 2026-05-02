namespace VeterinaryBackend.Business.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string UploadsRelativePath { get; set; } = "uploads";

    public string PublicBaseUrl { get; set; } = "/uploads";

    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024;

    public HashSet<string> AllowedExtensions { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg", ".bmp", ".ico",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".json", ".xml", ".txt", ".csv", ".md", ".rtf",
        ".mp4", ".webm", ".mp3", ".wav", ".ogg",
        ".zip", ".rar", ".7z"
    };
}
