namespace SnpDepotManagement.Application.Abstractions.Tenancy;

public interface ITenantProvider
{
    TenantInfo? GetCurrentTenant();
    string? GetCurrentTenantId();
}
