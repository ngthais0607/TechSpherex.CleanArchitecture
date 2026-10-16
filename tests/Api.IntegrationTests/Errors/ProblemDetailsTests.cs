using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Errors;

[Collection("Api")]
public sealed class ProblemDetailsTests(ApiFactory factory)
{
    [Theory]
    [InlineData("size=999")]
    [InlineData("condition=999")]
    public async Task UndefinedQueryEnumIsRejected(string query)
    {
        using var client = await ApiTestClient.LoginAsync(factory);
        var response = await client.GetAsync("/api/containers/?" + query, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("{", 400)]
    [InlineData("{}", 400)]
    [InlineData("{\"userName\":\"nobody\",\"password\":\"IncorrectPassword!\"}", 401)]
    public async Task LoginErrorsUseSafeProblemDetails(string body, int expectedStatus)
    {
        using var client = factory.Client();
        var response = await client.PostAsync("/api/auth/login", new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal((HttpStatusCode)expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expectedStatus, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("code").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
        var text = problem.ToString();
        Assert.DoesNotContain("StackTrace", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnauthenticatedRequestUsesProblemDetails()
    {
        using var client = factory.Client();
        var response = await client.GetAsync("/api/containers/");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Auth.Unauthorized", problem.GetProperty("code").GetString());
    }
}
