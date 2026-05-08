using Microsoft.Extensions.Options;
using VeterinaryBackend.Business.Common;
using VeterinaryBackend.Business.DTOs.Auth;
using VeterinaryBackend.Business.Options;
using VeterinaryBackend.DataAccess.Repositories;
using VeterinaryBackend.DataAccess.UnitOfWork;
using VeterinaryBackend.Domain.Exceptions;

namespace VeterinaryBackend.Business.Services;

public class AuthService : IAuthService
{
    private const int MinPasswordLength = 6;

    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IOptionsMonitor<AdminOptions> _adminMonitor;

    public AuthService(
        IUserRepository users,
        IUnitOfWork uow,
        IJwtTokenGenerator tokenGenerator,
        IOptionsMonitor<AdminOptions> adminMonitor)
    {
        _users = users;
        _uow = uow;
        _tokenGenerator = tokenGenerator;
        _adminMonitor = adminMonitor;
    }

    public async Task<TokenResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            throw new UnauthorizedAppException("Username and password are required.");

        var username = request.Username.Trim();
        var user = await _users.GetByUsernameAsync(username, ct);

        if (user is null || !user.IsActive)
            throw new UnauthorizedAppException("Invalid username or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAppException("Invalid username or password.");

        return _tokenGenerator.Generate(user.Username, user.Role);
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

        var bootstrapUsername = _adminMonitor.CurrentValue.Username;
        var user = await _users.GetByUsernameAsync(bootstrapUsername, ct)
            ?? throw new UnauthorizedAppException("Admin foydalanuvchi topilmadi.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAppException("Joriy parol noto'g'ri.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 11);
        await _uow.SaveChangesAsync(ct);
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

        if (!FixedTimeEquals(admin.RecoveryKey, request.RecoveryKey.Trim()))
            throw new UnauthorizedAppException("Tiklash kaliti noto'g'ri.");

        var user = await _users.GetByUsernameAsync(admin.Username, ct)
            ?? throw new UnauthorizedAppException("Admin foydalanuvchi topilmadi.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 11);
        await _uow.SaveChangesAsync(ct);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var diff = 0;
        for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }
}
