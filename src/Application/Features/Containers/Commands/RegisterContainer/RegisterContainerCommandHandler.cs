using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.Features.Containers.Commands.RegisterContainer;

public sealed class RegisterContainerCommandHandler : ICommandHandler<RegisterContainerCommand, Guid>
{
    private readonly IAppDbContext _context;
    private readonly TimeProvider _timeProvider;

    public RegisterContainerCommandHandler(IAppDbContext context, TimeProvider? timeProvider = null)
    {
        _context = context;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Result<Guid>> Handle(RegisterContainerCommand command, CancellationToken cancellationToken = default)
    {
        var numberResult = ContainerNumber.Create(command.ContainerNumber);
        if (numberResult.IsFailure)
        {
            return Result.Failure<Guid>(numberResult.Error);
        }

        var containerNumber = numberResult.Value!;

        // Validate ISO format: 2 chữ số + 1 chữ hoa + 1 chữ số
        if (string.IsNullOrWhiteSpace(command.IsoCode) || !System.Text.RegularExpressions.Regex.IsMatch(command.IsoCode.Trim(), @"^\d{2}[A-Z]\d$"))
        {
            return Result.Failure<Guid>(Error.Validation("Container.InvalidIsoCode", "Mã ISO không đúng định dạng (vd: 22G1)."));
        }

        // Validate kích thước khớp ISO
        var trimmedIso = command.IsoCode.Trim().ToUpperInvariant();
        var expectedSize = trimmedIso[0] == '2' ? Domain.Enums.ContainerSize.Ft20
                         : trimmedIso[0] == '4' ? Domain.Enums.ContainerSize.Ft40
                         : (Domain.Enums.ContainerSize?)null;
        if (expectedSize.HasValue && expectedSize.Value != command.Size)
        {
            return Result.Failure<Guid>(Error.Validation("Container.IsoSizeMismatch", "Mã ISO không khớp với kích thước container."));
        }

        // Validate ngày sản xuất
        if (command.DateOfManufacture.HasValue)
        {
            var today = DepotCalendar.GetBusinessDate(_timeProvider.GetUtcNow().UtcDateTime);
            var mfgDate = DateOnly.FromDateTime(command.DateOfManufacture.Value);
            if (mfgDate > today)
            {
                return Result.Failure<Guid>(Error.Validation("Container.FutureManufactureDate", "Ngày sản xuất không được ở tương lai."));
            }
        }

        var exists = await _context.Containers
            .AnyAsync(c => c.ContainerNumber == containerNumber, cancellationToken);

        if (exists)
        {
            return Result.Failure<Guid>(
                Error.Conflict("Container.AlreadyExists", $"Container with number '{containerNumber.Value}' already exists in the depot."));
        }

        var containerResult = Container.Create(
            containerNumber,
            command.IsoCode,
            command.Size,
            command.Type,
            command.TareWeight,
            command.MaxGrossWeight,
            command.DateOfManufacture,
            command.Grade,
            command.Condition);

        if (containerResult.IsFailure)
        {
            return Result.Failure<Guid>(containerResult.Error);
        }

        var container = containerResult.Value!;
        _context.Containers.Add(container);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(container.Id);
    }
}
