using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Operations;

[Collection("Api")]
public sealed class BookingLineOperatorTests(ApiFactory factory)
{
    [Fact]
    public async Task GateOut_ContainerEGH_WithOrderEGL_Returns422_LineOperatorMismatch_UntilMapped()
    {
        var ct = TestContext.Current.CancellationToken;
        using var admin = await ApiTestClient.LoginAsync(factory);

        // 1. Create two line operators: EGL (Evergreen) and EGH (Evergreen Marine)
        var eglRes = await admin.PostAsJsonAsync("/api/line-operators", new { Code = "EGL", Name = "Evergreen Line" }, ct);
        Assert.True(eglRes.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict);

        var eghRes = await admin.PostAsJsonAsync("/api/line-operators", new { Code = "EGH", Name = "Evergreen Marine" }, ct);
        Assert.True(eghRes.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict);

        // Retrieve EGL ID
        var listRes = await admin.GetFromJsonAsync<JsonElement>("/api/line-operators?pageSize=50", ct);
        var eglItem = listRes.GetProperty("items").EnumerateArray().First(x => x.GetProperty("code").GetString() == "EGL");
        var eglId = eglItem.GetProperty("id").GetGuid();

        // 2. Ensure Customer exists
        await using (var db = factory.Context("DEPOT-A"))
        {
            if (!db.Customers.Any(c => c.TaxCode == "0101234567"))
                db.Customers.Add(Domain.Entities.MasterData.Customer.Create("0101234567", "Logistics Customer").Value!);
            await db.SaveChangesAsync(ct);
        }

        // 3. Create yard block, container with EGH prefix, and GateIn
        var block = YardBlock.CreateStandard("B" + Guid.NewGuid().ToString("N")[..6], "EGL Block", 3, 1, 2, "DEPOT-A");
        var container = OperationScenario.NewContainer("EGH", "DEPOT-A");
        var order = DeliveryOrder.Create("DO-" + Guid.NewGuid().ToString("N")[..8], "EGL", "0101234567", "Customer", DateTime.UtcNow.AddDays(7), "EVER ACE", "DEPOT-A").Value!;
        order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);

        await using (var db = factory.Context("DEPOT-A"))
        {
            db.YardBlocks.Add(block);
            db.Containers.Add(container);
            db.DeliveryOrders.Add(order);

            var slot = block.Slots.First(s => s.Bay == 1 && s.Tier == 1);
            slot.Occupy(container.Id);
            var lifecycle = ContainerLifecycle.GateIn(container.Id, "EGH", Classification.A, ContainerCondition.Normal, "TRUCK-01", slot.Id, DateTime.UtcNow, "DEPOT-A");
            db.ContainerLifecycles.Add(lifecycle);

            // Ensure no leftover mapping for EGH
            var existingMap = db.LineOperatorOwnerCodes.Where(x => x.OwnerCode == "EGH");
            db.LineOperatorOwnerCodes.RemoveRange(existingMap);

            await db.SaveChangesAsync(ct);
        }

        // 4. Gate-Out before mapping: should FAIL with 422 LineOperatorMismatch
        var unmappedGateOut = await admin.PostAsJsonAsync("/api/gate/out", new
        {
            ContainerId = container.Id,
            DeliveryOrderId = order.Id,
            OutVehicleNumber = "TRUCK-OUT"
        }, ct);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, unmappedGateOut.StatusCode);
        var errJson = await unmappedGateOut.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal("OrderRule.LineOperatorMismatch", errJson.GetProperty("code").GetString());

        // 5. Map EGH to EGL via API
        var mapRes = await admin.PostAsJsonAsync($"/api/line-operators/{eglId}/owner-codes", new { OwnerCode = "EGH" }, ct);
        Assert.Equal(HttpStatusCode.NoContent, mapRes.StatusCode);

        // Verify mapped list
        var codesRes = await admin.GetFromJsonAsync<List<string>>($"/api/line-operators/{eglId}/owner-codes", ct);
        Assert.NotNull(codesRes);
        Assert.Contains("EGH", codesRes!);

        // 6. Gate-Out after mapping: should SUCCEED with 204 NoContent
        var mappedGateOut = await admin.PostAsJsonAsync("/api/gate/out", new
        {
            ContainerId = container.Id,
            DeliveryOrderId = order.Id,
            OutVehicleNumber = "TRUCK-OUT"
        }, ct);

        Assert.Equal(HttpStatusCode.NoContent, mappedGateOut.StatusCode);

        // 7. Remove mapping via DELETE
        var deleteRes = await admin.DeleteAsync($"/api/line-operators/{eglId}/owner-codes/EGH", ct);
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);
    }
}
