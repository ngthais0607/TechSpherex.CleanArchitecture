using Scalar.AspNetCore;
using SnpDepotManagement.Api.Endpoints;
using SnpDepotManagement.Application;
using SnpDepotManagement.Infrastructure;
using SnpDepotManagement.Infrastructure.Persistence.Seed;
using SnpDepotManagement.Infrastructure.Tenancy;

var builder = WebApplication.CreateBuilder(args);

// Register application & infrastructure services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("YardPlanner", policy => policy.RequireRole("YardPlanner", "Admin"));
    options.AddPolicy("OrderClerk", policy => policy.RequireRole("OrderClerk", "Admin"));
    options.AddPolicy("GateClerk", policy => policy.RequireRole("GateClerk", "Admin"));
});

// Configure OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Seed Database automatically on startup (creates roles & admin user)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var seeder = scope.ServiceProvider.GetRequiredService<IAppDbSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Seeding could not complete at startup (e.g. database not migrated or offline in current environment).");
    }
}

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Multi-tenancy resolution middleware (reads X-Tenant-Id header)
app.UseMiddleware<TenantMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Map Minimal API Endpoints
app.MapIdentityEndpoints();
app.MapContainerEndpoints();

app.Run();

public partial class Program { }
