using SnpDepotManagement.Application.Features.Containers.Queries.GetContainerSummary;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.UnitTests.Containers;

public class GetContainerSummaryQueryHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly GetContainerSummaryQueryHandler _handler;

    public GetContainerSummaryQueryHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new GetContainerSummaryQueryHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenContainersExist_ShouldReturnAccurateSummaryStatistics()
    {
        // 2x 20ft Dry Normal (MAEU)
        var c1 = CreateContainer("MAE", "100001", ContainerSize.Ft20, ContainerType.Dry, "MAEU");
        var c2 = CreateContainer("MAE", "100002", ContainerSize.Ft20, ContainerType.Dry, "MAEU");

        // 1x 40ft Reefer Damaged (MAEU)
        var c3 = CreateContainer("MAE", "100003", ContainerSize.Ft40, ContainerType.Reefer, "MAEU");
        c3.MarkDamaged(ContainerCondition.Damaged);

        // 1x 40ft FlatRack Dented (ONE)
        var c4 = CreateContainer("ONE", "200001", ContainerSize.Ft40, ContainerType.FlatRack, "ONE");
        c4.MarkDamaged(ContainerCondition.Dented);

        _context.Containers.AddRange(c1, c2, c3, c4);
        await _context.SaveChangesAsync();

        var query = new GetContainerSummaryQuery();
        var result = await _handler.Handle(query);

        result.IsSuccess.Should().BeTrue();
        var summary = result.Value!;
        summary.Total.Should().Be(4);

        summary.BySize["Ft20"].Should().Be(2);
        summary.BySize["Ft40"].Should().Be(2);

        summary.ByType["Dry"].Should().Be(2);
        summary.ByType["Reefer"].Should().Be(1);
        summary.ByType["FlatRack"].Should().Be(1);

        summary.ByCondition["Normal"].Should().Be(2);
        summary.ByCondition["Damaged"].Should().Be(1);
        summary.ByCondition["Dented"].Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenFilterByOwnerCode_ShouldReturnFilteredSummary()
    {
        var c1 = CreateContainer("MAE", "100001", ContainerSize.Ft20, ContainerType.Dry, "MAEU");
        var c2 = CreateContainer("ONE", "200001", ContainerSize.Ft40, ContainerType.Dry, "ONE");

        _context.Containers.AddRange(c1, c2);
        await _context.SaveChangesAsync();

        var query = new GetContainerSummaryQuery(OwnerCode: "MAEU");
        var result = await _handler.Handle(query);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(1);
        result.Value.BySize["Ft20"].Should().Be(1);
        result.Value.BySize.ContainsKey("Ft40").Should().BeFalse();
    }

    private static Container CreateContainer(string ownerPrefix, string serialNumber, ContainerSize size, ContainerType type, string ownerCode)
    {
        string raw = $"{ownerPrefix}U{serialNumber}";
        int sum = 0;
        for (int i = 0; i < 10; i++)
        {
            char c = raw[i];
            int val = char.IsLetter(c) ? (c - 'A' + 10) : (c - '0');
            if (char.IsLetter(c))
            {
                if (val >= 11) val++;
                if (val >= 22) val++;
                if (val >= 33) val++;
            }
            sum += val * (1 << i);
        }
        int rem = sum % 11;
        int checkDigit = rem == 10 ? 0 : rem;

        var number = ContainerNumber.Create($"{raw}{checkDigit}").Value!;
        return Container.Create(number, "22G1", size, type, 2200, 30480, null, ownerCode).Value!;
    }
}
