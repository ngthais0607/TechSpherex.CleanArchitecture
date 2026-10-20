using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Containers.Commands.RegisterContainer;

public sealed record RegisterContainerCommand(
    string ContainerNumber,
    string IsoCode,
    ContainerSize Size,
    ContainerType Type,
    decimal TareWeight,
    decimal MaxGrossWeight,
    DateTime? DateOfManufacture,
    Classification Grade = Classification.A,
    ContainerCondition Condition = ContainerCondition.Normal
) : ICommand<Guid>;
