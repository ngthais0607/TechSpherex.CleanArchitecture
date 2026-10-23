using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Domain.Entities.MasterData;

public sealed class LineOperatorOwnerCode : BaseEntity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public Guid LineOperatorId { get; private set; }
    public string OwnerCode { get; private set; } = string.Empty;

    private LineOperatorOwnerCode() { }

    public static LineOperatorOwnerCode Create(Guid lineOperatorId, string ownerCode, string tenantId = "")
        => new() { LineOperatorId = lineOperatorId, OwnerCode = ownerCode.Trim().ToUpperInvariant(), TenantId = tenantId };
}
