namespace VeterinaryBackend.Business.Options;

public class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Empty (or null) means "allow any origin" — only safe in development.
    /// In production set this to the public site origin(s), e.g.
    /// ["https://vettashxismarkaz.uz"]. Configurable via env var
    /// <c>Cors__AllowedOrigins__0=https://...</c>.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
