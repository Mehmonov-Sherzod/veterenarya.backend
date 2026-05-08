namespace VeterinaryBackend.Business.Options;

public class TelegramBotOptions
{
    public const string SectionName = "TelegramBot";

    public string BotToken { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string AutoReplyText { get; set; } = "Murojaatingiz qabul qilindi.";
}
