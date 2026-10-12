using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Tenancy;
using SnpDepotManagement.Infrastructure.Identity;
using SnpDepotManagement.Infrastructure.Persistence;
using SnpDepotManagement.Infrastructure.Tenancy;

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

        // 2. Identity
        services.AddScoped<ICurrentUser, CurrentUser>();

        // 3. Persistence (EF Core + PostgreSQL Npgsql)
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["Database:ConnectionString"]
            ?? "Host=localhost;Port=5432;Database=snp_depot_db;Username=postgres;Password=postgres_password_123";

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            });
            options.UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return services;
    }
}
