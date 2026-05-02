using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using VeterinaryBackend.Business.Common;
using VeterinaryBackend.Business.DTOs.Auth;
using VeterinaryBackend.Business.Options;
using VeterinaryBackend.Domain.Exceptions;

namespace VeterinaryBackend.Business.Services;

public class AuthService : IAuthService
{
    private const int MinPasswordLength = 6;

    private readonly IOptionsMonitor<AdminOptions> _adminMonitor;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IConfigurationRoot _configurationRoot;
    private readonly IWebHostEnvironment _env;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public AuthService(
        IOptionsMonitor<AdminOptions> adminMonitor,
        IJwtTokenGenerator tokenGenerator,
        IConfiguration configuration,
        IWebHostEnvironment env)
    {
        _adminMonitor = adminMonitor;
        _tokenGenerator = tokenGenerator;
        _configurationRoot = (IConfigurationRoot)configuration;
        _env = env;
    }

    public Task<TokenResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            throw new UnauthorizedAppException("Username and password are required.");

        var admin = _adminMonitor.CurrentValue;

        if (string.IsNullOrWhiteSpace(admin.Username) || string.IsNullOrWhiteSpace(admin.PasswordHash))
            throw new UnauthorizedAppException("Admin account is not configured.");

        var usernameMatches = string.Equals(request.Username.Trim(), admin.Username, StringComparison.OrdinalIgnoreCase);
        var passwordMatches = usernameMatches && BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash);

        if (!passwordMatches)
            throw new UnauthorizedAppException("Invalid username or password.");

        var token = _tokenGenerator.Generate(admin.Username, AppRoles.Admin);
        return Task.FromResult(token);
    }

    public async Task ChangePasswordAsync(ChangePasswordDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["password"] = new[] { "Joriy va yangi parol talab qilinadi." }
            });

        if (request.NewPassword.Length < MinPasswordLength)
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["newPassword"] = new[] { $"Yangi parol kamida {MinPasswordLength} ta belgi bo'lishi kerak." }
            });

        var admin = _adminMonitor.CurrentValue;
        if (string.IsNullOrWhiteSpace(admin.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, admin.PasswordHash))
        {
            throw new UnauthorizedAppException("Joriy parol noto'g'ri.");
        }

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 11);
        await UpdateAppSettingsAsync(new[] { ("Admin", "PasswordHash", newHash) }, ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RecoveryKey) || string.IsNullOrWhiteSpace(request.NewPassword))
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["recoveryKey"] = new[] { "Tiklash kaliti va yangi parol talab qilinadi." }
            });

        if (request.NewPassword.Length < MinPasswordLength)
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["newPassword"] = new[] { $"Yangi parol kamida {MinPasswordLength} ta belgi bo'lishi kerak." }
            });

        var admin = _adminMonitor.CurrentValue;
        if (string.IsNullOrWhiteSpace(admin.RecoveryKey))
            throw new UnauthorizedAppException("Tiklash funksiyasi sozlanmagan.");

        // Constant-time comparison
        if (!FixedTimeEquals(admin.RecoveryKey, request.RecoveryKey.Trim()))
            throw new UnauthorizedAppException("Tiklash kaliti noto'g'ri.");

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 11);
        await UpdateAppSettingsAsync(new[] { ("Admin", "PasswordHash", newHash) }, ct);
    }

    private async Task UpdateAppSettingsAsync(IEnumerable<(string Section, string Key, string Value)> updates, CancellationToken ct)
    {
        var path = Path.Combine(_env.ContentRootPath, "appsettings.json");
        if (!File.Exists(path))
            throw new InvalidOperationException("appsettings.json not found at " + path);

        var json = await File.ReadAllTextAsync(path, ct);
        var root = JsonNode.Parse(json) as JsonObject
            ?? throw new InvalidOperationException("appsettings.json is not a JSON object.");

        foreach (var (section, key, value) in updates)
        {
            if (root[section] is not JsonObject sectionObj)
            {
                sectionObj = new JsonObject();
                root[section] = sectionObj;
            }
            sectionObj[key] = value;
        }

        var output = root.ToJsonString(JsonOpts);
        await File.WriteAllTextAsync(path, output, ct);

        _configurationRoot.Reload();
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var diff = 0;
        for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }
}
