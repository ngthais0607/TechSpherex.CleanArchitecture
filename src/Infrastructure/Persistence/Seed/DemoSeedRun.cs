namespace SnpDepotManagement.Infrastructure.Persistence.Seed;

public sealed class DemoSeedRun
{
    public string TenantId { get; private set; } = string.Empty;
    public string DatasetVersion { get; private set; } = string.Empty;
    public DateTime CompletedAtUtc { get; private set; }

    private DemoSeedRun() { }

    public DemoSeedRun(string tenantId, string datasetVersion, DateTime completedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetVersion);
        if (completedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Seed completion must use UTC.", nameof(completedAtUtc));
        TenantId = tenantId;
        DatasetVersion = datasetVersion;
        CompletedAtUtc = completedAtUtc;
    }
}
