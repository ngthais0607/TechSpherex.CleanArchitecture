using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Endpoints;

public sealed class DevelopmentTransportTests
{
    [Fact]
    public async Task HttpsOptIn_RetainsRedirection()
    {
        await using var factory = new DevelopmentApiFactory();
        await using var host = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("Transport:UseHttpsRedirection", "true"));
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false
        });
        using var response = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
        Assert.Equal("https", response.Headers.Location?.Scheme);
    }

    [Fact]
    public async Task HttpDevelopment_DoesNotRedirectRequestsOrPreflight()
    {
        await using var host = new DevelopmentApiFactory();
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false
        });
        var ct = TestContext.Current.CancellationToken;
        using var live = await client.GetAsync("/health/live", ct);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Null(live.Headers.Location);
        using var options = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        options.Headers.Add("Origin", "http://localhost:4200");
        options.Headers.Add("Access-Control-Request-Method", "POST");
        options.Headers.Add("Access-Control-Request-Headers", "content-type");
        using var preflight = await client.SendAsync(options, ct);
        Assert.Equal(HttpStatusCode.NoContent, preflight.StatusCode);
        Assert.Null(preflight.Headers.Location);
    }
}
