using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using VeterinaryBackend.Business.Options;
using VeterinaryBackend.DataAccess.Repositories;
using VeterinaryBackend.DataAccess.UnitOfWork;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.Business.Services;

/// <summary>
/// Wraps a singleton <see cref="ITelegramBotClient"/> and exposes broadcast.
/// Resolves scoped repositories from <see cref="IServiceScopeFactory"/> because
/// this service is itself a singleton (must be — it's used by the hosted
/// polling service).
/// </summary>
public class TelegramBotService : ITelegramBotService
{
    private readonly ITelegramBotClient? _bot;
    private readonly TelegramBotOptions _options;
    private readonly SiteOptions _siteOptions;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<TelegramBotService> _logger;

    public TelegramBotService(
        ITelegramBotClient? bot,
        IOptions<TelegramBotOptions> options,
        IOptions<SiteOptions> siteOptions,
        IServiceScopeFactory scopeFactory,
        IWebHostEnvironment env,
        ILogger<TelegramBotService> logger)
    {
        _bot = bot;
        _options = options.Value;
        _siteOptions = siteOptions.Value;
        _scopeFactory = scopeFactory;
        _env = env;
        _logger = logger;
    }

    public async Task BroadcastContentAsync(int contentId, string title, string? excerpt, string? imageUrl, CancellationToken ct = default)
    {
        if (!IsConfigured()) return;

        using var scope = _scopeFactory.CreateScope();
        var text = BuildBroadcastText(contentId, title, excerpt);

        // Try to resolve the local image bytes once. Telegram can't reach
        // localhost via URL, but we can stream the file content directly.
        byte[]? imageBytes = null;
        string? imageFileName = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(imageUrl) && imageUrl.StartsWith("/"))
            {
                var rel = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var localPath = Path.Combine(_env.WebRootPath ?? string.Empty, rel);
                if (System.IO.File.Exists(localPath))
                {
                    imageBytes = await System.IO.File.ReadAllBytesAsync(localPath, ct);
                    imageFileName = Path.GetFileName(localPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load image {ImageUrl} for broadcast; sending text only.", imageUrl);
        }

        // Per-subscriber broadcast intentionally disabled — content is published
        // ONLY to the channels where the bot is admin (auto-discovered via
        // MyChatMember + the optional TargetChannelId fallback). Subscribers
        // still get the per-conversation auto-replies and admin messages, but
        // not the post feed.

        // Channel cross-post: union of (auto-discovered admin channels) + the
        // optional TargetChannelId fallback in config. Failures are logged per
        // channel — a missing-permission channel doesn't block the others.
        var channelTargets = new List<string>();
        var channelRepo = scope.ServiceProvider.GetRequiredService<IBotChannelRepository>();
        var autoChannels = await channelRepo.GetActiveAsync(ct);
        foreach (var c in autoChannels) channelTargets.Add(c.ChatId.ToString());
        if (!string.IsNullOrWhiteSpace(_options.TargetChannelId) &&
            !channelTargets.Contains(_options.TargetChannelId))
        {
            channelTargets.Add(_options.TargetChannelId);
        }

        foreach (var channelId in channelTargets)
        {
            try
            {
                if (imageBytes is not null)
                {
                    using var ms = new MemoryStream(imageBytes, writable: false);
                    await _bot!.SendPhoto(
                        chatId: channelId,
                        photo: InputFile.FromStream(ms, imageFileName ?? "photo.jpg"),
                        caption: text,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                        cancellationToken: ct);
                }
                else
                {
                    await _bot!.SendMessage(
                        chatId: channelId,
                        text: text,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                        cancellationToken: ct);
                }
                _logger.LogInformation("Cross-posted content {ContentId} to channel {ChannelId}.", contentId, channelId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cross-post content {ContentId} to channel {ChannelId}. Make sure the bot is an admin with Post permission.", contentId, channelId);
            }
        }
    }

    public async Task SendDirectAsync(long chatId, string text, CancellationToken ct = default)
    {
        if (!IsConfigured())
            throw new InvalidOperationException("Telegram bot is not configured.");

        var html = "<b>Admin javobi:</b>\n\n" + System.Net.WebUtility.HtmlEncode(text);
        await _bot!.SendMessage(chatId, html, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);
    }

    private bool IsConfigured() =>
        _bot is not null && _options.Enabled && !string.IsNullOrWhiteSpace(_options.BotToken);

    private string BuildBroadcastText(int contentId, string title, string? excerpt)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("📰 <b>").Append(System.Net.WebUtility.HtmlEncode(title)).Append("</b>");
        if (!string.IsNullOrWhiteSpace(excerpt))
        {
            sb.AppendLine();
            sb.AppendLine();
            var trimmed = excerpt!.Length > 700 ? excerpt[..700] + "…" : excerpt;
            sb.Append(System.Net.WebUtility.HtmlEncode(trimmed));
        }

        // Anchor the link on the words "bizning sahifa" (HTML hyperlink). The raw URL
        // is hidden — the user just sees underlined "bizning sahifa" and tapping it
        // opens the site. Works in both dev (localhost link, tappable on desktop) and
        // production (https://vettashxismarkaz.uz, tappable everywhere).
        var frontend = (_siteOptions.FrontendUrl ?? string.Empty).TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(frontend))
        {
            var url = $"{frontend}/content.html?id={contentId}";
            sb.AppendLine();
            sb.AppendLine();
            sb.Append("👉 <a href=\"").Append(url).Append("\"><b>Davlat markazi sahifasi</b></a>");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Build a single-button inline keyboard that opens the post on the public site.
    /// Telegram rejects URL buttons that point at localhost / 127.0.0.1 / private IPs
    /// with "BUTTON_URL_INVALID", which used to swallow the entire message — so we
    /// return null in dev and rely on the caller to send the message without a button.
    /// </summary>
    private Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup? BuildOpenUrlKeyboard(int contentId)
    {
        var frontend = (_siteOptions.FrontendUrl ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(frontend) || IsLocalUrl(frontend)) return null;
        var url = $"{frontend}/content.html?id={contentId}";
        return new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
        {
            new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithUrl("📖 To'liq o'qish", url) }
        });
    }

    internal static bool IsLocalUrl(string url) =>
        url.Contains("localhost", StringComparison.OrdinalIgnoreCase)
        || url.Contains("127.0.0.1")
        || url.Contains("0.0.0.0")
        || url.Contains("//192.168.")
        || url.Contains("//10.")
        || url.Contains("//172.16.")
        || url.Contains("//172.17.")
        || url.Contains("//172.18.")
        || url.Contains("//172.19.")
        || url.Contains("//172.2")
        || url.Contains("//172.30.")
        || url.Contains("//172.31.");
}
