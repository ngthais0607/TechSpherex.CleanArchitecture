using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Authentication;

[Collection("Api")]
public sealed class AuthMeTests(ApiFactory factory)
{
    [Fact]
    public async Task GetCurrentUser_WhenAuthenticated_Returns200WithUserInfo()
    {
        using var client = await ApiTestClient.LoginAsync(factory);

        var response = await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("admin", json.GetProperty("userName").GetString());
        Assert.Contains(json.GetProperty("roles").EnumerateArray(), r => r.GetString() == "Admin");
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("tenantId").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("depotName").GetString()));
    }

    [Fact]
    public async Task GetCurrentUser_WhenUnauthenticated_Returns401()
    {
        using var client = factory.Client();

        var response = await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WhenAuthenticated_RevokesRefreshToken()
    {
        using var client = factory.Client();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            UserName = "admin",
            Password = ApiFactory.AdminPassword
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var auth = await loginResponse.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var accessToken = auth.GetProperty("accessToken").GetString();
        var refreshToken = auth.GetProperty("refreshToken").GetString();

        // Call Logout with accessToken
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var logoutResponse = await client.PostAsync("/api/auth/logout", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        // Subsequent refresh using revoked token should fail
        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            UserName = "admin",
            RefreshToken = refreshToken
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }
}
