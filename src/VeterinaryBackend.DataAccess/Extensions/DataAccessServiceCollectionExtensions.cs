using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VeterinaryBackend.DataAccess.Context;
using VeterinaryBackend.DataAccess.Repositories;
using UnitOfWorkNs = VeterinaryBackend.DataAccess.UnitOfWork;

namespace VeterinaryBackend.DataAccess.Extensions;

public static class DataAccessServiceCollectionExtensions
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ISectionRepository, SectionRepository>();
        services.AddScoped<IContentRepository, ContentRepository>();
        services.AddScoped<IMediaFileRepository, MediaFileRepository>();
        services.AddScoped<UnitOfWorkNs.IUnitOfWork, UnitOfWorkNs.UnitOfWork>();

        return services;
    }
}
