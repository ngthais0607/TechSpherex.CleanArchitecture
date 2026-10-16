using System.Data.Common;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Infrastructure.Persistence;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Reports;

[Collection("Api")]
public sealed class ReportPerformanceTests
{
    [Fact]
    public async Task ReportsOnTwentyThousandContainersHaveBoundedQueriesAndP95BelowOneSecond()
    {
        var ct = TestContext.Current.CancellationToken;
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "SnpDepotManagement.sln"))) root = root.Parent;
        Assert.NotNull(root);
        var evidence = Path.Combine(root.FullName, "docs", "week-04", "performance-evidence");
        Directory.CreateDirectory(evidence);
        // A separate container ensures neither shared test data nor any developer DB is modified.
        await using var factory = new ApiFactory();
        await factory.InitializeAsync();
        await using var db = factory.Context("DEPOT-A");
        await db.Database.OpenConnectionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SET snp.allow_report_benchmark = 'true'", ct);
        await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(Path.Combine(root.FullName, "scripts", "benchmarks", "seed-reports.sql"), ct), ct);
        Assert.Equal(20000, await db.Containers.IgnoreQueryFilters().CountAsync(ct));
        using var authenticated = await ApiTestClient.LoginAsync(factory);
        var capture = new QueryCapture();
        await using var host = factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
            services.AddDbContext<AppDbContext>(options => options.AddInterceptors(capture))));
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = authenticated.DefaultRequestHeaders.Authorization;
        var numbers = await db.Containers.OrderBy(c => c.Id).Take(2).Select(c => c.ContainerNumber).ToListAsync(ct);
        var cases = new (string Name, string Route, int Queries)[]
        {
            ("inventory-all", "/api/reports/yard-inventory", 3),
            ("inventory-selective", "/api/reports/yard-inventory?lineOperator=BCH&size=20", 3),
            ("dwell-all", "/api/reports/dwell-times?includeGatedOut=true", 2),
            ("dwell-selective", "/api/reports/dwell-times?lineOperator=BCH&minDays=30", 2),
            ("history-first", $"/api/reports/containers/{numbers[0].Value}/history", 3),
            ("history-second", $"/api/reports/containers/{numbers[1].Value}/history", 3),
            ("expiry-week", "/api/reports/expiring-orders?days=7", 2),
            ("expiry-today", "/api/reports/expiring-orders?days=0&lineOperator=BCH", 2)
        };
        var results = new List<object>();
        foreach (var (name, route, queryCount) in cases)
        {
            for (var i = 0; i < 5; i++) { using var warmup = await client.GetAsync(route, ct); warmup.EnsureSuccessStatusCode(); }
            var timings = new List<double>();
            for (var i = 0; i < 30; i++)
            {
                capture.Commands.Clear();
                var timer = Stopwatch.StartNew();
                using var response = await client.GetAsync(route, ct);
                await response.Content.ReadAsByteArrayAsync(ct);
                timer.Stop();
                response.EnsureSuccessStatusCode();
                Assert.Equal(queryCount, capture.Commands.Count);
                timings.Add(timer.Elapsed.TotalMilliseconds);
            }
            var p95 = timings.Order().ElementAt((int)Math.Ceiling(timings.Count * .95) - 1);
            results.Add(new { name, route, count = 30, warmup = 5, queryCount, p95Milliseconds = p95, timingsMilliseconds = timings });
            var output = new StringBuilder();
            foreach (var command in capture.Commands)
            {
                output.AppendLine("-- Parameters: " + JsonSerializer.Serialize(command.Parameters.Select(p => new { p.Name, Type = p.Type.ToString(), p.Value })));
                output.AppendLine(command.Sql);
                await using var explain = new NpgsqlCommand("EXPLAIN (ANALYZE, BUFFERS) " + command.Sql, (NpgsqlConnection)db.Database.GetDbConnection());
                foreach (var p in command.Parameters) explain.Parameters.Add(new NpgsqlParameter(p.Name, p.Type) { Value = p.Value });
                await using var reader = await explain.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct)) output.AppendLine(reader.GetString(0));
                output.AppendLine();
            }
            await File.WriteAllTextAsync(Path.Combine(evidence, name + ".txt"), output.ToString(), ct);
            await File.WriteAllTextAsync(Path.Combine(evidence, "timings.json"), JsonSerializer.Serialize(new
            {
                measuredAtUtc = DateTime.UtcNow, os = Environment.OSVersion.ToString(), processors = Environment.ProcessorCount,
                memoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes, dotnet = Environment.Version.ToString(),
                database = "PostgreSQL 17-alpine / disposable Testcontainers", containers = 20000, tenants = 2, results
            }, new JsonSerializerOptions { WriteIndented = true }), ct);
            Assert.True(p95 < 1000, $"{name} p95 {p95:F2} ms exceeds 1000 ms; timings and plans saved for diagnosis.");
        }
    }

    private sealed record Parameter(string Name, NpgsqlDbType Type, object Value);
    private sealed record CapturedCommand(string Sql, Parameter[] Parameters);
    private sealed class QueryCapture : DbCommandInterceptor
    {
        public List<CapturedCommand> Commands { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Commands.Add(new(command.CommandText, command.Parameters.Cast<NpgsqlParameter>()
                .Select(p => new Parameter(p.ParameterName, p.NpgsqlDbType, p.Value!)).ToArray()));
            return ValueTask.FromResult(result);
        }
    }
}
