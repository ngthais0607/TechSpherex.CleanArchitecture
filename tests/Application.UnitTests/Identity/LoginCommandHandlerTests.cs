using NSubstitute;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Common;
using SnpDepotManagement.Application.Features.Identity.Commands.Login;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Identity;

namespace SnpDepotManagement.Application.UnitTests.Identity;

public class LoginCommandHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _tokenService = Substitute.For<ITokenService>();
        _timeProvider = TimeProvider.System;

        _handler = new LoginCommandHandler(_context, _passwordHasher, _tokenService, _timeProvider);
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreValid_ShouldReturnAuthResponseDto()
    {
        // Arrange
        var user = ApplicationUser.Create("operator01", "op@snp.com.vn", "Nguyen", "Van A", "DEPOT-A", Roles.YardPlanner, "hashed_pw").Value!;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _passwordHasher.VerifyPassword("PlainPassword", "hashed_pw").Returns(true);
        _tokenService.GenerateAccessToken(user).Returns("access_token_xyz");
        _tokenService.GenerateRefreshToken().Returns("raw_refresh_token_123");
        _passwordHasher.HashPassword("raw_refresh_token_123").Returns("hashed_refresh_token");

        var command = new LoginCommand("operator01", "PlainPassword");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("access_token_xyz");
        result.Value.RefreshToken.Should().Be("raw_refresh_token_123");
        result.Value.UserName.Should().Be("operator01");
        result.Value.Role.Should().Be(Roles.YardPlanner);

        // Verify user refreshToken updated in DB
        var userInDb = await _context.Users.FindAsync(user.Id);
        userInDb!.RefreshTokenHash.Should().Be("hashed_refresh_token");
        userInDb.RefreshTokenExpiryTime.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        var command = new LoginCommand("nonexistent", "SomePassword");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.NotFound");
    }

    [Fact]
    public async Task Handle_WhenPasswordIsIncorrect_ShouldReturnValidationError()
    {
        // Arrange
        var user = ApplicationUser.Create("operator02", "op2@snp.com.vn", "Tran", "Van B", "DEPOT-A", Roles.GateClerk, "hashed_pw").Value!;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _passwordHasher.VerifyPassword("WrongPassword", "hashed_pw").Returns(false);

        var command = new LoginCommand("operator02", "WrongPassword");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.InvalidCredentials");
    }
}
