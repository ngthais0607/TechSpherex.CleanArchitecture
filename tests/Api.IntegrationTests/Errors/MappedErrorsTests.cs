using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Containers.Dtos;
using SnpDepotManagement.Application.Features.Containers.Queries.GetContainers;
using SnpDepotManagement.Domain.Common;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Errors;

[Collection("Api")]
public sealed class MappedErrorsTests(ApiFactory factory)
{
    [Theory]
    [InlineData("Container.NotFound", 404)]
    [InlineData("Container.AlreadyExists", 409)]
    [InlineData("OrderRule.DeliveryOrderExpired", 422)]
    [InlineData("Unknown.Error", 500)]
    [InlineData("throw-concurrency", 409)]
    [InlineData("throw-unique", 409)]
    [InlineData("throw-private", 500)]
    public async Task RealHttpPipelineMapsApplicationFailuresAndHidesPrivateExceptions(string failure, int status)
    {
        using var authenticated = await ApiTestClient.LoginAsync(factory);
        using var host = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.Replace(ServiceDescriptor.Scoped<IQueryHandler<GetContainersQuery, PagedResult<ContainerDto>>>(
                _ => new FailureHandler(failure)))));
        using var client = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authenticated.DefaultRequestHeaders.Authorization!.Parameter);
        var response = await client.GetAsync("/api/containers/", TestContext.Current.CancellationToken);
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(status, problem.GetProperty("status").GetInt32());
        Assert.DoesNotContain("Private", problem.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", problem.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FailureHandler(string failure) : IQueryHandler<GetContainersQuery, PagedResult<ContainerDto>>
    {
        public Task<Result<PagedResult<ContainerDto>>> Handle(GetContainersQuery query, CancellationToken cancellationToken = default) => failure switch
        {
            "throw-concurrency" => throw new DbUpdateConcurrencyException("Private row details"),
            "throw-unique" => throw new DbUpdateException("Private SQL", new PostgresException("Private unique detail", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation)),
            "throw-private" => throw new InvalidOperationException("Private internal failure"),
            _ => Task.FromResult(Result.Failure<PagedResult<ContainerDto>>(new Error(failure, "The operation was rejected.")))
        };
    }
}
