using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.MasterData;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.AcceptanceTests;

/// <summary>
/// Kịch bản 10: Lệnh qua hạn → Booking Reserved của lệnh hết hạn được chuyển Expired và container gán được lệnh khác.
/// </summary>
[Collection("Api")]
public sealed class Scenario10_ExpiredBookingReassignmentTests(ApiFactory factory)
{
    [Fact]
    public async Task Scenario10_AssignContainer_FromExpiredOrder_TransitionsOldBookingToExpired_AndSucceeds()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ApiTestClient.LoginAsync(factory);

        var tenantId = "DEPOT-A";
        var container = OperationScenario.NewContainer("MSC", tenantId);
        var oldExpiredOrderNumber = "DO-EXP-" + Guid.NewGuid().ToString("N")[..6];
        var newOrderNumber = "DO-NEW-" + Guid.NewGuid().ToString("N")[..6];
        var taxCode = "010" + Random.Shared.Next(1000000, 9999999);
        Guid oldBookingId;

        await using (var db = factory.Context(tenantId))
        {
            if (!db.LineOperators.Any(l => l.Code == "MSC"))
                db.LineOperators.Add(LineOperator.Create("MSC", "MSC Line").Value!);

            if (!db.Customers.Any(c => c.TaxCode == taxCode))
                db.Customers.Add(Customer.Create(taxCode, "Customer 10").Value!);

            // Lệnh cũ đã hết hạn (ExpiryDate 5 ngày trước)
            var oldOrder = DeliveryOrder.Create(oldExpiredOrderNumber, "MSC", taxCode, "Customer 10", DateTime.UtcNow.AddDays(-5), "OLD VESSEL", tenantId).Value!;
            oldOrder.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);

            // Lệnh mới còn hạn (ExpiryDate 10 ngày sau)
            var newOrder = DeliveryOrder.Create(newOrderNumber, "MSC", taxCode, "Customer 10", DateTime.UtcNow.AddDays(10), "NEW VESSEL", tenantId).Value!;
            newOrder.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);

            var lifecycle = ContainerLifecycle.GateIn(container.Id, container.OwnerCode, Classification.A, ContainerCondition.Normal, "29C-11111", null, DateTime.UtcNow, tenantId);
            db.Containers.Add(container);
            db.ContainerLifecycles.Add(lifecycle);
            db.DeliveryOrders.AddRange(oldOrder, newOrder);
            await db.SaveChangesAsync(ct);

            // Booking Reserved của lệnh cũ
            var oldBooking = DeliveryOrderBooking.Reserve(oldOrder.Id, oldOrder.Items.First().Id, container.Id, tenantId, DateTime.UtcNow.AddDays(-10));
            db.Bookings.Add(oldBooking);
            await db.SaveChangesAsync(ct);
            oldBookingId = oldBooking.Id;
        }

        // Act: Gán container vào lệnh mới còn hạn
        var payload = new
        {
            ContainerNumbers = new[] { container.ContainerNumber.Value }
        };

        var response = await client.PostAsJsonAsync($"/api/delivery-orders/{newOrderNumber}/assign", payload, ct);
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent, $"Assign failed: {await response.Content.ReadAsStringAsync(ct)}");

        // Assert: Booking cũ đã chuyển sang Expired, booking mới là Reserved
        await using (var db = factory.Context(tenantId))
        {
            var oldB = await db.Bookings.FindAsync(new object[] { oldBookingId }, ct);
            Assert.NotNull(oldB);
            Assert.Equal(BookingStatus.Expired, oldB.Status);

            var newB = await db.Bookings.FirstOrDefaultAsync(b => b.ContainerId == container.Id && b.Status == BookingStatus.Reserved, ct);
            Assert.NotNull(newB);
        }
    }
}
