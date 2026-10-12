using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Containers.Dtos;

namespace SnpDepotManagement.Application.Features.Containers.Queries.GetContainerByNumber;

public sealed record GetContainerByNumberQuery(string ContainerNumber) : IQuery<ContainerDto>;
