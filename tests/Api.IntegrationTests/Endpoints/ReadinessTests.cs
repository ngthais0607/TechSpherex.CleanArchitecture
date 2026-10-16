using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Endpoints;

[Collection("Api")]
public sealed class ReadinessTests(ApiFactory factory)
{
    [Fact]
    public async Task RequiredRedisCannotReportReadyWhenNotConfigured()
    {
        var ct = TestContext.Current.CancellationToken;
        using var auth = await ApiTestClient.LoginAsync(factory);
        await using var host = factory.WithWebHostBuilder(b => b.UseSetting("Redis:Required", "true"));
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = auth.DefaultRequestHeaders.Authorization;
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/ready", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live", ct)).StatusCode);
    }

    [Fact]
    public async Task OptionalRedisIsReportedAsDegradedToAdmin()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);
        var response = await client.GetAsync("/health/ready", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Degraded", await response.Content.ReadAsStringAsync(ct));
        using var anonymous = factory.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/health/ready", ct)).StatusCode);
    }

    [Fact]
    public async Task DatabaseOutageFailsReadinessWhileProcessRemainsLive()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var isolated = new ApiFactory(); await isolated.InitializeAsync();
        using var client = await ApiTestClient.LoginAsync(isolated);
        await isolated.StopDatabaseAsync(ct);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/ready", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live", ct)).StatusCode);
    }
}
