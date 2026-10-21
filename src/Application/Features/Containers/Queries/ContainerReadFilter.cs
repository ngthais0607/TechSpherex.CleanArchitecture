using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Containers.Queries;

public sealed record ContainerFilter(string? Search = null, string? OwnerCode = null, ContainerSize? Size = null,
    ContainerCondition? Condition = null, ContainerType? Type = null, string? LineOperator = null,
    string? DisplayStatus = null, string? Booking = null);

public static class ContainerReadFilter
{
    public static bool IsValid(ContainerFilter filter) =>
        (string.IsNullOrWhiteSpace(filter.Booking) || filter.Booking.Trim().ToLowerInvariant() is "assigned" or "unassigned") &&
        (string.IsNullOrWhiteSpace(filter.DisplayStatus) || filter.DisplayStatus.Trim().ToLowerInvariant() is "reserved" or "inyard" or "outofyard" or "neverin");

    public static bool IsValidSort(string? sort) => string.IsNullOrWhiteSpace(sort) || sort.Trim().ToLowerInvariant() is "number" or "size";

    public static IQueryable<Container> Apply(IQueryable<Container> source, IAppDbContext db, ContainerFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToUpperInvariant();
            if (db is DbContext context && context.Database.ProviderName?.Contains("InMemory") == true)
                source = source.Where(container => container.IsoCode.Contains(search) || container.OwnerCode.Contains(search) || container.ContainerNumber.Value.Contains(search));
            else
            {
                var pattern = "%" + search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
                source = source.Where(container => EF.Functions.Like(container.IsoCode, pattern, "\\") ||
                    EF.Functions.Like(container.OwnerCode, pattern, "\\") || EF.Functions.Like((string)(object)container.ContainerNumber, pattern, "\\"));
            }
        }
        if (!string.IsNullOrWhiteSpace(filter.OwnerCode))
        {
            var owner = filter.OwnerCode.Trim().ToUpperInvariant();
            source = source.Where(container => container.OwnerCode == owner);
        }
        if (filter.Size.HasValue) source = source.Where(container => container.Size == filter.Size);
        if (filter.Type.HasValue) source = source.Where(container => container.Type == filter.Type);
        if (filter.Condition.HasValue) source = source.Where(container => container.Condition == filter.Condition);
        if (!string.IsNullOrWhiteSpace(filter.LineOperator))
        {
            var op = filter.LineOperator.Trim().ToUpperInvariant();
            source = source.Where(container => container.OwnerCode == op || db.LineOperatorOwnerCodes.Any(mapping =>
                mapping.OwnerCode == container.OwnerCode && db.LineOperators.Any(line => line.Id == mapping.LineOperatorId && line.Code == op)));
        }
        if (filter.Booking?.Trim().ToLowerInvariant() is "assigned")
            source = source.Where(container => db.Bookings.Any(booking => booking.ContainerId == container.Id && booking.Status == BookingStatus.Reserved));
        if (filter.Booking?.Trim().ToLowerInvariant() is "unassigned")
            source = source.Where(container => !db.Bookings.Any(booking => booking.ContainerId == container.Id && booking.Status == BookingStatus.Reserved));
        switch (filter.DisplayStatus?.Trim().ToLowerInvariant())
        {
            case "reserved":
                source = source.Where(container => db.Bookings.Any(booking => booking.ContainerId == container.Id && booking.Status == BookingStatus.Reserved));
                break;
            case "inyard":
                source = source.Where(container => !db.Bookings.Any(booking => booking.ContainerId == container.Id && booking.Status == BookingStatus.Reserved) &&
                    db.ContainerLifecycles.Any(lifecycle => lifecycle.ContainerId == container.Id && lifecycle.Status == LifecycleStatus.InYard));
                break;
            case "outofyard":
                source = source.Where(container => !db.Bookings.Any(booking => booking.ContainerId == container.Id && booking.Status == BookingStatus.Reserved) &&
                    !db.ContainerLifecycles.Any(lifecycle => lifecycle.ContainerId == container.Id && lifecycle.Status == LifecycleStatus.InYard) &&
                    db.ContainerLifecycles.Any(lifecycle => lifecycle.ContainerId == container.Id && lifecycle.Status == LifecycleStatus.GatedOut));
                break;
            case "neverin":
                source = source.Where(container => !db.Bookings.Any(booking => booking.ContainerId == container.Id && booking.Status == BookingStatus.Reserved) &&
                    !db.ContainerLifecycles.Any(lifecycle => lifecycle.ContainerId == container.Id &&
                        (lifecycle.Status == LifecycleStatus.InYard || lifecycle.Status == LifecycleStatus.GatedOut)));
                break;
        }
        return source;
    }

    public static IOrderedQueryable<Container> Order(IQueryable<Container> source, string? sort, bool descending)
    {
        var ordered = (sort?.Trim().ToLowerInvariant(), descending) switch
        {
            ("number", false) => source.OrderBy(container => container.ContainerNumber),
            ("number", true) => source.OrderByDescending(container => container.ContainerNumber),
            ("size", false) => source.OrderBy(container => container.Size),
            ("size", true) => source.OrderByDescending(container => container.Size),
            (_, true) => source.OrderByDescending(container => container.IsoCode),
            _ => source.OrderBy(container => container.IsoCode)
        };
        return ordered.ThenBy(container => container.Id);
    }
}
