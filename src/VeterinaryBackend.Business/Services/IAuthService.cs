using VeterinaryBackend.Business.DTOs.Auth;

namespace VeterinaryBackend.Business.Services;

public interface IAuthService
{
    Task<TokenResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
    Task ChangePasswordAsync(ChangePasswordDto request, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordDto request, CancellationToken ct = default);
}
