using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using VeterinaryBackend.Business.Options;
using VeterinaryBackend.DataAccess.Repositories;
using VeterinaryBackend.DataAccess.UnitOfWork;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.Business.Services;

/// <summary>
/// Long-running polling worker. Pulls updates from Telegram and dispatches:
///   • /start  → register subscriber, show main menu (reply keyboard)
///   • "📩 So'rov yuborish" button → start name → phone → message conversation
///   • "📰 Barcha postlar" button → list latest 10 contents
///   • /cancel → reset conversation state
/// In-memory state machine; if the process restarts mid-conversation users
/// just get reset on the next message — acceptable for short flows.
/// </summary>
public class TelegramBotHostedService : BackgroundService
{
    private const string BtnRequest = "📩 So'rov yuborish";
    private const string BtnPosts = "📰 Barcha postlar";
    private const string BtnShareContact = "📱 Kontaktni ulashish";
    private const string BtnCancel = "❌ Bekor qilish";

    private enum ConvState { Idle, AwaitingContact, AwaitingMessage }

    private sealed class Conversation
    {
        public ConvState State { get; set; } = ConvState.Idle;
        public string? Name { get; set; }
        public string? Phone { get; set; }
    }

    private static readonly ConcurrentDictionary<long, Conversation> _conversations = new();

    private static readonly ReplyKeyboardMarkup MainMenu = new(new[]
    {
        new[] { new KeyboardButton(BtnRequest), new KeyboardButton(BtnPosts) }
    })
    { ResizeKeyboard = true };

    private static readonly ReplyKeyboardMarkup ContactMenu = new(new[]
    {
        new[] { new KeyboardButton(BtnShareContact) { RequestContact = true } },
        new[] { new KeyboardButton(BtnCancel) }
    })
    { ResizeKeyboard = true, OneTimeKeyboard = true };

    private readonly ITelegramBotClient? _bot;
    private readonly TelegramBotOptions _options;
    private readonly SiteOptions _siteOptions;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<TelegramBotHostedService> _logger;

    public TelegramBotHostedService(
        ITelegramBotClient? bot,
        IOptions<TelegramBotOptions> options,
        IOptions<SiteOptions> siteOptions,
        IServiceScopeFactory scopeFactory,
        IWebHostEnvironment env,
        ILogger<TelegramBotHostedService> logger)
    {
        _bot = bot;
        _options = options.Value;
        _siteOptions = siteOptions.Value;
        _scopeFactory = scopeFactory;
        _env = env;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_bot is null || !_options.Enabled || string.IsNullOrWhiteSpace(_options.BotToken))
        {
            _logger.LogInformation("Telegram bot polling disabled (no token or Enabled=false).");
            return;
        }

