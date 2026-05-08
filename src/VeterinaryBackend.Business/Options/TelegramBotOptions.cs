namespace VeterinaryBackend.Business.Options;

public class TelegramBotOptions
{
    public const string SectionName = "TelegramBot";

    public string BotToken { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string AutoReplyText { get; set; } = "Murojaatingiz qabul qilindi.";

    /// <summary>
    /// Optional Telegram channel where every new content post is published in
    /// addition to the per-subscriber broadcast. Accepts either the numeric chat
    /// id (e.g. <c>-1001234567890</c>) or a public username with @ prefix
    /// (<c>@vetcenter_news</c>). Leave empty to disable channel cross-posting.
    /// The bot must be an admin of the target channel with "Post Messages"
    /// permission, otherwise Telegram returns 403/400 and the post is skipped.
    /// </summary>
    public string TargetChannelId { get; set; } = string.Empty;
}
