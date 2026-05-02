namespace VeterinaryBackend.Business.Options;

public class SiteOptions
{
    public const string SectionName = "Site";

    /// <summary>Public-facing frontend URL — used by admin panel "back to site" button.</summary>
    public string FrontendUrl { get; set; } = "http://localhost:8080";
}
