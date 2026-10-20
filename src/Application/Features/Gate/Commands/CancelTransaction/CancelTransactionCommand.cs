using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.Gate.Commands.CancelTransaction;

public sealed record CancelTransactionCommand(
    Guid TransactionId,
    string? Reason = null
) : ICommand;
