using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Domain.Entities.Identity;

namespace SnpDepotManagement.Infrastructure.Persistence.Seed;

public interface IAppDbSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

public sealed class AppDbSeeder : IAppDbSeeder
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppDbSeeder> _logger;

    public AppDbSeeder(
        AppDbContext _context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<AppDbSeeder> logger)
    {
        this._context = _context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(cancellationToken);
        await SeedAdminUserAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        var predefinedRoles = new[]
        {
            ("Admin", "Quản trị hệ thống toàn quyền"),
            ("YardPlanner", "Điều phối bãi và xếp dỡ container"),
            ("OrderClerk", "Quản lý đơn hàng và cấp phát lệnh giao nhận"),
            ("GateClerk", "Kiểm soát cổng vào ra (Gate In / Gate Out)")
        };

        foreach (var (name, description) in predefinedRoles)
        {
            var exists = await _context.Roles.AnyAsync(r => r.Name == name, cancellationToken);
            if (!exists)
            {
                var roleResult = Role.Create(name, description);
                if (roleResult.IsSuccess)
                {
                    _context.Roles.Add(roleResult.Value!);
                    _logger.LogInformation("Seeded role: {RoleName}", name);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        var adminUserName = _configuration["Admin:UserName"] ?? "admin";
        var adminEmail = _configuration["Admin:Email"] ?? "admin@snp.com.vn";
        var adminPassword = _configuration["Admin:Password"] ?? "Admin@123456";
        var adminFirstName = _configuration["Admin:FirstName"] ?? "System";
        var adminLastName = _configuration["Admin:LastName"] ?? "Administrator";

        var adminExists = await _context.Users.AnyAsync(u => u.UserName == adminUserName || u.Email == adminEmail, cancellationToken);
        if (adminExists)
        {
            return;
        }

        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin", cancellationToken);
        if (adminRole is null)
        {
            _logger.LogWarning("Admin role not found during admin user seeding.");
            return;
        }

        var passwordHash = _passwordHasher.HashPassword(adminPassword);
        var userResult = ApplicationUser.Create(
            adminUserName,
            adminEmail,
            adminFirstName,
            adminLastName,
            adminRole.Name,
            passwordHash);

        if (userResult.IsFailure)
        {
            _logger.LogError("Failed to create admin user: {Error}", userResult.Error.Description);
            return;
        }

        var adminUser = userResult.Value!;
        adminUser.AddRole(adminRole.Id);

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully seeded default Admin user: {UserName}", adminUserName);
    }
}
