using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Authentication;

[Collection("Api")]
public sealed class DemoAuthTests(ApiFactory factory)
{
    [Theory]
    [InlineData(" admin ", "admin")]
    [InlineData("admin", " admin ")]
    public async Task LoginAndRefresh_TrimUsername(string loginUsername, string refreshUsername)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.Client();
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { userName = loginUsername, password = ApiFactory.AdminPassword }, ct);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<JsonElement>(ct);
        var refresh = await client.PostAsJsonAsync("/api/auth/refresh",
            new { userName = refreshUsername, refreshToken = auth.GetProperty("refreshToken").GetString() }, ct);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
    }

    [Fact]
    public async Task InvalidAuthentication_IsRejected_ReviewDiagnostics()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.Client();
        var missingTimes = new List<double>();
        var existingTimes = new List<double>();
        string? missingCode = null, existingCode = null;
        for (var i = 0; i < 30; i++)
        {
            foreach (var (name, times) in new[] { ("demo-missing-user", missingTimes), ("admin", existingTimes) })
            {
                var timer = Stopwatch.StartNew();
                using var response = await client.PostAsJsonAsync("/api/auth/login", new { userName = name, password = "InvalidPassword!123" }, ct);
                times.Add(timer.Elapsed.TotalMilliseconds);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
                if (name == "admin") existingCode = body.GetProperty("code").GetString();
                else missingCode = body.GetProperty("code").GetString();
            }
        }
        missingTimes.Sort(); existingTimes.Sort();
        TestContext.Current.TestOutputHelper!.WriteLine($"M3: missing={missingCode}, existing={existingCode}; medianMs missing={missingTimes[15]:F1}, existing={existingTimes[15]:F1}");

        var throttled = 0;
        for (var i = 0; i < 50; i++)
        {
            using var response = await client.PostAsJsonAsync("/api/auth/refresh",
                new { userName = "demo-missing-user", refreshToken = "invalid-refresh-token" }, ct);
            Assert.True(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.TooManyRequests);
            if (response.StatusCode == HttpStatusCode.TooManyRequests) throttled++;
        }
        TestContext.Current.TestOutputHelper!.WriteLine($"M4: refresh 429 responses={throttled}/50");
    }
}
