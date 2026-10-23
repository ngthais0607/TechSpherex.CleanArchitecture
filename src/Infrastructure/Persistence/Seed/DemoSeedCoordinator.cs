using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Domain.Entities.Identity;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Infrastructure.Persistence.Seed;

public sealed class DemoSeedCoordinator(AppDbContext context, IConfiguration configuration,
    IPasswordHasher passwordHasher, TimeProvider timeProvider)
{
    internal const string GlobalLockSql = "SELECT pg_advisory_xact_lock(72640, 1)";

    public async Task RunAsync(string tenantId, CancellationToken ct)
    {
        await using var owned = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(ct) : null;
        await context.Database.ExecuteSqlRawAsync(GlobalLockSql, ct);
        var key = tenantId + ":" + DemoDataset.Version;
        await context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(72641, hashtext({key}))", ct);
        if (await context.DemoSeedRuns.AnyAsync(run => run.TenantId == tenantId && run.DatasetVersion == DemoDataset.Version, ct))
        {
            if (owned is not null) await owned.CommitAsync(ct);
            return;
        }
        await RefuseConflictingDataAsync(tenantId, ct);
        await SeedUsersAsync(tenantId, ct);
        await DemoDatasetWriter.WriteAsync(context, tenantId, timeProvider.GetUtcNow().UtcDateTime, ct);
        context.DemoSeedRuns.Add(new DemoSeedRun(tenantId, DemoDataset.Version, timeProvider.GetUtcNow().UtcDateTime));
        await context.SaveChangesAsync(ct);
        if (owned is not null) await owned.CommitAsync(ct);
    }

    private async Task RefuseConflictingDataAsync(string tenantId, CancellationToken ct)
    {
        var block = await context.YardBlocks.IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId && DemoDataset.BlockCodes.Contains(item.Code))
            .Select(item => item.Code).FirstOrDefaultAsync(ct);
        var order = await context.DeliveryOrders.IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId && DemoDataset.OrderNumbers.Contains(item.OrderNumber))
            .Select(item => item.OrderNumber).FirstOrDefaultAsync(ct);
        var customer = await context.Customers.IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId && DemoDataset.CustomerTaxCodes.Contains(item.TaxCode))
            .Select(item => item.TaxCode).FirstOrDefaultAsync(ct);
        var numbers = DemoDataset.Containers.Select(spec => ContainerNumber.Create(spec.Number).Value!).ToList();
        var containerExists = await context.Containers.IgnoreQueryFilters()
            .AnyAsync(item => item.TenantId == tenantId && numbers.Contains(item.ContainerNumber), ct);
        if (block is not null || order is not null || customer is not null || containerExists)
            throw new InvalidOperationException($"Demo seed conflict for tenant {tenantId}: {block ?? order ?? customer ?? "container number"} exists without dataset marker. Use a separate development database; existing data was not changed.");
    }

    private async Task SeedUsersAsync(string tenantId, CancellationToken ct)
    {
        var password = configuration["Seed:UserPassword"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Seed:UserPassword must be configured when creating development demo users.");
        foreach (var (name, roleName) in new[] { ("yard", "YardPlanner"), ("order", "OrderClerk"), ("gate", "GateClerk") })
        {
            var role = await context.Roles.SingleOrDefaultAsync(role => role.Name == roleName, ct)
                ?? throw new InvalidOperationException($"Bootstrap role {roleName} must exist before demo seeding.");
            var email = $"{name}@demo.snp.local";
            var existing = await context.Users.Include(user => user.UserRoles)
                .SingleOrDefaultAsync(user => user.UserName == name || user.Email == email, ct);
            if (existing is not null)
            {
                if (existing.UserName != name || existing.TenantId != tenantId || existing.Role != roleName || !existing.IsActive ||
                    !existing.UserRoles.Any(userRole => userRole.RoleId == role.Id))
                    throw new InvalidOperationException($"Demo user {name} conflicts with an existing account. Its password, depot and roles were not changed.");
                continue;
            }
            var result = ApplicationUser.Create(name, email, roleName, "Demo", tenantId, roleName, passwordHasher.HashPassword(password));
            if (result.IsFailure) throw new InvalidOperationException($"Cannot create demo user {name}: {result.Error.Code}");
            result.Value!.AddRole(role.Id);
            context.Users.Add(result.Value);
        }
        await context.SaveChangesAsync(ct);
    }
}
