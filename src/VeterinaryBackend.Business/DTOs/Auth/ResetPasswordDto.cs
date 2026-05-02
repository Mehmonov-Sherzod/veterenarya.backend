namespace VeterinaryBackend.Business.DTOs.Auth;

public class ResetPasswordDto
{
    public string RecoveryKey { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
