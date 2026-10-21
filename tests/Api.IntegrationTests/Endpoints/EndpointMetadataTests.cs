using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Api.Contracts;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Endpoints;

[Collection("Api")]
public sealed class EndpointMetadataTests(ApiFactory factory)
{
    [Fact]
    public async Task ProductionRoutesHaveUniqueNamesTagsAndExplicitPoliciesWithAuthenticatedFallback()
    {
        using var client = factory.Client();
        var provider = factory.Services.GetRequiredService<IAuthorizationPolicyProvider>();
        var fallback = await provider.GetFallbackPolicyAsync();
        Assert.NotNull(fallback);
        Assert.Contains(fallback.Requirements, r => r is DenyAnonymousAuthorizationRequirement);
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToArray();
        Assert.NotEmpty(endpoints);
        var names = new HashSet<string>(StringComparer.Ordinal);
        var anonymous = new List<string>();
        var inventory = new List<object>();
        foreach (var endpoint in endpoints.OrderBy(e => e.RoutePattern.RawText, StringComparer.Ordinal)
            .ThenBy(e => e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.FirstOrDefault(), StringComparer.Ordinal))
        {
            var name = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName;
            Assert.False(string.IsNullOrWhiteSpace(name));
            Assert.True(names.Add(name!), $"Duplicate endpoint name: {name}");
            var tags = endpoint.Metadata.GetMetadata<ITagsMetadata>()?.Tags;
            Assert.NotNull(tags); Assert.NotEmpty(tags);
            var policies = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Select(p => p.Policy)
                .Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().Order(StringComparer.Ordinal).ToArray();
            var allowsAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
            if (allowsAnonymous) anonymous.Add(name!);
            else
            {
                Assert.NotEmpty(policies);
                foreach (var policy in policies) Assert.NotNull(await provider.GetPolicyAsync(policy!));
            }
            Assert.DoesNotContain("openapi", endpoint.RoutePattern.RawText!, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("scalar", endpoint.RoutePattern.RawText!, StringComparison.OrdinalIgnoreCase);
            inventory.Add(new { path = endpoint.RoutePattern.RawText, methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods,
                name, tags, policies, anonymous = allowsAnonymous,
                deprecated = endpoint.Metadata.GetMetadata<DeprecatedEndpointMetadata>() is not null });
        }
        Assert.Equal(new[] { "Liveness", "Login", "RefreshToken" }, anonymous.Order(StringComparer.Ordinal));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "SnpDepotManagement.sln"))) root = root.Parent;
        Assert.NotNull(root);
        await File.WriteAllTextAsync(Path.Combine(root.FullName, "docs/week-04/endpoint-inventory.json"),
            JsonSerializer.Serialize(inventory, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine,
            TestContext.Current.CancellationToken);
    }
}
