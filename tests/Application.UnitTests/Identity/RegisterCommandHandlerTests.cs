using NSubstitute;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Common;
using SnpDepotManagement.Application.Features.Identity.Commands.Register;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Identity;

namespace SnpDepotManagement.Application.UnitTests.Identity;

public class RegisterCommandHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _currentUser = Substitute.For<ICurrentUser>();
        _passwordHasher = Substitute.For<IPasswordHasher>();

        _handler = new RegisterCommandHandler(_context, _currentUser, _passwordHasher);
    }

    [Fact]
    public async Task Handle_WhenExecutedByAdmin_ShouldRegisterUserSuccessfully()
    {
        // Arrange
        _currentUser.Role.Returns(Roles.Admin);
        _passwordHasher.HashPassword("Secr3tPassword").Returns("hashed_secret");

        var command = new RegisterCommand("newuser", "newuser@snp.com.vn", "Le", "Van C", "Secr3tPassword", Roles.OrderClerk);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var createdUser = await _context.Users.FindAsync(result.Value);
        createdUser.Should().NotBeNull();
        createdUser!.UserName.Should().Be("newuser");
        createdUser.Email.Should().Be("newuser@snp.com.vn");
        createdUser.Role.Should().Be(Roles.OrderClerk);
        createdUser.PasswordHash.Should().Be("hashed_secret");
    }

    [Fact]
    public async Task Handle_WhenExecutedByNonAdmin_ShouldReturnForbiddenError()
    {
        // Arrange: User is not Admin
        _currentUser.Role.Returns(Roles.YardPlanner);

        var command = new RegisterCommand("newuser", "newuser@snp.com.vn", "Le", "Van C", "Secr3tPassword", Roles.OrderClerk);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.Forbidden");
    }

    [Fact]
    public async Task Handle_WhenUsernameAlreadyExists_ShouldReturnConflictError()
    {
        // Arrange
        _currentUser.Role.Returns(Roles.Admin);
        var existingUser = ApplicationUser.Create("existinguser", "old@snp.com.vn", "A", "B", Roles.GateClerk, "hash").Value!;
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var command = new RegisterCommand("existinguser", "unique@snp.com.vn", "Le", "Van C", "Secr3tPassword", Roles.OrderClerk);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.AlreadyExists");
    }
}
