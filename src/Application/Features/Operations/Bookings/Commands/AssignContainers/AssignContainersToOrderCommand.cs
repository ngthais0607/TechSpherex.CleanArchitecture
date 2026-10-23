using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.Operations.Bookings.Commands.AssignContainers;

public sealed record AssignContainersToOrderCommand(
    string OrderNumber,
    IReadOnlyList<string> ContainerNumbers) : ICommand<AssignContainersResult>;

public sealed record AssignContainersResult(
    Guid OrderId,
    int RequestedCount,
    int AssignedCount,
    IReadOnlyList<Guid> BookingIds,
    IReadOnlyList<string> FailedContainerNumbers);
