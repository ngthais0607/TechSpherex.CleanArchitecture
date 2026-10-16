namespace SnpDepotManagement.Api.Authorization;

public static class Policies
{
    public const string Admin = nameof(Admin);
    public const string ReadMasterData = nameof(ReadMasterData);
    public const string ReadContainers = nameof(ReadContainers);
    public const string RegisterContainer = nameof(RegisterContainer);
    public const string ManageYard = nameof(ManageYard);
    public const string ReadYard = nameof(ReadYard);
    public const string ManageDeliveryOrders = nameof(ManageDeliveryOrders);
    public const string ReadDeliveryOrders = nameof(ReadDeliveryOrders);
    public const string ManageGate = nameof(ManageGate);
    public const string ReadReports = nameof(ReadReports);
    public const string ReadExpiringOrders = nameof(ReadExpiringOrders);
}
