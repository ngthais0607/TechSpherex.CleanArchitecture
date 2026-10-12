using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using SnpDepotManagement.Infrastructure.Tenancy;
using Xunit;

namespace SnpDepotManagement.Infrastructure.UnitTests.Tenancy;

public sealed class TenantProviderTests
{
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly TenantProvider _sut;

    public TenantProviderTests()
    {
        _sut = new TenantProvider(_httpContextAccessor);
    }

    [Fact]
    public void GetCurrentTenantId_WhenClaimPresent_ShouldReturnClaimValue()
    {
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("tenant_id", "DEPOT-01")
        }, "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        _httpContextAccessor.HttpContext.Returns(context);

        var tenantId = _sut.GetCurrentTenantId();

        tenantId.Should().Be("DEPOT-01");
        _sut.GetCurrentTenant()?.TenantId.Should().Be("DEPOT-01");
    }

    [Fact]
    public void GetCurrentTenantId_WhenHeaderPresentAndNoClaim_ShouldReturnHeaderValue()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = "DEPOT-02";

        _httpContextAccessor.HttpContext.Returns(context);

        var tenantId = _sut.GetCurrentTenantId();

        tenantId.Should().Be("DEPOT-02");
        _sut.GetCurrentTenant()?.TenantId.Should().Be("DEPOT-02");
    }

    [Fact]
    public void GetCurrentTenantId_WhenBothClaimAndHeaderPresent_ShouldPrioritizeClaim()
    {
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("tenant_id", "DEPOT-CLAIM")
        }, "TestAuth");
        context.User = new ClaimsPrincipal(identity);
        context.Request.Headers["X-Tenant-Id"] = "DEPOT-HEADER";

        _httpContextAccessor.HttpContext.Returns(context);

        var tenantId = _sut.GetCurrentTenantId();

        tenantId.Should().Be("DEPOT-CLAIM");
    }

    [Fact]
    public void GetCurrentTenantId_WhenNeitherPresent_ShouldReturnNull()
    {
        var context = new DefaultHttpContext();
        _httpContextAccessor.HttpContext.Returns(context);

        var tenantId = _sut.GetCurrentTenantId();

        tenantId.Should().BeNull();
        _sut.GetCurrentTenant().Should().BeNull();
    }
}
