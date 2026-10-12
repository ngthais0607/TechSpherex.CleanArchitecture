using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Containers.Dtos;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.Features.Containers.Queries.GetContainerByNumber;

public sealed class GetContainerByNumberQueryHandler : IQueryHandler<GetContainerByNumberQuery, ContainerDto>
{
    private readonly IAppDbContext _context;

    public GetContainerByNumberQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ContainerDto>> Handle(GetContainerByNumberQuery query, CancellationToken cancellationToken = default)
    {
        var numberResult = ContainerNumber.Create(query.ContainerNumber);
        if (numberResult.IsFailure)
        {
            return Result.Failure<ContainerDto>(numberResult.Error);
        }

        var containerNumber = numberResult.Value!;

        var container = await _context.Containers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ContainerNumber == containerNumber, cancellationToken);

        if (container is null)
        {
            return Result.Failure<ContainerDto>(
                Error.NotFound("Container.NotFound", $"Container '{containerNumber.Value}' not found."));
        }

        var dto = new ContainerDto(
            container.Id,
            container.ContainerNumber.Value,
            container.IsoCode,
            container.Size,
            container.Type,
            container.TareWeight,
            container.MaxGrossWeight,
            container.OwnerCode,
            container.Condition,
            container.Classification,
            container.DateOfManufacture);

        return Result.Success(dto);
    }
}
