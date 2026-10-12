using SnpDepotManagement.Application.Features.MasterData.Customers.Commands.CreateCustomer;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.MasterData;

namespace SnpDepotManagement.Application.UnitTests.MasterData;

public class CreateCustomerCommandHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly CreateCustomerCommandHandler _handler;

    public CreateCustomerCommandHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new CreateCustomerCommandHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenCustomerIsValid_ShouldCreateAndReturnId()
    {
        // Arrange
        var command = new CreateCustomerCommand("0102030405", "Cong ty TNHH Logistics Saigon");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var customerInDb = await _context.Customers.FindAsync(result.Value);
        customerInDb.Should().NotBeNull();
        customerInDb!.TaxCode.Should().Be("0102030405");
        customerInDb.Name.Should().Be("Cong ty TNHH Logistics Saigon");
    }

    [Fact]
    public async Task Handle_WhenTaxCodeAlreadyExists_ShouldReturnConflictError()
    {
        // Arrange
        var existing = Customer.Create("0102030405", "Cong ty Cu").Value!;
        _context.Customers.Add(existing);
        await _context.SaveChangesAsync();

        var command = new CreateCustomerCommand("0102030405", "Cong ty Moi");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Customer.AlreadyExists");
    }
}
