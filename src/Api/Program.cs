using Scalar.AspNetCore;
using FluentValidation;
using SnpDepotManagement.Api.Contracts;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using SnpDepotManagement.Api.Authorization;
using SnpDepotManagement.Api.Endpoints;
using SnpDepotManagement.Api.Extensions;
using SnpDepotManagement.Application;
using SnpDepotManagement.Infrastructure;
using SnpDepotManagement.Infrastructure.Persistence.Seed;
using SnpDepotManagement.Infrastructure.Tenancy;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    options.SerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict;
});
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict;
});
var trustedProxy = builder.Configuration["ReverseProxy:TrustedProxy"];
if (!string.IsNullOrWhiteSpace(trustedProxy))
{
    if (!IPAddress.TryParse(trustedProxy, out var proxyAddress))
        throw new InvalidOperationException("ReverseProxy:TrustedProxy must be an IP address.");
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        options.KnownProxies.Add(proxyAddress);
    });
}
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 1024 * 1024);
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Contains("*")) throw new InvalidOperationException("CORS requires explicit allowed origins.");
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Length > 0) policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddHealthChecks();
builder.Services.AddTransient<IValidator<YardBlockSizeRequest>, YardBlockSizeRequestValidator>();
builder.Services.AddTransient<IValidator<UpdateTransactionRequest>, UpdateTransactionRequestValidator>();
builder.Services.AddTransient<IValidator<CancelTransactionRequest>, CancelTransactionRequestValidator>();
builder.Services.AddTransient<IValidator<PlaceFromVirtualBlockRequest>, PlaceFromVirtualBlockRequestValidator>();
builder.Services.AddTransient<IValidator<AssignContainersRequest>, AssignContainersRequestValidator>();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", context =>
    {
        var partitionKey = context.Connection.RemoteIpAddress?.ToString()
            ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? context.TraceIdentifier;

        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Register application & infrastructure services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add Authorization policies
builder.Services.AddApiAuthorization();

// Configure OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Seed Database automatically on startup (creates roles & admin user)
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IAppDbSeeder>();
    await seeder.SeedAsync();
}

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.UseExceptionHandler();
if (!string.IsNullOrWhiteSpace(trustedProxy)) app.UseForwardedHeaders();
app.UseStatusCodePages(async statusContext =>
{
    var http = statusContext.HttpContext;
    var status = http.Response.StatusCode;
    var code = status switch { 401 => "Auth.Unauthorized", 403 => "Auth.Forbidden", 404 => "Resource.NotFound", 413 => "Request.TooLarge", _ => "Request.Failed" };
    await ResultExtensions.Problem(status, code).ExecuteAsync(http);
});
app.UseCors();
app.UseRateLimiter();
if (app.Environment.IsDevelopment()) app.UseHttpsRedirection();

// Resolve tenant only after authentication and authorization; header cannot select a depot.
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantMiddleware>();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
    .WithName("Liveness").WithTags("Health").AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    ResponseWriter = (http, report) => http.Response.WriteAsJsonAsync(new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString(), description = e.Value.Description })
    }, cancellationToken: http.RequestAborted)
}).WithName("Readiness").WithTags("Health").RequireAuthorization(Policies.Admin);

// Map Minimal API Endpoints
app.MapIdentityEndpoints();
app.MapContainerEndpoints();
app.MapMasterDataEndpoints();
app.MapYardEndpoints();
app.MapDeliveryOrderEndpoints();
app.MapGateEndpoints();
app.MapReportEndpoints();

app.Run();

public partial class Program { }
