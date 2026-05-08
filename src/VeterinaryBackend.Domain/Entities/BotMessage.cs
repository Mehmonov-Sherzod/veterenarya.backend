using VeterinaryBackend.Domain.Common;

namespace VeterinaryBackend.Domain.Entities;

public class BotMessage : BaseEntity
{
    public long ChatId { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public BotMessageStatus Status { get; set; } = BotMessageStatus.Pending;
}
