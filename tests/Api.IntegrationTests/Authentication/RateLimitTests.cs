using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Authentication;

[Collection("Api")]
public sealed class RateLimitTests(ApiFactory factory)
{
    [Fact]
    public async Task Login_WhenExceedingLimit_Returns429TooManyRequests()
    {
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "192.168.1.150");

        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 6; i++)
        {
            lastResponse = await client.PostAsJsonAsync("/api/auth/login", new
            {
                UserName = "invalid_user",
                Password = "WrongPassword123!"
            }, TestContext.Current.CancellationToken);
        }

        Assert.NotNull(lastResponse);
        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
        var problem = await lastResponse.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("RateLimit.TooManyRequests", problem.GetProperty("code").GetString());
        Assert.Equal(429, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
    }
}
