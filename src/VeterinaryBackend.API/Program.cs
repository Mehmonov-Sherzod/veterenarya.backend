using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using VeterinaryBackend.API.Middleware;
using VeterinaryBackend.Business.Extensions;
using VeterinaryBackend.Business.Options;
using VeterinaryBackend.DataAccess.Context;
using VeterinaryBackend.DataAccess.Extensions;
using VeterinaryBackend.DataAccess.Repositories;
using VeterinaryBackend.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt section is missing from configuration.");

if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey))

    throw new InvalidOperationException("Jwt:SecretKey is not configured.");

builder.Services
    .AddControllers(options =>
    {
        options.SuppressAsyncSuffixInActionNames = false;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

const long maxUploadSize = 524_288_000;
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = maxUploadSize;
    o.ValueLengthLimit = int.MaxValue;
    o.MultipartHeadersLengthLimit = int.MaxValue;
});

builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = maxUploadSize);

builder.Services.AddDataAccess(connectionString);
builder.Services.AddBusinessServices(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtOptions.SecretKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Veterinary Backend API",
        Version = "v1",
        Description = "Veterinariya va oziq-ovqat xavfsizligi — ko'p tilli (UZ/RU/EN) informatsion backend. Bo'limlar va kontentlar CRUD, fayl yuklash (har qanday format), JWT admin auth."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste only the JWT (without 'Bearer ' prefix). Get it from POST /api/v1/auth/login."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    options.AddSecurityDefinition("AcceptLanguage", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "Accept-Language",
        Description = "Set 'uz', 'ru' or 'en' to receive localized content."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

// Bind config sections used by the CORS + database hardening below.
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));

var corsAllowed = builder.Configuration
    .GetSection(CorsOptions.SectionName)
    .Get<CorsOptions>()?.AllowedOrigins ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsAllowed.Length == 0)
        {
            // Dev / unconfigured — permissive. Production MUST set Cors:AllowedOrigins.
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(corsAllowed)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// Rate limiting — protects login + media-upload from brute force / abuse.
// All other endpoints fall back to the global "public" limiter applied to anonymous
// requests by default (admin JWT-bearing requests bypass it via PartitionedRateLimiter).
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    opts.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    opts.AddPolicy("upload", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ctx.User.Identity?.Name ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    opts.AddPolicy("public", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 200,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

var app = builder.Build();

// Auto-migrate the database to the latest schema and seed the bootstrap super-admin
// from AdminOptions when the users table is empty. Idempotent: safe to run on every
// startup. If the DB is unreachable we log and continue so endpoints can still serve
// readonly requests against existing infrastructure (admin will fail until DB is up).
//
// Multi-instance prod deploys should set Database:AutoMigrateOnStartup=false and
// run `dotnet ef database update` as a one-shot pre-deploy step instead, to avoid
// races between pods.
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var logger = sp.GetRequiredService<ILogger<Program>>();
    var dbOpts = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    try
    {
        var db = sp.GetRequiredService<AppDbContext>();
        if (dbOpts.AutoMigrateOnStartup)
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            logger.LogInformation("Database:AutoMigrateOnStartup=false — skipping migrations. Run them manually before promoting a new build.");
        }

        var users = sp.GetRequiredService<IUserRepository>();
        if (!await users.AnyAsync())
        {
            var adminOpts = sp.GetRequiredService<IOptions<AdminOptions>>().Value;

            var seedUsername = string.IsNullOrWhiteSpace(adminOpts.Username) ? "admin" : adminOpts.Username.Trim();
            var seedHash = string.IsNullOrWhiteSpace(adminOpts.PasswordHash)
                ? BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 11)
                : adminOpts.PasswordHash;

            await users.AddAsync(new User
            {
                Username = seedUsername,
                PasswordHash = seedHash,
                Role = "Admin",
                IsActive = true
            });
            await db.SaveChangesAsync();

            logger.LogInformation("Seeded super-admin user '{Username}' into the database.", seedUsername);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration/seed failed at startup.");
    }
}

var webRoot = app.Environment.WebRootPath;
if (string.IsNullOrEmpty(webRoot))
{
    webRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    Directory.CreateDirectory(webRoot);
}
Directory.CreateDirectory(Path.Combine(webRoot, "uploads"));

if (!app.Environment.IsDevelopment())
{
    // Tells browsers to upgrade subsequent visits to HTTPS for one year.
    app.UseHsts();
}

app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseMiddleware<LanguageMiddleware>();
app.UseRateLimiter();

// Lightweight liveness/readiness probes for load balancers + k8s.
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }))
    .AllowAnonymous();
app.MapGet("/ready", async (AppDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "ready" });
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Veterinary Backend API v1");
        c.RoutePrefix = "swagger";
    });
}

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (path == "/" || path == "/admin")
    {
        context.Response.Redirect("/admin/", permanent: false);
        return;
    }
    await next();
});

app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = new List<string> { "index.html" }
});
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.Context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            ctx.Context.Response.Headers["Pragma"] = "no-cache";
            ctx.Context.Response.Headers["Expires"] = "0";
        }
    }
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
