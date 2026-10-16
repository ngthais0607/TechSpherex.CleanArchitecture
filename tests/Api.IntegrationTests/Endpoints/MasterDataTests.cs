using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Endpoints;

[Collection("Api")]
public sealed class MasterDataTests(ApiFactory factory)
{
    [Theory]
    [InlineData("YardPlanner")]
    [InlineData("OrderClerk")]
    [InlineData("GateClerk")]
    public async Task NonAdminRolesReadSharedCatalogueButCannotWrite(string role)
    {
        using var admin = await ApiTestClient.LoginAsync(factory);
        var userName = "role" + Guid.NewGuid().ToString("N")[..10];
        var registered = await admin.PostAsJsonAsync("/api/auth/register", new
        {
            UserName = userName, Email = userName + "@example.test", FirstName = "Role", LastName = "Test",
            Password = ApiFactory.AdminPassword, Role = role
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        using var client = await ApiTestClient.LoginAsync(factory, userName);
        foreach (var resource in new[] { "customers", "line-operators", "voyages", "vehicles" })
        {
            var read = await client.GetAsync($"/api/{resource}", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
            var write = await client.PostAsJsonAsync($"/api/{resource}", new { }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
            Assert.Equal("application/problem+json", write.Content.Headers.ContentType?.MediaType);
        }
    }

    [Theory]
    [InlineData("line-operators")]
    [InlineData("customers")]
    [InlineData("voyages")]
    [InlineData("vehicles")]
    public async Task AdminCanCreateAndPageCatalogueAndDuplicateIs409(string resource)
    {
        using var client = await ApiTestClient.LoginAsync(factory);
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        object body = resource switch
        {
            "line-operators" => new { Code = suffix, Name = "Test operator" },
            "customers" => new { TaxCode = Random.Shared.Next(1000000000, int.MaxValue).ToString(), Name = "Test customer" },
            "voyages" => new { VesselName = "Test Vessel", VoyageNumber = suffix, Etd = DateTime.UtcNow.AddDays(10) },
            _ => new { PlateNumber = "51C" + suffix, Type = 0 }
        };
        var created = await client.PostAsJsonAsync($"/api/{resource}", body, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var duplicate = await client.PostAsJsonAsync($"/api/{resource}", body, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var response = await client.GetAsync($"/api/{resource}?pageSize=1", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(1, page.GetProperty("pageSize").GetInt32());
        Assert.Single(page.GetProperty("items").EnumerateArray());
    }

    [Theory]
    [InlineData("pageIndex=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("pageSize=oops")]
    [InlineData("pageIndex=2147483647&pageSize=100")]
    public async Task InvalidPaginationReturns400(string query)
    {
        using var client = await ApiTestClient.LoginAsync(factory);
        var response = await client.GetAsync($"/api/customers?{query}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
