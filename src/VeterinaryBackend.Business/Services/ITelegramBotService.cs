namespace VeterinaryBackend.Business.Services;

/// <summary>
/// Public-facing API for the Telegram bot integration. The hosted polling
/// service is separate; consumers (e.g. ContentService) only need broadcast.
/// </summary>
public interface ITelegramBotService
{
    /// <summary>
    /// Send a content notification to every active subscriber. Failures
    /// per-recipient are swallowed so one bad chat does not block the rest.
    /// No-op when the bot is disabled or has no token.
    /// </summary>
    Task BroadcastContentAsync(int contentId, string title, string? excerpt, string? imageUrl, CancellationToken ct = default);

    /// <summary>
    /// Send a direct admin reply to a specific chat. Throws when the bot is
    /// not configured so the caller can surface a 400/500 to the admin.
    /// </summary>
    Task SendDirectAsync(long chatId, string text, CancellationToken ct = default);
}
