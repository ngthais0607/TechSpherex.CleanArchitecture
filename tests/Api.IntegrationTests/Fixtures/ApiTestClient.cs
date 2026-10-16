using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Fixtures;

public static class ApiTestClient
{
    public static async Task<HttpClient> LoginAsync(ApiFactory factory, string userName = "admin", string password = ApiFactory.AdminPassword)
    {
        var client = factory.Client();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { userName, password }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.GetProperty("accessToken").GetString());
        return client;
    }
}
