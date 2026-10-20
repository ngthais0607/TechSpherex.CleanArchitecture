using Microsoft.AspNetCore.Mvc;
using SnpDepotManagement.Api.Authorization;
using SnpDepotManagement.Api.Extensions;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Gate.Commands.CancelTransaction;
using SnpDepotManagement.Application.Features.Gate.Commands.UpdateTransaction;
using SnpDepotManagement.Application.Features.Gate.Queries.GetGateTransactions;
using SnpDepotManagement.Application.Features.Gate.Queries.ValidateGate;
using SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateIn;
using SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateOut;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Operations;

namespace SnpDepotManagement.Api.Endpoints;

public static class GateEndpoints
{
    public static IEndpointRouteBuilder MapGateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/gate")
            .WithTags("Gate")
            .RequireAuthorization(Policies.ManageGate)
            .AddEndpointFilter<ValidationFilter>();

        group.MapPost("/validate", async (
            ValidateGateQuery query,
            IQueryHandler<ValidateGateQuery, ValidateGateResponseDto> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(query, ct);
            return result.ToHttpResult();
        })
        .WithName("ValidateGate")
        .WithSummary("Kiểm tra điều kiện cổng (nhập/xuất) trước khi thực hiện giao dịch");

        group.MapPost("/in", async (GateInCommand command, ICommandHandler<GateInCommand, Guid> handler, CancellationToken ct) =>
            (await handler.Handle(command, ct)).ToHttpResult(id => Results.Json(new { id }, statusCode: 201)))
            .WithName("GateInContainer")
            .WithSummary("Ghi nhận xe/container vào cổng");

        group.MapPost("/out", async (GateOutCommand command, ICommandHandler<GateOutCommand> handler, CancellationToken ct) =>
            (await handler.Handle(command, ct)).ToHttpResult())
            .WithName("GateOutContainer")
            .WithSummary("Ghi nhận xe/container xuất cổng");

        group.MapGet("/transactions", async (
            [AsParameters] GetGateTransactionsQuery query,
            IQueryHandler<GetGateTransactionsQuery, PagedResult<GateTransactionDto>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(query, ct);
            return result.ToHttpResult();
        })
        .WithName("GetGateTransactions")
        .WithSummary("Danh sách lịch sử giao dịch cổng (có phân trang và tìm kiếm)");

        group.MapPut("/transactions/{id:guid}", async (
            Guid id,
            [FromBody] UpdateTransactionRequest request,
            ICommandHandler<UpdateTransactionCommand> handler,
            CancellationToken ct) =>
        {
            var command = new UpdateTransactionCommand(
                id,
                request.VehicleNumber,
                request.VehicleType,
                request.DriverName,
                request.Grade,
                request.Condition,
                request.Note);

            var result = await handler.Handle(command, ct);
            return result.ToHttpResult();
        })
        .WithName("UpdateGateTransaction")
        .WithSummary("Chỉnh sửa thông tin xe/tài xế/tình trạng container của giao dịch cổng");

        group.MapDelete("/transactions/{id:guid}/cancel", async (
            Guid id,
            HttpContext context,
            ICommandHandler<CancelTransactionCommand> handler,
            CancellationToken ct) =>
        {
            CancelTransactionRequest? request = null;
            if (context.Request.HasJsonContentType())
            {
                try { request = await context.Request.ReadFromJsonAsync<CancelTransactionRequest>(ct); } catch { /* ignore invalid body */ }
            }
            var command = new CancelTransactionCommand(id, request?.Reason);
            var result = await handler.Handle(command, ct);
            return result.ToHttpResult();
        })
        .WithName("CancelGateTransaction")
        .WithSummary("Hủy giao dịch cổng và hoàn tác trạng thái/vị trí");

        group.MapPost("/transactions/{id:guid}/cancel", async (
            Guid id,
            HttpContext context,
            ICommandHandler<CancelTransactionCommand> handler,
            CancellationToken ct) =>
        {
            CancelTransactionRequest? request = null;
            if (context.Request.HasJsonContentType())
            {
                try { request = await context.Request.ReadFromJsonAsync<CancelTransactionRequest>(ct); } catch { /* ignore invalid body */ }
            }
            var command = new CancelTransactionCommand(id, request?.Reason);
            var result = await handler.Handle(command, ct);
            return result.ToHttpResult();
        })
        .WithName("CancelGateTransactionPost")
        .WithSummary("Hủy giao dịch cổng (POST alias)");

        return app;
    }
}

public sealed record UpdateTransactionRequest(
    string? VehicleNumber,
    Domain.Enums.VehicleType? VehicleType,
    string? DriverName,
    Domain.Enums.Classification? Grade,
    Domain.Enums.ContainerCondition? Condition,
    string? Note);

public sealed record CancelTransactionRequest(string? Reason = null);
