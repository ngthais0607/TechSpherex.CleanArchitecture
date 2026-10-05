using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Domain.UnitTests.Yard;

public sealed class YardPositionTests
{
    [Fact]
    public void Create_WithValidCoordinates_ShouldSucceed()
    {
        var result = YardPosition.Create("BLOCK-A", 1, 2, 3);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Block.Should().Be("BLOCK-A");
        result.Value.Bay.Should().Be(1);
        result.Value.Row.Should().Be(2);
        result.Value.Tier.Should().Be(3);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(-1, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    public void Create_WhenCoordinateLessOrEqualToZero_ShouldReturnError(int bay, int row, int tier)
    {
        var result = YardPosition.Create("BLOCK-A", bay, row, tier);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("YardPosition.InvalidCoordinate");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenBlockIsEmpty_ShouldReturnError(string? block)
    {
        var result = YardPosition.Create(block!, 1, 1, 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("YardPosition.EmptyBlock");
    }
}
