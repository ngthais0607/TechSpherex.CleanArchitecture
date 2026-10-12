using SnpDepotManagement.Application.Features.Containers.Queries.GetContainers;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.UnitTests.Containers;

public class GetContainersQueryHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly GetContainersQueryHandler _handler;

    public GetContainersQueryHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new GetContainersQueryHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenFiltersAreApplied_ShouldReturnFilteredPagedResult()
    {
        var c1 = CreateContainer("MAE", "100001", ContainerSize.Ft20, "MAEU");
        var c2 = CreateContainer("MAE", "100002", ContainerSize.Ft40, "MAEU");
        var c3 = CreateContainer("ONE", "100003", ContainerSize.Ft20, "ONE");

        _context.Containers.AddRange(c1, c2, c3);
        await _context.SaveChangesAsync();

        var query = new GetContainersQuery(OwnerCode: "MAEU", Size: ContainerSize.Ft20, PageIndex: 1, PageSize: 10);

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().ContainerNumber.Should().Be(c1.ContainerNumber.Value);
    }

    private static Container CreateContainer(string ownerPrefix, string serialNumber, ContainerSize size, string ownerCode)
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
        return Container.Create(number, "22G1", size, ContainerType.Dry, 2200, 30480, null, ownerCode).Value!;
    }
}
