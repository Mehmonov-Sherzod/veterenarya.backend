namespace VeterinaryBackend.Business.Options;

public class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Run pending migrations on startup. Convenient for single-instance dev,
    /// but in horizontally-scaled production deploys two pods racing this can
    /// deadlock — set to false and run <c>dotnet ef database update</c> as a
    /// one-shot pre-deploy step (or migration init container).
    /// </summary>
    public bool AutoMigrateOnStartup { get; set; } = true;
}
