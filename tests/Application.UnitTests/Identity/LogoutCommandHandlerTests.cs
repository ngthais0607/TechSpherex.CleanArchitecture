using FluentAssertions;
using NSubstitute;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Common;
using SnpDepotManagement.Application.Features.Identity.Commands.Logout;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Identity;
using Xunit;

namespace SnpDepotManagement.Application.UnitTests.Identity;

public sealed class LogoutCommandHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _currentUser = Substitute.For<ICurrentUser>();
        _handler = new LogoutCommandHandler(_context, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldRevokeRefreshToken()
    {
        // Arrange
        var user = ApplicationUser.Create("operator01", "op@snp.com.vn", "Nguyen", "Van A", "DEPOT-A", Roles.YardPlanner, "hash").Value!;
        user.SetRefreshToken("refresh_token_hash", DateTime.UtcNow.AddDays(7));
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _currentUser.UserId.Returns(user.Id);

        var command = new LogoutCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var userInDb = await _context.Users.FindAsync(user.Id);
        userInDb!.RefreshTokenHash.Should().BeNull();
        userInDb.RefreshTokenExpiryTime.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _currentUser.UserId.Returns(Guid.NewGuid());

        var command = new LogoutCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.NotFound");
    }
}
