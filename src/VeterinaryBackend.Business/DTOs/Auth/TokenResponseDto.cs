namespace VeterinaryBackend.Business.DTOs.Auth;

public class TokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTime ExpiresAt { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin";
}
