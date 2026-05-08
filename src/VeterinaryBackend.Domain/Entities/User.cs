using VeterinaryBackend.Domain.Common;

namespace VeterinaryBackend.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "SuperAdmin";
    public bool IsActive { get; set; } = true;
}
