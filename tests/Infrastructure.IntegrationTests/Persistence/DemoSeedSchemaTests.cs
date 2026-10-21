using Npgsql;
using SnpDepotManagement.Infrastructure.IntegrationTests.Fixtures;
using Xunit;

namespace SnpDepotManagement.Infrastructure.IntegrationTests.Persistence;

[Collection("PostgreSqlCollection")]
public sealed class DemoSeedSchemaTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task SeedMarkerTable_ExistsAfterMigrations()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand("SELECT to_regclass('public.demo_seed_runs')::text", connection);
        Assert.Equal("demo_seed_runs", await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SameVersionInDifferentTenant_IsAllowed()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var suffix = Guid.NewGuid().ToString("N");
        await using var command = new NpgsqlCommand("""
            INSERT INTO demo_seed_runs (tenant_id, dataset_version, completed_at_utc)
            VALUES (@tenantA, 'fe-be-v1', now()), (@tenantB, 'fe-be-v1', now());
            SELECT count(*) FROM demo_seed_runs WHERE tenant_id IN (@tenantA, @tenantB);
            """, connection);
        command.Parameters.AddWithValue("tenantA", "A-" + suffix);
        command.Parameters.AddWithValue("tenantB", "B-" + suffix);
        Assert.Equal(2L, await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DuplicateDatasetVersionInSameTenant_IsRejected()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand("""
            INSERT INTO demo_seed_runs (tenant_id, dataset_version, completed_at_utc)
            VALUES (@tenant, 'fe-be-v1', now());
            """, connection);
        command.Parameters.AddWithValue("tenant", "D-" + Guid.NewGuid().ToString("N"));
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, exception.SqlState);
    }
}
