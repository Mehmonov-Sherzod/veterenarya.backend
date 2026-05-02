namespace VeterinaryBackend.Business.Options;

public class AdminOptions
{
    public const string SectionName = "Admin";

    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Long random key used for password reset when admin forgets password.
    /// Admin must save this securely on initial setup. Empty means reset is disabled.
    /// </summary>
    public string RecoveryKey { get; set; } = string.Empty;
}
