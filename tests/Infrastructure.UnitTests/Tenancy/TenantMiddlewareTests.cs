using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SnpDepotManagement.Infrastructure.Tenancy;
using Xunit;

namespace SnpDepotManagement.Infrastructure.UnitTests.Tenancy;

public sealed class TenantMiddlewareTests
{
    [Fact]
    public async Task DifferentHeaderCannotSelectAnotherDepot()
    {
        var context = Context(authenticated: true);
        context.Request.Headers["X-Tenant-Id"] = "DEPOT-B";
        var reachedEndpoint = false;
        await Middleware(context).InvokeAsync(context, _ => { reachedEndpoint = true; return Task.CompletedTask; });
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.False(reachedEndpoint);
    }

    [Fact]
    public async Task AnonymousEndpointDoesNotRequireTenant()
    {
        var context = Context(authenticated: false);
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
            new EndpointMetadataCollection(new AllowAnonymousAttribute()), "Login"));
        var reachedEndpoint = false;
        await Middleware(context).InvokeAsync(context, _ => { reachedEndpoint = true; return Task.CompletedTask; });
        Assert.True(reachedEndpoint);
    }

    [Fact]
    public async Task HeaderAloneDoesNotGrantAuthenticatedTenant()
    {
        var context = Context(authenticated: false);
        context.Request.Headers["X-Tenant-Id"] = "DEPOT-A";
        var reachedEndpoint = false;
        await Middleware(context).InvokeAsync(context, _ => { reachedEndpoint = true; return Task.CompletedTask; });
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(reachedEndpoint);
    }

    [Fact]
    public async Task SignedTenantWithoutHeaderCanReachEndpoint()
    {
        var context = Context(authenticated: true);
        var reachedEndpoint = false;
        await Middleware(context).InvokeAsync(context, _ => { reachedEndpoint = true; return Task.CompletedTask; });
        Assert.True(reachedEndpoint);
    }

    private static DefaultHttpContext Context(bool authenticated)
    {
        var context = new DefaultHttpContext();
        context.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/containers";
        if (authenticated)
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("tenant_id", "DEPOT-A")], "test"));
        return context;
    }

    private static TenantMiddleware Middleware(HttpContext context) =>
        new(new TenantProvider(new HttpContextAccessor { HttpContext = context }));
}
