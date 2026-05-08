using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using VeterinaryBackend.Business.Options;
using VeterinaryBackend.Business.Services;
using VeterinaryBackend.Business.Validators;

namespace VeterinaryBackend.Business.Extensions;

public static class BusinessServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AdminOptions>(configuration.GetSection(AdminOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<SiteOptions>(configuration.GetSection(SiteOptions.SectionName));
        services.Configure<TelegramBotOptions>(configuration.GetSection(TelegramBotOptions.SectionName));

        services.AddScoped<ISectionService, SectionService>();
        services.AddScoped<IContentService, ContentService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<ILabHeadService, LabHeadService>();
        services.AddScoped<ISectionHeadService, SectionHeadService>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // Telegram bot — single shared HttpClient + bot client. Returns null when token missing
        // so the rest of the app can be DI'd without a real bot in dev/local.
        var botSection = configuration.GetSection(TelegramBotOptions.SectionName).Get<TelegramBotOptions>();
        if (botSection is { Enabled: true } && !string.IsNullOrWhiteSpace(botSection.BotToken))
        {
            services.AddHttpClient("telegram_bot_client").AddTypedClient<ITelegramBotClient>(
                (httpClient, sp) => new TelegramBotClient(botSection.BotToken, httpClient));
        }
        else
        {
            services.AddSingleton<ITelegramBotClient>(_ => null!);
        }
        services.AddSingleton<ITelegramBotService, TelegramBotService>();
        services.AddHostedService<TelegramBotHostedService>();

        services.AddValidatorsFromAssemblyContaining<CreateContentDtoValidator>(ServiceLifetime.Scoped);

        return services;
    }
}
