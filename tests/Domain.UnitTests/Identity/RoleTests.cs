using FluentAssertions;
using SnpDepotManagement.Domain.Entities.Identity;
using Xunit;

namespace SnpDepotManagement.Domain.UnitTests.Identity;

public sealed class RoleTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var result = Role.Create("Admin", "Administrator role");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Admin");
        result.Value.Description.Should().Be("Administrator role");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ShouldFail(string name)
    {
        var result = Role.Create(name);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Role.EmptyName");
    }

    [Fact]
    public void ApplicationUser_AddRole_ShouldAddUserRole()
    {
        var user = ApplicationUser.Create("admin", "admin@snp.com", "Admin", "User", "Admin").Value!;
        var roleId = Guid.NewGuid();

        user.AddRole(roleId);

        user.UserRoles.Should().HaveCount(1);
        user.UserRoles.First().RoleId.Should().Be(roleId);
        user.UserRoles.First().UserId.Should().Be(user.Id);

        // Adding same role again should not duplicate
        user.AddRole(roleId);
        user.UserRoles.Should().HaveCount(1);
    }
}
