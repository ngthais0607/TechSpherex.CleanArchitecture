using System.Globalization;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Infrastructure.Persistence.Seed;

public enum DemoContainerScenario { Physical, Virtual, Shipped, NeverIn }

public sealed record DemoContainerSpec(int Index, string Owner, string LineOperator,
    ContainerSize Size, ContainerType Type, DemoContainerScenario Scenario, string? OrderNumber)
{
    public string Number
    {
        get
        {
            var serial = (900000 + Index).ToString("D6", CultureInfo.InvariantCulture);
            return $"{Owner}U{serial}{ContainerNumberGenerator.ComputeCheckDigitChar(Owner, "U", serial, false)}";
        }
    }
}

public static class DemoDataset
{
    public const string Version = "fe-be-v1";
    public static IReadOnlyList<DemoContainerSpec> Containers { get; } = Build();
    public static readonly IReadOnlyList<string> OrderNumbers =
        ["DO-2026-001", "DO-2026-002", "DO-2026-003", "DO-2026-004", "DO-2026-005"];
    public static readonly IReadOnlyList<string> BlockCodes = ["A", "B", "VB-01"];
    public static readonly IReadOnlyList<string> CustomerTaxCodes = ["0102030405", "0109876543", "0105556667"];

    private static IReadOnlyList<DemoContainerSpec> Build()
    {
        var result = new List<DemoContainerSpec>();
        Add(0, 3, "MSC", "MSC", ContainerSize.Ft20, ContainerType.Dry, DemoContainerScenario.Physical, "DO-2026-001");
        Add(3, 7, "MED", "MSC", ContainerSize.Ft20, ContainerType.Dry, DemoContainerScenario.Physical);
        Add(10, 3, "MAE", "MAEU", ContainerSize.Ft20, ContainerType.Dry, DemoContainerScenario.Physical, "DO-2026-004");
        Add(13, 2, "MSC", "MSC", ContainerSize.Ft20, ContainerType.Dry, DemoContainerScenario.Physical);
        Add(15, 2, "MSC", "MSC", ContainerSize.Ft20, ContainerType.Reefer, DemoContainerScenario.Virtual, "DO-2026-003");
        Add(17, 3, "MED", "MSC", ContainerSize.Ft20, ContainerType.Reefer, DemoContainerScenario.Virtual);
        Add(20, 3, "MSC", "MSC", ContainerSize.Ft20, ContainerType.Dry, DemoContainerScenario.Shipped, "DO-2026-005");
        Add(23, 12, "MSC", "MSC", ContainerSize.Ft20, ContainerType.Dry, DemoContainerScenario.NeverIn);
        Add(35, 2, "MAE", "MAEU", ContainerSize.Ft40, ContainerType.Dry, DemoContainerScenario.NeverIn);
        Add(37, 13, "MSK", "MAEU", ContainerSize.Ft40, ContainerType.Dry, DemoContainerScenario.NeverIn);
        return result.AsReadOnly();

        void Add(int start, int count, string owner, string lineOperator, ContainerSize size,
            ContainerType type, DemoContainerScenario scenario, string? order = null)
        {
            for (var index = start; index < start + count; index++)
                result.Add(new(index, owner, lineOperator, size, type, scenario, order));
        }
    }
}
