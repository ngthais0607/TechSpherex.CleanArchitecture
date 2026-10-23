using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SnpDepotManagement.Application.Abstractions.Caching;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Tenancy;
using SnpDepotManagement.Infrastructure.Caching;
using SnpDepotManagement.Infrastructure.Identity;
using SnpDepotManagement.Infrastructure.Health;
using SnpDepotManagement.Infrastructure.Persistence;
using SnpDepotManagement.Infrastructure.Persistence.Seed;
using SnpDepotManagement.Infrastructure.Tenancy;
using StackExchange.Redis;

namespace SnpDepotManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. HttpContext & Tenancy
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddTransient<TenantMiddleware>();

        // 2. Identity & Security
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(jwtSection);
        var jwtSecret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32 || jwtSecret.StartsWith("REPLACE_", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Jwt:Secret must be configured with at least 32 characters and must not be an example placeholder.");

        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ITokenService, TokenService>();

        // Authentication & JWT Bearer
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        // 3. Persistence (EF Core + PostgreSQL Npgsql)
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["Database:ConnectionString"]
            ?? throw new InvalidOperationException("A database connection string must be configured.");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("A database connection string must be configured.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            });
            options.UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IAppDbSeeder, AppDbSeeder>();
        services.AddScoped<IDemoDataSeeder, DemoDataSeeder>();

        // 4. Caching (.NET 9 HybridCache + Redis)
#pragma warning disable EXTEXP0018
        services.AddHybridCache();
#pragma warning restore EXTEXP0018

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? configuration["Redis:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var options = ConfigurationOptions.Parse(redisConnectionString);
                options.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(options);
            });
            services.AddStackExchangeRedisCache(opt => opt.Configuration = redisConnectionString);
        }

        services.AddScoped<ICacheService, HybridCacheService>();
        services.AddHealthChecks().AddCheck<DependencyHealthCheck>("dependencies", timeout: TimeSpan.FromSeconds(5));

        services.AddSingleton<SnpDepotManagement.Application.Abstractions.Configuration.IGateSettings, SnpDepotManagement.Infrastructure.Configuration.GateSettings>();
        services.AddHostedService<SnpDepotManagement.Infrastructure.BackgroundServices.BookingExpirationService>();

        return services;
    }
}
