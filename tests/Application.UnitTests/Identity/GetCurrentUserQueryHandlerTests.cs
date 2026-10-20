using FluentAssertions;
using NSubstitute;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Common;
using SnpDepotManagement.Application.Features.Identity.Queries.GetCurrentUser;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Identity;
using SnpDepotManagement.Domain.Entities.MasterData;
using Xunit;

namespace SnpDepotManagement.Application.UnitTests.Identity;

public sealed class GetCurrentUserQueryHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly GetCurrentUserQueryHandler _handler;

    public GetCurrentUserQueryHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _currentUser = Substitute.For<ICurrentUser>();
        _handler = new GetCurrentUserQueryHandler(_context, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldReturnCurrentUserDto()
    {
        // Arrange
        var user = ApplicationUser.Create("operator01", "op@snp.com.vn", "Nguyen", "Van A", "DEPOT-A", Roles.YardPlanner, "hash").Value!;
        _context.Users.Add(user);

        var depotId = Guid.NewGuid();
        var depot = Depot.Create(depotId, "SNP Depot Cat Lai", "Asia/Ho_Chi_Minh");
        _context.Depots.Add(depot);
        await _context.SaveChangesAsync();

        _currentUser.UserId.Returns(user.Id);
        _currentUser.TenantId.Returns(depotId.ToString());

        var query = new GetCurrentUserQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UserId.Should().Be(user.Id.ToString());
        result.Value.UserName.Should().Be("operator01");
        result.Value.Roles.Should().ContainSingle().Which.Should().Be(Roles.YardPlanner);
        result.Value.TenantId.Should().Be(depotId.ToString());
        result.Value.DepotName.Should().Be("SNP Depot Cat Lai");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _currentUser.UserId.Returns(Guid.NewGuid());

        var query = new GetCurrentUserQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.NotFound");
    }
}
