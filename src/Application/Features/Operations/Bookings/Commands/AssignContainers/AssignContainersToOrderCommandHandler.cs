using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.Rules;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.Features.Operations.Bookings.Commands.AssignContainers;

public sealed class AssignContainersToOrderCommandHandler : ICommandHandler<AssignContainersToOrderCommand, AssignContainersResult>
{
    private readonly IAppDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentUser? _currentUser;

    public AssignContainersToOrderCommandHandler(
        IAppDbContext context,
        TimeProvider? timeProvider = null,
        ICurrentUser? currentUser = null)
    {
        _context = context;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _currentUser = currentUser;
    }

    public async Task<Result<AssignContainersResult>> Handle(
        AssignContainersToOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ContainerNumbers is null || command.ContainerNumbers.Count == 0)
        {
            return Result.Failure<AssignContainersResult>(
                Error.Validation("AssignContainers.EmptyContainers", "Danh sách container không được để trống."));
        }

        var normalizedNumber = command.OrderNumber?.Trim().ToUpperInvariant() ?? string.Empty;
        var order = await _context.DeliveryOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == normalizedNumber, cancellationToken);

        if (order is null)
        {
            return Result.Failure<AssignContainersResult>(
                Error.NotFound("DeliveryOrder.NotFound", $"Không tìm thấy lệnh giao '{command.OrderNumber}'."));
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var dispatchValidation = order.ValidateForDispatch(utcNow);
        if (dispatchValidation.IsFailure)
        {
            return Result.Failure<AssignContainersResult>(dispatchValidation.Error);
        }

        var requestedNumbers = command.ContainerNumbers
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        if (requestedNumbers.Count == 0)
        {
            return Result.Failure<AssignContainersResult>(
                Error.Validation("AssignContainers.EmptyContainers", "Danh sách container không hợp lệ."));
        }

        var validNumbers = new List<ContainerNumber>();
        foreach (var num in requestedNumbers)
        {
            var parsed = ContainerNumber.Create(num);
            if (parsed.IsSuccess && parsed.Value is not null)
            {
                validNumbers.Add(parsed.Value);
            }
        }

        var containers = await _context.Containers
            .Where(c => validNumbers.Contains(c.ContainerNumber))
            .ToListAsync(cancellationToken);

        var foundNumbers = containers.Select(c => c.ContainerNumber.Value).ToHashSet();
        var failedContainers = requestedNumbers.Where(n => !foundNumbers.Contains(n)).ToList();

        if (containers.Count == 0)
        {
            return Result.Failure<AssignContainersResult>(
                Error.NotFound("Container.NotFound", "Không tìm thấy container nào trong danh mục."));
        }

        var containerIds = containers.Select(c => c.Id).ToList();
        var inYardLifecycles = await _context.ContainerLifecycles
            .Where(l => containerIds.Contains(l.ContainerId) && l.Status == LifecycleStatus.InYard)
            .ToDictionaryAsync(l => l.ContainerId, cancellationToken);

        // Lấy danh sách booking Reserved hiện tại của order này
        var reservedCountsByItem = await _context.Bookings
            .Where(b => b.OrderId == order.Id && b.Status == BookingStatus.Reserved)
            .GroupBy(b => b.ItemId)
            .Select(g => new { ItemId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.ItemId, g => g.Count, cancellationToken);

        var newBookings = new List<DeliveryOrderBooking>();
        var tenantId = !string.IsNullOrEmpty(_currentUser?.TenantId) ? _currentUser.TenantId : order.TenantId;

        foreach (var container in containers)
        {
            if (!inYardLifecycles.ContainsKey(container.Id))
            {
                return Result.Failure<AssignContainersResult>(
                    Error.Conflict("Lifecycle.NotInYard", $"Container '{container.ContainerNumber}' hiện không có trong bãi."));
            }

            // Tra cứu hãng khai thác của container
            var ownerCodeRecord = await _context.LineOperatorOwnerCodes
                .FirstOrDefaultAsync(x => x.OwnerCode == container.OwnerCode, cancellationToken);

            string? resolvedOpCode = null;
            if (ownerCodeRecord is not null)
            {
                resolvedOpCode = await _context.LineOperators
                    .Where(l => l.Id == ownerCodeRecord.LineOperatorId)
                    .Select(l => l.Code)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            else
            {
                var directOp = await _context.LineOperators
                    .FirstOrDefaultAsync(l => l.Code == container.OwnerCode, cancellationToken);
                if (directOp is not null)
                {
                    resolvedOpCode = directOp.Code;
                }
                else if (string.Equals(container.OwnerCode, order.LineOperator, StringComparison.OrdinalIgnoreCase))
                {
                    resolvedOpCode = container.OwnerCode;
                }
            }

            var lineOpRule = new ContainerBelongsToLineOperatorRule(resolvedOpCode, order.LineOperator);
            if (lineOpRule.IsBroken())
            {
                return Result.Failure<AssignContainersResult>(lineOpRule.Error);
            }

            // Kiểm tra booking hiện tại của container
            var activeBooking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.ContainerId == container.Id && b.Status == BookingStatus.Reserved, cancellationToken);

            if (activeBooking is not null)
            {
                if (activeBooking.OrderId == order.Id)
                {
                    // Đã assign cho chính order này rồi -> bỏ qua không tạo trùng
                    continue;
                }

                // Kiểm tra lệnh của activeBooking xem đã hết hạn chưa
                var otherOrder = await _context.DeliveryOrders
                    .FirstOrDefaultAsync(o => o.Id == activeBooking.OrderId, cancellationToken);

                if (otherOrder is not null && otherOrder.ExpiryDate < utcNow)
                {
                    activeBooking.Expire(utcNow);
                }
                else
                {
                    return Result.Failure<AssignContainersResult>(
                        Error.Conflict("Booking.ContainerAlreadyReserved", $"Container '{container.ContainerNumber}' đã được giữ chỗ cho lệnh khác."));
                }
            }

            // Khớp item trong order còn quota
            var availableItem = order.Items.FirstOrDefault(i =>
                i.Size == container.Size &&
                i.Type == container.Type &&
                (i.RemainingQuantity - reservedCountsByItem.GetValueOrDefault(i.Id, 0)) > 0);

            if (availableItem is null)
            {
                return Result.Failure<AssignContainersResult>(
                    Error.Conflict("Booking.QuotaExceeded", $"Lệnh giao không còn hạn mức cho container loại {container.Size}/{container.Type}."));
            }

            reservedCountsByItem[availableItem.Id] = reservedCountsByItem.GetValueOrDefault(availableItem.Id, 0) + 1;

            var booking = DeliveryOrderBooking.Reserve(order.Id, availableItem.Id, container.Id, tenantId, utcNow);
            newBookings.Add(booking);
        }

        if (newBookings.Count > 0)
        {
            _context.Bookings.AddRange(newBookings);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Result.Failure<AssignContainersResult>(
                    Error.Conflict("Booking.ContainerAlreadyReserved", "Một hoặc nhiều container đã được giữ chỗ bởi phiên làm việc khác."));
            }
        }

        return Result.Success(new AssignContainersResult(
            order.Id,
            command.ContainerNumbers.Count,
            newBookings.Count,
            newBookings.Select(b => b.Id).ToList(),
            failedContainers));
    }
}