        try
        {
            var me = await _bot.GetMe(stoppingToken);
            _logger.LogInformation("Telegram bot online: @{Username} (Id={Id}).", me.Username, me.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Telegram. Polling will not start.");
            return;
        }

        var receiverOptions = new ReceiverOptions { AllowedUpdates = new[] { UpdateType.Message } };

        _bot.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);

        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { /* graceful shutdown */ }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken ct)
    {
        if (update.Message is not { } msg) return;
        // Allow contact updates without text; for everything else we need text.
        if (msg.Contact is null && string.IsNullOrEmpty(msg.Text)) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var subsRepo = scope.ServiceProvider.GetRequiredService<IBotSubscriberRepository>();
            var msgRepo = scope.ServiceProvider.GetRequiredService<IBotMessageRepository>();
            var contentRepo = scope.ServiceProvider.GetRequiredService<IContentRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var chatId = msg.Chat.Id;
            var text = (msg.Text ?? string.Empty).Trim();
            await EnsureSubscriberAsync(subsRepo, msg, ct);

            // Cancel — both the slash command and the cancel button reset state
            if (text.Equals("/cancel", StringComparison.OrdinalIgnoreCase) || text == BtnCancel)
            {
                _conversations.TryRemove(chatId, out _);
                await uow.SaveChangesAsync(ct);
                await client.SendMessage(chatId, "Bekor qilindi. Asosiy menyu:", replyMarkup: MainMenu, cancellationToken: ct);
                return;
            }

            if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
            {
                _conversations.TryRemove(chatId, out _);
                await uow.SaveChangesAsync(ct);
                await client.SendMessage(
                    chatId,
                    "Assalomu alaykum! Veterinariya markazi botiga xush kelibsiz.\n\n" +
                    "📩 <b>So'rov yuborish</b> — savol yoki murojaatingizni qoldiring.\n" +
                    "📰 <b>Barcha postlar</b> — eng so'nggi yangiliklarni ko'rish.\n\n" +
                    "Yangi yangiliklar paydo bo'lganda bot avtomatik xabar beradi.",
                    parseMode: ParseMode.Html,
                    replyMarkup: MainMenu,
                    cancellationToken: ct);
                return;
            }

            if (text == BtnPosts)
            {
                _conversations.TryRemove(chatId, out _);
                await uow.SaveChangesAsync(ct);
                await SendLatestPostsAsync(client, contentRepo, chatId, ct);
                return;
            }

            if (text == BtnRequest)
            {
                _conversations[chatId] = new Conversation { State = ConvState.AwaitingContact };
                await client.SendMessage(
                    chatId,
                    "Murojaat yuborish uchun avval kontaktingizni ulashing.\n\n" +
                    "Quyidagi <b>📱 Kontaktni ulashish</b> tugmasini bosing — ismingiz va telefon raqamingiz avtomatik olinadi.",
                    parseMode: ParseMode.Html,
                    replyMarkup: ContactMenu,
                    cancellationToken: ct);
                return;
            }

            var conv = _conversations.GetOrAdd(chatId, _ => new Conversation());
            switch (conv.State)
            {
                case ConvState.AwaitingContact:
                    if (msg.Contact is null)
                    {
                        await client.SendMessage(
                            chatId,
                            "Iltimos, <b>📱 Kontaktni ulashish</b> tugmasini bosing yoki <b>❌ Bekor qilish</b>.",
                            parseMode: ParseMode.Html,
                            replyMarkup: ContactMenu,
                            cancellationToken: ct);
                        return;
                    }

                    var contact = msg.Contact;
                    conv.Name = string.Join(' ', new[] { contact.FirstName, contact.LastName }
                        .Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
                    if (string.IsNullOrWhiteSpace(conv.Name)) conv.Name = msg.From?.FirstName ?? "—";
                    conv.Phone = contact.PhoneNumber;
                    conv.State = ConvState.AwaitingMessage;

                    await client.SendMessage(
                        chatId,
                        $"Rahmat, <b>{System.Net.WebUtility.HtmlEncode(conv.Name)}</b>!\n\n" +
                        "Endi murojaatingizni yozing:",
                        parseMode: ParseMode.Html,
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: ct);
                    return;

                case ConvState.AwaitingMessage:
                    if (string.IsNullOrWhiteSpace(text) || text.Length < 3)
                    {
                        await client.SendMessage(chatId, "Murojaat juda qisqa. Iltimos, batafsilroq yozing:", cancellationToken: ct);
                        return;
                    }
                    await msgRepo.AddAsync(new BotMessage
                    {
                        ChatId = chatId,
                        Username = msg.From?.Username,
                        FirstName = msg.From?.FirstName,
                        ContactName = conv.Name,
                        Phone = conv.Phone,
                        Text = text.Length > 4000 ? text[..4000] : text,
                        IsRead = false
                    }, ct);
                    await uow.SaveChangesAsync(ct);
                    _conversations.TryRemove(chatId, out _);

                    await client.SendMessage(
                        chatId,
                        $"✅ <b>Murojaatingiz qabul qilindi!</b>\n\n" +
                        $"<b>Ism:</b> {System.Net.WebUtility.HtmlEncode(conv.Name)}\n" +
                        $"<b>Telefon:</b> {System.Net.WebUtility.HtmlEncode(conv.Phone)}\n\n" +
                        "Admin tez orada javob beradi.",
                        parseMode: ParseMode.Html,
                        replyMarkup: MainMenu,
                        cancellationToken: ct);
                    return;

                default:
                    await client.SendMessage(
                        chatId,
                        "Quyidagi tugmalardan birini tanlang yoki /start yuboring:",
                        replyMarkup: MainMenu,
                        cancellationToken: ct);
                    return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Telegram update {UpdateId}.", update.Id);
        }
    }

    private async Task EnsureSubscriberAsync(IBotSubscriberRepository subs, Message msg, CancellationToken ct)
    {
        var existing = await subs.GetByChatIdAsync(msg.Chat.Id, ct);
        if (existing is null)
        {
            await subs.AddAsync(new BotSubscriber
            {
                ChatId = msg.Chat.Id,
                Username = msg.From?.Username,
                FirstName = msg.From?.FirstName,
                LastName = msg.From?.LastName,
                IsActive = true
            }, ct);
        }
        else if (!existing.IsActive)
        {
            existing.IsActive = true;
            subs.Update(existing);
        }
    }

    private async Task SendLatestPostsAsync(ITelegramBotClient client, IContentRepository contents, long chatId, CancellationToken ct)
    {
        var paged = await contents.GetPagedWithSectionAsync(page: 1, pageSize: 10, sectionId: null, onlyActive: true, ct);
        if (paged.Items.Count == 0)
        {
            await client.SendMessage(chatId, "Hozircha postlar yo'q.", replyMarkup: MainMenu, cancellationToken: ct);
            return;
        }

        await client.SendMessage(chatId, $"📰 <b>So'nggi {paged.Items.Count} ta post:</b>", parseMode: ParseMode.Html, cancellationToken: ct);

        var frontend = (_siteOptions.FrontendUrl ?? string.Empty).TrimEnd('/');
        foreach (var c in paged.Items)
        {
            var title = !string.IsNullOrWhiteSpace(c.TitleUz) ? c.TitleUz : c.TitleRu;
            var excerpt = ExtractPlainText(!string.IsNullOrWhiteSpace(c.DescriptionUz) ? c.DescriptionUz : c.DescriptionRu);
            var caption = $"📰 <b>{System.Net.WebUtility.HtmlEncode(title)}</b>";
            if (!string.IsNullOrWhiteSpace(excerpt))
            {
                var trimmed = excerpt.Length > 700 ? excerpt[..700] + "…" : excerpt;
                caption += "\n\n" + System.Net.WebUtility.HtmlEncode(trimmed);
            }

            // HTML hyperlink anchored on the bold "Bizning sahifa" — appears as an
            // underlined blue link in every Telegram client (desktop, web, mobile).
            // Tappable on localhost-dev (desktop only) and production (everywhere).
            if (!string.IsNullOrWhiteSpace(frontend))
            {
                var url = $"{frontend}/content.html?id={c.Id}";
                caption += $"\n\n👉 <a href=\"{url}\"><b>Davlat markazi sahifasi</b></a>";
            }
            InlineKeyboardMarkup? keyboard = null;

            try
            {
                var bytes = TryReadLocalImage(c.ImageUrl);
                if (bytes is not null)
                {
                    using var ms = new MemoryStream(bytes, writable: false);
                    await client.SendPhoto(chatId, InputFile.FromStream(ms, "photo.jpg"), caption: caption, parseMode: ParseMode.Html, replyMarkup: keyboard, cancellationToken: ct);
                }
                else
                {
                    await client.SendMessage(chatId, caption, parseMode: ParseMode.Html, replyMarkup: keyboard, cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send post {ContentId} to chat {ChatId}.", c.Id, chatId);
            }
        }

        await client.SendMessage(chatId, "Asosiy menyu:", replyMarkup: MainMenu, cancellationToken: ct);
    }

    private byte[]? TryReadLocalImage(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith("/")) return null;
        try
        {
            var rel = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var localPath = Path.Combine(_env.WebRootPath ?? string.Empty, rel);
            return System.IO.File.Exists(localPath) ? System.IO.File.ReadAllBytes(localPath) : null;
        }
        catch { return null; }
    }

    private static string? ExtractPlainText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (raw.StartsWith("__VS_BLOCKS_V1__", StringComparison.Ordinal))
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                raw, "\"t\"\\s*:\\s*\"text\"\\s*,\\s*\"v\"\\s*:\\s*\"([^\"]+)\"");
            raw = match.Success ? System.Text.RegularExpressions.Regex.Unescape(match.Groups[1].Value) : raw;
        }
        raw = System.Text.RegularExpressions.Regex.Replace(raw, "<[^>]+>", " ");
        raw = System.Text.RegularExpressions.Regex.Replace(raw, "\\s+", " ").Trim();
        return raw;
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient client, Exception ex, CancellationToken ct)
    {
        _logger.LogWarning(ex, "Telegram polling error.");
        return Task.CompletedTask;
    }
}
