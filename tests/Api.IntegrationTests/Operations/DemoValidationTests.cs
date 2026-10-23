using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Operations;

[Collection("Api")]
public sealed class DemoValidationTests(ApiFactory factory)
{
    [Theory]
    [InlineData("driverName", 101, "DriverName")]
    [InlineData("vehicleNumber", 21, "VehicleNumber")]
    [InlineData("note", 201, "Note")]
    public async Task UpdateGate_OverlongField_ReturnsValidationWithoutChangingTransaction(string field, int length, string errorField)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);
        var transaction = await CreateTransactionAsync();
        var response = await client.PutAsJsonAsync($"/api/gate/transactions/{transaction.Id}",
            new Dictionary<string, string> { [field] = new('x', length) }, ct);
        await AssertValidationAsync(response, errorField);
        await using var db = factory.Context("DEPOT-A");
        var stored = await db.GateTransactions.Include(t => t.Changes).SingleAsync(t => t.Id == transaction.Id, ct);
        Assert.Equal("TRUCK", stored.VehicleNumber);
        Assert.Equal("Demo driver", stored.DriverName);
        Assert.Empty(stored.Changes);
    }

    [Fact]
    public async Task GateIn_OverlongDriver_ReturnsValidationWithoutCreatingLifecycle()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);
        var scenario = await OperationScenario.CreateAsync(factory);
        var response = await client.PostAsJsonAsync("/api/gate/in", new
        {
            containerId = scenario.Third.Id, vehicleNumber = "TRUCK", driverName = new string('x', 101),
            blockCode = scenario.Block.Code, bay = 3, row = 1, tier = 1
        }, ct);
        await AssertValidationAsync(response, "DriverName");
        await using var db = factory.Context("DEPOT-A");
        Assert.False(await db.ContainerLifecycles.AnyAsync(l => l.ContainerId == scenario.Third.Id, ct));
    }

    [Theory]
    [InlineData("{}", "TargetBlockCode")]
    [InlineData("{\"targetBlockCode\":\"A\",\"bay\":0,\"row\":1,\"tier\":1}", "Bay")]
    [InlineData("{\"targetBlockCode\":\"A\",\"bay\":1,\"row\":-1,\"tier\":1}", "Row")]
    [InlineData("{\"targetBlockCode\":\"A\",\"bay\":1,\"row\":1,\"tier\":0}", "Tier")]
    public async Task PlaceFromVirtual_InvalidInput_ReturnsValidation(string json, string field)
    {
        using var client = await ApiTestClient.LoginAsync(factory);
        var ct = TestContext.Current.CancellationToken;
        var scenario = await OperationScenario.CreateAsync(factory);
        var virtualBlock = YardBlock.CreateVirtual("V" + Guid.NewGuid().ToString("N")[..8], "Virtual demo test", "DEPOT-A");
        var lifecycle = ContainerLifecycle.GateIn(scenario.Third.Id, "MSK", Classification.A,
            ContainerCondition.Normal, "TRUCK", null, DateTime.UtcNow, "DEPOT-A");
        lifecycle.SetVirtualBlock(virtualBlock.Id);
        await using (var db = factory.Context("DEPOT-A"))
        {
            db.YardBlocks.Add(virtualBlock);
            db.ContainerLifecycles.Add(lifecycle);
            await db.SaveChangesAsync(ct);
        }
        json = json.Replace("\"A\"", $"\"{scenario.Block.Code}\"");
        var response = await client.PostAsync($"/api/yard/virtual-blocks/{virtualBlock.Code}/containers/{scenario.Third.Id}/place",
            new StringContent(json, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        await AssertValidationAsync(response, field);
        await using var verifyDb = factory.Context("DEPOT-A");
        var stored = await verifyDb.ContainerLifecycles.SingleAsync(l => l.Id == lifecycle.Id, ct);
        Assert.Equal(virtualBlock.Id, stored.VirtualBlockId);
        Assert.Null(stored.CurrentSlotId);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    public async Task AssignContainers_NullOrEmpty_ReturnsValidation(string numbersJson)
    {
        using var client = await ApiTestClient.LoginAsync(factory);
        var response = await client.PostAsync("/api/delivery-orders/DEMO/assign-containers",
            new StringContent($"{{\"containerNumbers\":{numbersJson}}}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);
        await AssertValidationAsync(response, "ContainerNumbers");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task AssignContainers_EmptyEntry_DoesNotPartiallyAssign(string? emptyEntry)
    {
        using var client = await ApiTestClient.LoginAsync(factory);
        var scenario = await OperationScenario.CreateAsync(factory);
        var response = await client.PostAsJsonAsync($"/api/delivery-orders/{scenario.Order.OrderNumber}/assign-containers",
            new { containerNumbers = new[] { scenario.First.ContainerNumber.Value, emptyEntry } }, TestContext.Current.CancellationToken);
        await AssertValidationAsync(response, "ContainerNumbers[1]");
        await using var db = factory.Context("DEPOT-A");
        Assert.False(await db.Bookings.AnyAsync(b => b.OrderId == scenario.Order.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateGate_FieldsAtSchemaLimits_AreAccepted()
    {
        using var client = await ApiTestClient.LoginAsync(factory);
        var transaction = await CreateTransactionAsync();
        var response = await client.PutAsJsonAsync($"/api/gate/transactions/{transaction.Id}",
            new { vehicleNumber = new string('V', 20), driverName = new string('D', 100), note = new string('N', 200) },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var db = factory.Context("DEPOT-A");
        var stored = await db.GateTransactions.Include(t => t.Changes).SingleAsync(t => t.Id == transaction.Id, TestContext.Current.CancellationToken);
        Assert.Equal(new string('D', 100), stored.DriverName);
        Assert.Equal(new string('V', 20), stored.VehicleNumber);
        Assert.Contains(stored.Changes, c => c.FieldName == "Note" && c.NewValue == new string('N', 200));
    }

    [Theory]
    [InlineData(201)]
    [InlineData(10000)]
    public async Task AssignContainers_TooManyEntries_ReturnsValidation(int count)
    {
        using var client = await ApiTestClient.LoginAsync(factory);
        var scenario = await OperationScenario.CreateAsync(factory);
        var response = await client.PostAsJsonAsync($"/api/delivery-orders/{scenario.Order.OrderNumber}/assign-containers",
            new { containerNumbers = Enumerable.Repeat(scenario.First.ContainerNumber.Value, count).ToArray() },
            TestContext.Current.CancellationToken);
        await AssertValidationAsync(response, "ContainerNumbers");
        await using var db = factory.Context("DEPOT-A");
        Assert.False(await db.Bookings.AnyAsync(b => b.OrderId == scenario.Order.Id, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("DELETE")]
    public async Task CancelGate_OverlongReason_ReturnsValidationWithoutCancellation(string method)
    {
        using var client = await ApiTestClient.LoginAsync(factory);
        var transaction = await CreateTransactionAsync();
        using var request = new HttpRequestMessage(new HttpMethod(method), $"/api/gate/transactions/{transaction.Id}/cancel")
        { Content = JsonContent.Create(new { reason = new string('x', 201) }) };
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        await AssertValidationAsync(response, "Reason");
        await using var db = factory.Context("DEPOT-A");
        Assert.Equal(GateTransactionStatus.Active,
            (await db.GateTransactions.SingleAsync(t => t.Id == transaction.Id, TestContext.Current.CancellationToken)).Status);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("DELETE")]
    public async Task CancelGate_MalformedJson_DoesNotCancel(string method)
    {
        using var client = await ApiTestClient.LoginAsync(factory);
        var transaction = await CreateTransactionAsync();
        using var request = new HttpRequestMessage(new HttpMethod(method), $"/api/gate/transactions/{transaction.Id}/cancel")
        { Content = new StringContent("{invalid", Encoding.UTF8, "application/json") };
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var db = factory.Context("DEPOT-A");
        Assert.Equal(GateTransactionStatus.Active,
            (await db.GateTransactions.SingleAsync(t => t.Id == transaction.Id, TestContext.Current.CancellationToken)).Status);
    }

    private async Task<GateTransaction> CreateTransactionAsync()
    {
        var scenario = await OperationScenario.CreateAsync(factory);
        var slot = scenario.Block.Slots.First(s => s.Bay == 1 && s.Tier == 1);
        var transaction = GateTransaction.CreateGateIn(scenario.First.Id, "TRUCK", VehicleType.Truck,
            "Demo driver", slot.Id, null, "DEPOT-A");
        await using var db = factory.Context("DEPOT-A");
        db.GateTransactions.Add(transaction);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return transaction;
    }

    private static async Task AssertValidationAsync(HttpResponseMessage response, string field)
    {
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {response.StatusCode}: {body}");
        var problem = JsonDocument.Parse(body).RootElement;
        Assert.Equal("Validation.Failed", problem.GetProperty("code").GetString());
        Assert.True(problem.GetProperty("errors").TryGetProperty(field, out _), body);
    }
}
