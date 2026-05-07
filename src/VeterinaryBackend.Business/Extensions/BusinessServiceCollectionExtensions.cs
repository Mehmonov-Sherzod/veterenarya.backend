using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddScoped<ISectionService, SectionService>();
        services.AddScoped<IContentService, ContentService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<ILabHeadService, LabHeadService>();
        services.AddScoped<ISectionHeadService, SectionHeadService>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        services.AddValidatorsFromAssemblyContaining<CreateContentDtoValidator>(ServiceLifetime.Scoped);

        return services;
    }
}
