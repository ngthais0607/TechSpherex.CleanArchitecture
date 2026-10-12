using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Containers.Commands.MarkContainerDamaged;

public sealed record MarkContainerDamagedCommand(
    Guid ContainerId,
    ContainerCondition Condition = ContainerCondition.Damaged) : ICommand;
