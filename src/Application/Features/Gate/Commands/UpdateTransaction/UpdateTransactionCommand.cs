using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Gate.Commands.UpdateTransaction;

public sealed record UpdateTransactionCommand(
    Guid TransactionId,
    string? VehicleNumber = null,
    VehicleType? VehicleType = null,
    string? DriverName = null,
    Classification? Grade = null,
    ContainerCondition? Condition = null,
    string? Note = null
) : ICommand;
