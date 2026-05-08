using VeterinaryBackend.Domain.Common;

namespace VeterinaryBackend.Domain.Entities;

public class BotSubscriber : BaseEntity
{
    public long ChatId { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; } = true;
}
