using Scalar.AspNetCore;
using Microsoft.AspNetCore.Routing;
using SnpDepotManagement.Api.Authorization;
using SnpDepotManagement.Api.Endpoints;
using SnpDepotManagement.Api.Extensions;
using SnpDepotManagement.Application;
using SnpDepotManagement.Infrastructure;
using SnpDepotManagement.Infrastructure.Persistence.Seed;
using SnpDepotManagement.Infrastructure.Tenancy;

var builder = WebApplication.CreateBuilder(args);
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
app.UseStatusCodePages(async statusContext =>
{
    var http = statusContext.HttpContext;
    var status = http.Response.StatusCode;
    var code = status switch { 401 => "Auth.Unauthorized", 403 => "Auth.Forbidden", 404 => "Resource.NotFound", 413 => "Request.TooLarge", _ => "Request.Failed" };
    await ResultExtensions.Problem(status, code).ExecuteAsync(http);
});
app.UseCors();
if (app.Environment.IsDevelopment()) app.UseHttpsRedirection();

// Resolve tenant only after authentication and authorization; header cannot select a depot.
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantMiddleware>();
app.MapHealthChecks("/health/live").WithName("Liveness").WithTags("Health").AllowAnonymous();
app.MapHealthChecks("/health/ready").WithName("Readiness").WithTags("Health").RequireAuthorization(Policies.Admin);

// Map Minimal API Endpoints
app.MapIdentityEndpoints();
app.MapContainerEndpoints();
app.MapMasterDataEndpoints();

app.Run();

public partial class Program { }
