using System.Net;
using System.Net.Http.Json;
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
    }
}
