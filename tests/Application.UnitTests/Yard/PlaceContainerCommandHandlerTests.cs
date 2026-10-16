using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Common.Rules;
using SnpDepotManagement.Application.Features.Yard.Commands.PlaceContainer;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.UnitTests.Yard;

public class PlaceContainerCommandHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly PlaceContainerCommandHandler _handler;

    public PlaceContainerCommandHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new PlaceContainerCommandHandler(_context, new RuleEngine());
    }

    private static Container CreateValidContainer(string ownerPrefix, string serialNumber, ContainerSize size)
    {
        string raw = $"{ownerPrefix}U{serialNumber}";
        int sum = 0;
        for (int i = 0; i < 10; i++)
        {
            char c = raw[i];
            int val = char.IsLetter(c) ? (c - 'A' + 10) : (c - '0');
            if (char.IsLetter(c))
            {
                if (val >= 11) val++;
                if (val >= 22) val++;
                if (val >= 33) val++;
            }
            sum += val * (1 << i);
        }
        int rem = sum % 11;
        int checkDigit = rem == 10 ? 0 : rem;

        var number = ContainerNumber.Create($"{raw}{checkDigit}").Value!;
        return Container.Create(number, "22G1", size, ContainerType.Dry, 2200, 30480, null, ownerPrefix).Value!;
    }

    [Fact]
    public async Task Handle_WhenPlacing20FtAtGroundTier_ShouldSucceed()
    {
        // Arrange
        var block = YardBlock.CreateStandard("BLK-A", "Block A", 5, 3, 4);
        var container = CreateValidContainer("CSQ", "305438", ContainerSize.Ft20);
        _context.YardBlocks.Add(block);
        _context.Containers.Add(container);
        YardLifecycleFixture.TrackActiveLifecycles(_context);
        await _context.SaveChangesAsync();

        var command = new PlaceContainerCommand(block.Id, 1, 1, 1, container.Id);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedBlock = await _context.YardBlocks.Include(b => b.Slots).FirstAsync(b => b.Id == block.Id);
        var slot = updatedBlock.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1);
        slot.IsOccupied.Should().BeTrue();
        slot.ContainerId.Should().Be(container.Id);
    }

    [Fact]
    public async Task Handle_WhenPlacing40FtAtEvenBay_ShouldOccupyBothSlots()
    {
        // Arrange (Bay 2 occupies slots at Bay 1 and Bay 3)
        var block = YardBlock.CreateStandard("BLK-B", "Block B", 5, 3, 4);
        var container = CreateValidContainer("MSK", "100001", ContainerSize.Ft40);
        _context.YardBlocks.Add(block);
        _context.Containers.Add(container);
        YardLifecycleFixture.TrackActiveLifecycles(_context);
        await _context.SaveChangesAsync();

        var command = new PlaceContainerCommand(block.Id, 2, 1, 1, container.Id);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedBlock = await _context.YardBlocks.Include(b => b.Slots).FirstAsync(b => b.Id == block.Id);
        var slot1 = updatedBlock.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1);
        var slot2 = updatedBlock.Slots.First(s => s.Bay == 3 && s.Row == 1 && s.Tier == 1);

        slot1.IsOccupied.Should().BeTrue();
        slot1.ContainerId.Should().Be(container.Id);
        slot2.IsOccupied.Should().BeTrue();
        slot2.ContainerId.Should().Be(container.Id);
    }

    [Fact]
    public async Task Handle_WhenPlacingAtTier2WithoutSupportBelow_ShouldFailWithRuleError()
    {
        // Arrange
        var block = YardBlock.CreateStandard("BLK-C", "Block C", 5, 3, 4);
        var container = CreateValidContainer("CSQ", "305438", ContainerSize.Ft20);
        _context.YardBlocks.Add(block);
        _context.Containers.Add(container);
        YardLifecycleFixture.TrackActiveLifecycles(_context);
        await _context.SaveChangesAsync();

        var command = new PlaceContainerCommand(block.Id, 1, 1, 2, container.Id);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("YardRule.SlotBelowNotOccupied");
    }

    [Fact]
    public async Task Handle_WhenSlotIsAlreadyOccupied_ShouldFailWithConflict()
    {
        // Arrange
        var block = YardBlock.CreateStandard("BLK-D", "Block D", 5, 3, 4);
        var c1 = CreateValidContainer("CSQ", "305438", ContainerSize.Ft20);
        var c2 = CreateValidContainer("ONE", "100002", ContainerSize.Ft20);
        block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1).Occupy(c1.Id);

        _context.YardBlocks.Add(block);
        _context.Containers.AddRange(c1, c2);
        YardLifecycleFixture.TrackActiveLifecycles(_context);
        await _context.SaveChangesAsync();

        var command = new PlaceContainerCommand(block.Id, 1, 1, 1, c2.Id);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("YardRule.SlotAlreadyOccupied");
    }

    [Fact]
    public async Task Handle_WhenBayIsInvalidForSize_ShouldFailWithInvalidBayError()
    {
        // Arrange: 20ft container placed at even bay 2
        var block = YardBlock.CreateStandard("BLK-E", "Block E", 5, 3, 4);
        var container = CreateValidContainer("CSQ", "305438", ContainerSize.Ft20);
        _context.YardBlocks.Add(block);
        _context.Containers.Add(container);
        YardLifecycleFixture.TrackActiveLifecycles(_context);
        await _context.SaveChangesAsync();

        var command = new PlaceContainerCommand(block.Id, 2, 1, 1, container.Id);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("YardRule.InvalidBayForSize");
    }

    [Fact]
    public async Task Handle_WhenContainerAlreadyPlacedInYard_ShouldFailWithConflict()
    {
        // Arrange
        var block = YardBlock.CreateStandard("BLK-F", "Block F", 5, 3, 4);
        var container = CreateValidContainer("CSQ", "305438", ContainerSize.Ft20);
        block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1).Occupy(container.Id);

        _context.YardBlocks.Add(block);
        _context.Containers.Add(container);
        YardLifecycleFixture.TrackActiveLifecycles(_context);
        await _context.SaveChangesAsync();

        // Try to place the same container into another slot (Bay 3)
        var command = new PlaceContainerCommand(block.Id, 3, 1, 1, container.Id);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("YardSlot.ContainerAlreadyPlaced");
    }
}
