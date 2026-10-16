using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Authentication;

[Collection("Api")]
public sealed class TenantAuthenticationTests(ApiFactory factory)
{
    [Fact]
    public async Task AnotherDepotsContainerIsNotListedAndCannotBeChangedByHeader()
    {
        await using var context = factory.Context("DEPOT-B");
        var container = Container.Create(ContainerNumber.Create("CSQU3054383").Value!,
            "22G1", ContainerSize.Ft20, ContainerType.Dry, 2000, 30000, null, "CSQ", "DEPOT-B").Value!;
        context.Containers.Add(container);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        using var client = await ApiTestClient.LoginAsync(factory);
        var listed = await client.GetFromJsonAsync<JsonElement>("/api/containers/", TestContext.Current.CancellationToken);
        Assert.DoesNotContain(listed.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == container.Id);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "DEPOT-B");
        var write = await client.PostAsJsonAsync("/api/containers/", new
        {
            ContainerNumber = "CSQU3054383", IsoCode = "22G1", Size = 20, Type = 85,
            TareWeight = 2000, MaxGrossWeight = 30000, OwnerCode = "ATTACK"
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
        await context.Entry(container).ReloadAsync(TestContext.Current.CancellationToken);
        Assert.Equal("CSQ", container.OwnerCode);
    }

    [Fact]
    public async Task RefreshRotatesTokenAndKeepsStoredDepot()
    {
        using var client = factory.Client();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { UserName = "admin", Password = ApiFactory.AdminPassword }, TestContext.Current.CancellationToken);
        var auth = await login.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var oldRefresh = auth.GetProperty("refreshToken").GetString();
        var refreshed = await client.PostAsJsonAsync("/api/auth/refresh", new { UserName = "admin", RefreshToken = oldRefresh }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var next = await refreshed.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.NotEqual(oldRefresh, next.GetProperty("refreshToken").GetString());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(next.GetProperty("accessToken").GetString());
        Assert.Equal("DEPOT-A", jwt.Claims.Single(c => c.Type == "tenant_id").Value);
        var replay = await client.PostAsJsonAsync("/api/auth/refresh", new { UserName = "admin", RefreshToken = oldRefresh }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [Fact]
    public async Task LoginWithoutTenantHeaderIssuesTokenBoundToStoredDepot()
    {
        using var client = factory.Client();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { UserName = "admin", Password = ApiFactory.AdminPassword });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.GetProperty("accessToken").GetString());
        Assert.Equal("DEPOT-A", jwt.Claims.Single(c => c.Type == "tenant_id").Value);
    }

    [Fact]
    public async Task ProtectedEndpointWithoutTokenReturns401RatherThanTenant400()
    {
        using var client = factory.Client();
        var response = await client.GetAsync("/api/containers/");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenCannotBeUsedToSelectAnotherDepotWithHeader()
    {
        using var client = factory.Client();
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { UserName = "admin", Password = ApiFactory.AdminPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            auth.GetProperty("accessToken").GetString());
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "DEPOT-B");
        var response = await client.GetAsync("/api/containers/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
