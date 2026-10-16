using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Features.Yard.Commands.RemoveContainer;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.UnitTests.Yard;

public class RemoveContainerCommandHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly RemoveContainerCommandHandler _handler;

    public RemoveContainerCommandHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new RemoveContainerCommandHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenRemoving20FtContainer_ShouldReleaseSlot()
    {
        // Arrange
        var block = YardBlock.CreateStandard("BLK-REM", "Block", 5, 3, 4);
        var containerId = SeedActiveContainer(block, ContainerSize.Ft20);
        block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1).Occupy(containerId);

        _context.YardBlocks.Add(block);
        await _context.SaveChangesAsync();

        var command = new RemoveContainerCommand(block.Id, 1, 1, 1);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedBlock = await _context.YardBlocks.Include(b => b.Slots).FirstAsync(b => b.Id == block.Id);
        var slot = updatedBlock.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1);
        slot.IsOccupied.Should().BeFalse();
        slot.ContainerId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenRemoving40FtContainer_ShouldReleaseBothSupportingSlots()
    {
        // Arrange (40ft at Bay 2 occupies slots 1 and 3)
        var block = YardBlock.CreateStandard("BLK-REM40", "Block 40", 5, 3, 4);
        var containerId = SeedActiveContainer(block, ContainerSize.Ft40);
        block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1).Occupy(containerId);
        block.Slots.First(s => s.Bay == 3 && s.Row == 1 && s.Tier == 1).Occupy(containerId);

        _context.YardBlocks.Add(block);
        await _context.SaveChangesAsync();

        var command = new RemoveContainerCommand(block.Id, 2, 1, 1);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedBlock = await _context.YardBlocks.Include(b => b.Slots).FirstAsync(b => b.Id == block.Id);
        var slot1 = updatedBlock.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1);
        var slot2 = updatedBlock.Slots.First(s => s.Bay == 3 && s.Row == 1 && s.Tier == 1);
        slot1.IsOccupied.Should().BeFalse();
        slot2.IsOccupied.Should().BeFalse();
    }

    private Guid SeedActiveContainer(YardBlock block, ContainerSize size)
    {
        var container = Container.Create(ContainerNumber.Create("CSQU3054383").Value!, "22G1", size,
            ContainerType.Dry, 2000, 30000, null, "CSQ", "TEST-DEPOT").Value!;
        _context.Containers.Add(container);
        _context.ContainerLifecycles.Add(ContainerLifecycle.GateIn(container.Id, "CSQ", Classification.A,
            ContainerCondition.Normal, "TEST-TRUCK", block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1).Id,
            DateTime.UtcNow, "TEST-DEPOT"));
        return container.Id;
    }

    [Fact]
    public async Task Handle_WhenSlotIsEmpty_ShouldFailWithNotFound()
    {
        // Arrange
        var block = YardBlock.CreateStandard("BLK-EMPTY", "Empty", 5, 3, 4);
        _context.YardBlocks.Add(block);
        await _context.SaveChangesAsync();

        var command = new RemoveContainerCommand(block.Id, 1, 1, 1);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("YardSlot.SlotNotOccupied");
    }

    [Fact]
    public async Task Handle_WhenAnotherContainerIsStackedAbove_ShouldFailWithConflict()
    {
        // Arrange: Container at Tier 1 and another container at Tier 2
        var block = YardBlock.CreateStandard("BLK-STACK", "Stack", 5, 3, 4);
        block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1).Occupy(Guid.NewGuid());
        block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 2).Occupy(Guid.NewGuid());

        _context.YardBlocks.Add(block);
        await _context.SaveChangesAsync();

        // Attempt to remove tier 1 container
        var command = new RemoveContainerCommand(block.Id, 1, 1, 1);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("YardSlot.CannotRemoveWithContainersAbove");
    }
}
