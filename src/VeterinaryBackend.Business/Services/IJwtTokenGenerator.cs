using VeterinaryBackend.Business.DTOs.Auth;

namespace VeterinaryBackend.Business.Services;

public interface IJwtTokenGenerator
{
    TokenResponseDto Generate(string username, string role);
}
