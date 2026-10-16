using System.Net;
using System.Net.Http.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Reports;

[Collection("Api")]
public sealed class ReportAuthorizationTests(ApiFactory factory)
{
    [Theory]
    [InlineData("Admin")]
    [InlineData("YardPlanner")]
    [InlineData("OrderClerk")]
    [InlineData("GateClerk")]
    public async Task ReportPoliciesAllowEachRoleAndHideOtherDepotHistory(string role)
    {
        var ct = TestContext.Current.CancellationToken;
        using var admin = await ApiTestClient.LoginAsync(factory);
        var user = "report" + Guid.NewGuid().ToString("N")[..8];
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/auth/register", new
        { UserName = user, Email = user + "@example.test", FirstName = "Report", LastName = "Role", Password = ApiFactory.AdminPassword, Role = role }, ct)).StatusCode);
        using var client = await ApiTestClient.LoginAsync(factory, user);
        var own = OperationScenario.NewContainer();
        var other = OperationScenario.NewContainer(tenant: "DEPOT-B");
        await using var db = factory.Context("DEPOT-A"); db.Containers.AddRange(own, other); await db.SaveChangesAsync(ct);
        foreach (var route in new[] { "/api/reports/yard-inventory", "/api/reports/dwell-times", $"/api/reports/containers/{own.ContainerNumber.Value}/history" })
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(route, ct)).StatusCode);
            using var anonymous = factory.Client();
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(route, ct)).StatusCode);
        }
        Assert.Equal(role == "YardPlanner" ? HttpStatusCode.Forbidden : HttpStatusCode.OK,
            (await client.GetAsync("/api/reports/expiring-orders", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/reports/containers/{other.ContainerNumber.Value}/history", ct)).StatusCode);
    }

    [Theory]
    [InlineData("yard-inventory?size=999")]
    [InlineData("yard-inventory?type=999")]
    [InlineData("yard-inventory?condition=999")]
    [InlineData("yard-inventory?blockId=invalid")]
    [InlineData("dwell-times?minDays=-1")]
    [InlineData("dwell-times?includeGatedOut=oops")]
    [InlineData("expiring-orders?days=-1")]
    [InlineData("expiring-orders?days=366")]
    [InlineData("expiring-orders?pageSize=101")]
    public async Task InvalidReportQueriesReturn400(string query)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);
        using var response = await client.GetAsync("/api/reports/" + query, ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
