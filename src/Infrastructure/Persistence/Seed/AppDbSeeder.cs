using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Domain.Entities.Identity;
using SnpDepotManagement.Domain.Entities.MasterData;

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
    private readonly IDemoDataSeeder _demoDataSeeder;

    public AppDbSeeder(
        AppDbContext _context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<AppDbSeeder> logger,
        IDemoDataSeeder demoDataSeeder)
    {
        this._context = _context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
        _demoDataSeeder = demoDataSeeder;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedDepotAsync(cancellationToken);
        await SeedRolesAsync(cancellationToken);
        await SeedAdminUserAsync(cancellationToken);
        await _demoDataSeeder.SeedAsync(cancellationToken);
    }

    private async Task SeedDepotAsync(CancellationToken cancellationToken)
    {
        var depotName = _configuration["Depot:Name"] ?? "SNP Depot";
        var depotTimezone = _configuration["Depot:Timezone"] ?? "Asia/Ho_Chi_Minh";
        var tenantIdStr = _configuration["Admin:TenantId"] ?? "DEPOT-A";

        if (!Guid.TryParse(tenantIdStr, out var depotId))
        {
            var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(tenantIdStr));
            depotId = new Guid(hash);
        }

        var exists = await _context.Depots.AnyAsync(d => d.Id == depotId || d.Name == depotName, cancellationToken);
        if (!exists)
        {
            var depot = Depot.Create(depotId, depotName, depotTimezone);
            _context.Depots.Add(depot);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded depot: {DepotName}", depotName);
        }
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
        var adminPassword = _configuration["Admin:Password"];
        var adminTenantId = _configuration["Admin:TenantId"];
        if (string.IsNullOrWhiteSpace(adminPassword) || string.IsNullOrWhiteSpace(adminTenantId))
            throw new InvalidOperationException("Admin:Password and Admin:TenantId must be configured.");
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
            adminTenantId,
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
