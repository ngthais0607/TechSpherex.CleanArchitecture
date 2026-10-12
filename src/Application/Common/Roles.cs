namespace SnpDepotManagement.Application.Common;

public static class Roles
{
    public const string Admin = "Admin";
    public const string YardPlanner = "YardPlanner";
    public const string OrderClerk = "OrderClerk";
    public const string GateClerk = "GateClerk";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Admin,
        YardPlanner,
        OrderClerk,
        GateClerk
    };
}
