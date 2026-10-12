using NSubstitute;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Common;
using SnpDepotManagement.Application.Features.Identity.Commands.RefreshToken;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Identity;

namespace SnpDepotManagement.Application.UnitTests.Identity;

public class RefreshTokenCommandHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _tokenService = Substitute.For<ITokenService>();
        _timeProvider = TimeProvider.System;

        _handler = new RefreshTokenCommandHandler(_context, _passwordHasher, _tokenService, _timeProvider);
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenIsValid_ShouldRotateTokensAndReturnSuccess()
    {
        // Arrange
        var user = ApplicationUser.Create("operator01", "op@snp.com.vn", "Nguyen", "Van A", Roles.YardPlanner, "pw_hash").Value!;
        user.SetRefreshToken("valid_stored_hash", _timeProvider.GetUtcNow().AddDays(3).UtcDateTime);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _passwordHasher.VerifyPassword("my_refresh_token", "valid_stored_hash").Returns(true);
        _tokenService.GenerateAccessToken(user).Returns("new_access_token");
        _tokenService.GenerateRefreshToken().Returns("new_raw_refresh_token");
        _passwordHasher.HashPassword("new_raw_refresh_token").Returns("new_hash");

        var command = new RefreshTokenCommand("operator01", "my_refresh_token");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("new_access_token");
        result.Value.RefreshToken.Should().Be("new_raw_refresh_token");

        // Verify rotation in DB
        var userInDb = await _context.Users.FindAsync(user.Id);
        userInDb!.RefreshTokenHash.Should().Be("new_hash");
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenIsExpired_ShouldReturnValidationError()
    {
        // Arrange
        var user = ApplicationUser.Create("operator02", "op2@snp.com.vn", "Tran", "Van B", Roles.GateClerk, "pw_hash").Value!;
        user.SetRefreshToken("expired_hash", _timeProvider.GetUtcNow().AddDays(-1).UtcDateTime); // Expired yesterday
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new RefreshTokenCommand("operator02", "some_token");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.RefreshTokenExpired");
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenHashDoesNotMatch_ShouldReturnValidationError()
    {
        // Arrange
        var user = ApplicationUser.Create("operator03", "op3@snp.com.vn", "Hoang", "Van C", Roles.OrderClerk, "pw_hash").Value!;
        user.SetRefreshToken("correct_stored_hash", _timeProvider.GetUtcNow().AddDays(2).UtcDateTime);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _passwordHasher.VerifyPassword("wrong_token", "correct_stored_hash").Returns(false);

        var command = new RefreshTokenCommand("operator03", "wrong_token");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidRefreshToken");
    }
}
