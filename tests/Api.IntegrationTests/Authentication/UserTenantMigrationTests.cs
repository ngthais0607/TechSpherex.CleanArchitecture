using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using NSubstitute;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Tenancy;
using SnpDepotManagement.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Authentication;

public sealed class UserTenantMigrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpgradeRequiresAnExplicitMappingForEveryExistingAccount(bool mapUser)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var postgres = new PostgreSqlBuilder().WithImage("postgres:17-alpine")
            .WithDatabase("upgrade_test").WithUsername("test_user").WithPassword("test_password").Build();
        await postgres.StartAsync(ct);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(postgres.GetConnectionString()).UseSnakeCaseNamingConvention().Options;
        await using var db = new AppDbContext(options, Substitute.For<ITenantProvider>(), TimeProvider.System, Substitute.For<ICurrentUser>());
        var migrator = db.GetService<IMigrator>();
        var expand = db.Database.GetMigrations().Single(m => m.EndsWith("_AddUserTenant", StringComparison.Ordinal));
        await migrator.MigrateAsync(expand, ct);
        var userId = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO application_users (id,user_name,email,first_name,last_name,password_hash,role,is_active,created_at)
            VALUES ({userId},'legacy','legacy@example.test','Legacy','User','test_hash','Admin',true,now())
            """, ct);
        if (!mapUser)
        {
            var failure = await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync(cancellationToken: ct));
            Assert.Equal(PostgresErrorCodes.NotNullViolation, failure.SqlState);
            return;
        }
        await db.Database.OpenConnectionAsync(ct);
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            await db.Database.ExecuteSqlRawAsync("CREATE TEMP TABLE user_tenant_mapping(user_id uuid PRIMARY KEY,tenant_id varchar(50) NOT NULL)", ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO user_tenant_mapping VALUES ({userId},'DEPOT-LEGACY')", ct);
            var script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "apply-user-tenants.sql"), ct);
            await db.Database.ExecuteSqlRawAsync(script, ct);
            await transaction.CommitAsync(ct);
        }
        await migrator.MigrateAsync(cancellationToken: ct);
        var user = await db.Users.AsNoTracking().SingleAsync(ct);
        Assert.Equal("DEPOT-LEGACY", user.TenantId);
    }
}
