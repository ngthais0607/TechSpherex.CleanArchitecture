using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Operations;

namespace SnpDepotManagement.Application.Features.Gate.Queries.GetGateTransactions;

public sealed record GetGateTransactionsQuery(
    string? Search = null,
    GateTransactionType? Type = null,
    GateTransactionStatus? Status = null,
    int PageIndex = 1,
    int PageSize = 20
) : IQuery<PagedResult<GateTransactionDto>>;
