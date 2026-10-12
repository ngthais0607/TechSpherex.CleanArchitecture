using SnpDepotManagement.Application.Features.MasterData.Voyages.Commands.CreateVoyage;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.MasterData;

namespace SnpDepotManagement.Application.UnitTests.MasterData;

public class CreateVoyageCommandHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly CreateVoyageCommandHandler _handler;

    public CreateVoyageCommandHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new CreateVoyageCommandHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenVoyageIsValid_ShouldCreateAndReturnId()
    {
        // Arrange
        var etd = DateTime.UtcNow.AddDays(5);
        var command = new CreateVoyageCommand("MSC EMMA", "2401E", etd);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var entity = await _context.Voyages.FindAsync(result.Value);
        entity.Should().NotBeNull();
        entity!.VesselName.Should().Be("MSC EMMA");
        entity.VoyageNumber.Should().Be("2401E");
        entity.Etd.Should().Be(etd);
    }

    [Fact]
    public async Task Handle_WhenVoyageAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var existing = Voyage.Create("MSC EMMA", "2401E", DateTime.UtcNow.AddDays(2)).Value!;
        _context.Voyages.Add(existing);
        await _context.SaveChangesAsync();

        var command = new CreateVoyageCommand("msc emma", "2401e", DateTime.UtcNow.AddDays(5));

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Voyage.AlreadyExists");
    }
}
