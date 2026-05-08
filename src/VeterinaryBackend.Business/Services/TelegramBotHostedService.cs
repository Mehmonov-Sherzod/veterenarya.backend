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
        new[] { new KeyboardButton(BtnRequest) }
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

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.MyChatMember }
        };

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
        // Auto-discover channels: when the bot is promoted/demoted in a channel
        // or supergroup, Telegram delivers a MyChatMember update we listen to.
        if (update.MyChatMember is { } cm)
        {
            await HandleMyChatMemberAsync(cm, ct);
            return;
        }

        if (update.Message is not { } msg) return;
        // Allow contact updates without text; for everything else we need text.
        if (msg.Contact is null && string.IsNullOrEmpty(msg.Text)) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var subsRepo = scope.ServiceProvider.GetRequiredService<IBotSubscriberRepository>();
            var msgRepo = scope.ServiceProvider.GetRequiredService<IBotMessageRepository>();
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
                    "📩 <b>So'rov yuborish</b> tugmasini bosib savol yoki murojaatingizni qoldiring. Admin tez orada javob beradi.",
                    parseMode: ParseMode.Html,
                    replyMarkup: MainMenu,
                    cancellationToken: ct);
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

    private async Task HandleMyChatMemberAsync(ChatMemberUpdated cm, CancellationToken ct)
    {
        // Only care about channels and supergroups — group/private bot promotions
        // don't broadcast there.
        if (cm.Chat.Type != ChatType.Channel && cm.Chat.Type != ChatType.Supergroup) return;

        var newStatus = cm.NewChatMember.Status;
        var becameAdmin = newStatus == ChatMemberStatus.Administrator || newStatus == ChatMemberStatus.Creator;
        var lostAccess = newStatus == ChatMemberStatus.Left || newStatus == ChatMemberStatus.Kicked
                         || newStatus == ChatMemberStatus.Restricted || newStatus == ChatMemberStatus.Member;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var channels = scope.ServiceProvider.GetRequiredService<IBotChannelRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var existing = await channels.GetByChatIdAsync(cm.Chat.Id, ct);

            if (becameAdmin)
            {
                if (existing is null)
                {
                    await channels.AddAsync(new BotChannel
                    {
                        ChatId = cm.Chat.Id,
                        Title = cm.Chat.Title,
                        Username = cm.Chat.Username,
                        IsActive = true
                    }, ct);
                    _logger.LogInformation("Bot promoted to admin in channel {ChatId} ({Title}).", cm.Chat.Id, cm.Chat.Title);
                }
                else
                {
                    existing.IsActive = true;
                    existing.Title = cm.Chat.Title;
                    existing.Username = cm.Chat.Username;
                    channels.Update(existing);
                    _logger.LogInformation("Re-activated channel {ChatId} ({Title}).", cm.Chat.Id, cm.Chat.Title);
                }
            }
            else if (lostAccess && existing is not null)
            {
                existing.IsActive = false;
                channels.Update(existing);
                _logger.LogInformation("Bot demoted/removed from channel {ChatId} — disabling cross-post.", cm.Chat.Id);
            }

            await uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process MyChatMember update for chat {ChatId}.", cm.Chat.Id);
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

    private Task HandlePollingErrorAsync(ITelegramBotClient client, Exception ex, CancellationToken ct)
    {
        _logger.LogWarning(ex, "Telegram polling error.");
        return Task.CompletedTask;
    }
}
