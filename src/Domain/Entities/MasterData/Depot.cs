using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Domain.Entities.MasterData;

public sealed class Depot : BaseEntity
{
    // Id (từ BaseEntity) = TenantId (Guid) — seed bởi Infrastructure
    public string Name { get; private set; } = string.Empty;
    public string Timezone { get; private set; } = "Asia/Ho_Chi_Minh";

    private Depot() { }

    public static Depot Create(Guid tenantId, string name, string timezone = "Asia/Ho_Chi_Minh")
    {
        var depot = new Depot { Name = name.Trim(), Timezone = timezone };
        depot.Id = tenantId;
        return depot;
    }
}
