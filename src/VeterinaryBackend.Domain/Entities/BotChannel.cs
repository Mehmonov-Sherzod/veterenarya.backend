using VeterinaryBackend.Domain.Common;

namespace VeterinaryBackend.Domain.Entities;

/// <summary>
/// Telegram channel/supergroup where the bot has been promoted to administrator.
/// Discovered automatically via MyChatMember updates — no manual config needed.
/// </summary>
public class BotChannel : BaseEntity
{
    public long ChatId { get; set; }
    public string? Title { get; set; }
    public string? Username { get; set; }
    public bool IsActive { get; set; } = true;
}
