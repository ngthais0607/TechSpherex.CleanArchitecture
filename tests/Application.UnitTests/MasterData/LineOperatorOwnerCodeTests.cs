using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.AddOwnerCode;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.RemoveOwnerCode;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Queries.GetOwnerCodes;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.MasterData;

namespace SnpDepotManagement.Application.UnitTests.MasterData;

public sealed class LineOperatorOwnerCodeTests
{
    private readonly TestAppDbContext _context = TestAppDbContext.Create();

    [Fact]
    public async Task AddOwnerCode_WhenOperatorNotFound_ReturnsNotFound()
    {
        var handler = new AddOwnerCodeCommandHandler(_context);
        var result = await handler.Handle(new AddOwnerCodeCommand(Guid.NewGuid(), "ONE"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LineOperator.NotFound");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ABCDE")] // 5 chars
    public async Task AddOwnerCode_WhenInvalidCode_ReturnsValidation(string code)
    {
        var op = LineOperator.Create("ONE", "Ocean Network Express").Value!;
        _context.LineOperators.Add(op);
        await _context.SaveChangesAsync();

        var handler = new AddOwnerCodeCommandHandler(_context);
        var result = await handler.Handle(new AddOwnerCodeCommand(op.Id, code));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LineOperator.InvalidOwnerCode");
    }

    [Fact]
    public async Task AddOwnerCode_WhenAlreadyExists_ReturnsConflict()
    {
        var op1 = LineOperator.Create("ONE", "Ocean Network Express").Value!;
        var op2 = LineOperator.Create("NYK", "NYK Line").Value!;
        _context.LineOperators.AddRange(op1, op2);
        _context.LineOperatorOwnerCodes.Add(LineOperatorOwnerCode.Create(op1.Id, "ONE"));
        await _context.SaveChangesAsync();

        var handler = new AddOwnerCodeCommandHandler(_context);
        var result = await handler.Handle(new AddOwnerCodeCommand(op2.Id, "one")); // case-insensitive duplicate

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LineOperator.OwnerCodeAlreadyExists");
    }

    [Fact]
    public async Task AddOwnerCode_WhenValid_AddsAndSucceeds()
    {
        var op = LineOperator.Create("ONE", "Ocean Network Express").Value!;
        _context.LineOperators.Add(op);
        await _context.SaveChangesAsync();

        var handler = new AddOwnerCodeCommandHandler(_context);
        var result = await handler.Handle(new AddOwnerCodeCommand(op.Id, "nyk"));

        result.IsSuccess.Should().BeTrue();
        var record = await _context.LineOperatorOwnerCodes.FirstOrDefaultAsync(x => x.LineOperatorId == op.Id);
        record.Should().NotBeNull();
        record!.OwnerCode.Should().Be("NYK");
    }

    [Fact]
    public async Task RemoveOwnerCode_WhenNotFound_ReturnsNotFound()
    {
        var handler = new RemoveOwnerCodeCommandHandler(_context);
        var result = await handler.Handle(new RemoveOwnerCodeCommand(Guid.NewGuid(), "NONEXISTENT"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LineOperator.OwnerCodeNotFound");
    }

    [Fact]
    public async Task RemoveOwnerCode_WhenExists_RemovesAndSucceeds()
    {
        var op = LineOperator.Create("ONE", "Ocean Network Express").Value!;
        _context.LineOperators.Add(op);
        var code = LineOperatorOwnerCode.Create(op.Id, "MOL");
        _context.LineOperatorOwnerCodes.Add(code);
        await _context.SaveChangesAsync();

        var handler = new RemoveOwnerCodeCommandHandler(_context);
        var result = await handler.Handle(new RemoveOwnerCodeCommand(op.Id, "mol"));

        result.IsSuccess.Should().BeTrue();
        var remaining = await _context.LineOperatorOwnerCodes.AnyAsync(x => x.Id == code.Id);
        remaining.Should().BeFalse();
    }

    [Fact]
    public async Task GetOwnerCodes_WhenOperatorNotFound_ReturnsNotFound()
    {
        var handler = new GetOwnerCodesQueryHandler(_context);
        var result = await handler.Handle(new GetOwnerCodesQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LineOperator.NotFound");
    }

    [Fact]
    public async Task GetOwnerCodes_WhenExists_ReturnsSortedCodes()
    {
        var op = LineOperator.Create("ONE", "Ocean Network Express").Value!;
        _context.LineOperators.Add(op);
        _context.LineOperatorOwnerCodes.AddRange(
            LineOperatorOwnerCode.Create(op.Id, "NYK"),
            LineOperatorOwnerCode.Create(op.Id, "KKL"),
            LineOperatorOwnerCode.Create(op.Id, "MOL"));
        await _context.SaveChangesAsync();

        var handler = new GetOwnerCodesQueryHandler(_context);
        var result = await handler.Handle(new GetOwnerCodesQuery(op.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new[] { "KKL", "MOL", "NYK" }, options => options.WithStrictOrdering());
    }
}
