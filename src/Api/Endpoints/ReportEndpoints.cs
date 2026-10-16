using SnpDepotManagement.Api.Authorization;
using SnpDepotManagement.Api.Contracts;
using SnpDepotManagement.Api.Extensions;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Reports.Dtos;
using SnpDepotManagement.Application.Features.Reports.Queries.GetYardInventory;
using SnpDepotManagement.Application.Features.Reports.Queries.GetDwellTimes;
using SnpDepotManagement.Application.Features.Reports.Queries.GetContainerHistory;
using SnpDepotManagement.Application.Features.Reports.Queries.GetExpiringOrders;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Api.Endpoints;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reports").RequireAuthorization().AddEndpointFilter<ValidationFilter>();
        group.MapGet("/yard-inventory", async (string? lineOperator, ContainerSize? size, ContainerType? type,
            ContainerCondition? condition, Guid? blockId, PaginationRequest page,
            IQueryHandler<GetYardInventoryQuery, InventoryReportDto> handler, CancellationToken ct) =>
            (await handler.Handle(new(lineOperator, size, type, condition, blockId, page.PageIndex, page.PageSize), ct)).ToHttpResult())
            .WithName("GetYardInventory").RequireAuthorization(Policies.ReadReports);
        group.MapGet("/dwell-times", async (string? lineOperator, int? minDays, bool? includeGatedOut, PaginationRequest page,
            IQueryHandler<GetDwellTimesQuery, PagedResult<DwellTimeDto>> handler, CancellationToken ct) =>
            minDays < 0 ? ResultExtensions.Problem(400, "Validation.Failed", "minDays must be nonnegative.") :
            (await handler.Handle(new(lineOperator, minDays, includeGatedOut ?? false, page.PageIndex, page.PageSize), ct)).ToHttpResult())
            .WithName("GetDwellTimes").RequireAuthorization(Policies.ReadReports);
        group.MapGet("/containers/{number}/history", async (string number, PaginationRequest page,
            IQueryHandler<GetContainerHistoryQuery, PagedResult<ContainerHistoryEventDto>> handler, CancellationToken ct) =>
            (await handler.Handle(new(number, page.PageIndex, page.PageSize), ct)).ToHttpResult())
            .WithName("GetContainerHistory").RequireAuthorization(Policies.ReadReports);
        group.MapGet("/expiring-orders", async (int? days, string? lineOperator, PaginationRequest page,
            IQueryHandler<GetExpiringOrdersQuery, PagedResult<ExpiringDeliveryOrderDto>> handler, CancellationToken ct) =>
            (await handler.Handle(new(days ?? 7, lineOperator, page.PageIndex, page.PageSize), ct)).ToHttpResult())
            .WithName("GetExpiringOrders").RequireAuthorization(Policies.ReadExpiringOrders);
        return app;
    }
}
