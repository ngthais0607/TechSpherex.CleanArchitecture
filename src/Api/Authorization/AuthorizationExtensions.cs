using Microsoft.AspNetCore.Authorization;
using SnpDepotManagement.Application.Common;

namespace SnpDepotManagement.Api.Authorization;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddApiAuthorization(this IServiceCollection services) => services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        void Policy(string name, params string[] roles) => options.AddPolicy(name,
            policy => policy.RequireAuthenticatedUser().RequireRole(roles));
        Policy(Policies.Admin, Roles.Admin);
        Policy(Policies.ReadMasterData, Roles.All.ToArray());
        Policy(Policies.ReadContainers, Roles.All.ToArray());
        Policy(Policies.RegisterContainer, Roles.Admin, Roles.GateClerk);
        Policy(Policies.ManageYard, Roles.Admin, Roles.YardPlanner);
        Policy(Policies.ReadYard, Roles.Admin, Roles.YardPlanner, Roles.GateClerk);
        Policy(Policies.ManageDeliveryOrders, Roles.Admin, Roles.OrderClerk);
        Policy(Policies.ReadDeliveryOrders, Roles.Admin, Roles.OrderClerk, Roles.GateClerk);
        Policy(Policies.ManageGate, Roles.Admin, Roles.GateClerk);
        Policy(Policies.ReadReports, Roles.All.ToArray());
        Policy(Policies.ReadExpiringOrders, Roles.Admin, Roles.OrderClerk, Roles.GateClerk);
    });
}
