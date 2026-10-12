using SnpDepotManagement.Application.Features.Operations.Lifecycles.Queries.GetContainerLifecycleHistory;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.UnitTests.Operations;

public class GetContainerLifecycleHistoryQueryHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly GetContainerLifecycleHistoryQueryHandler _handler;

    public GetContainerLifecycleHistoryQueryHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new GetContainerLifecycleHistoryQueryHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenContainerHasLifecycles_ShouldReturnOrderedHistory()
    {
        // Arrange
        var containerId = Guid.NewGuid();
        var firstGateIn = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc);
        var l1 = ContainerLifecycle.GateIn(containerId, "MAEU", Classification.A, ContainerCondition.Normal, "51C-11111", Guid.NewGuid(), firstGateIn);
        l1.GateOut("51C-22222", Guid.NewGuid(), firstGateIn.AddHours(1));

        var l2 = ContainerLifecycle.GateIn(containerId, "MAEU", Classification.B, ContainerCondition.Normal, "51C-33333", Guid.NewGuid(), firstGateIn.AddDays(1));

        _context.ContainerLifecycles.AddRange(l1, l2);
        await _context.SaveChangesAsync();

        var query = new GetContainerLifecycleHistoryQuery(containerId);

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.First().InVehicleNumber.Should().Be("51C-33333"); // most recent first
    }
}
