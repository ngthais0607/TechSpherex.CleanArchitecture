using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.Yard.Queries.GetVirtualBlockContainers;

public sealed record GetVirtualBlockContainersQuery(string Code) : IQuery<IReadOnlyList<VirtualBlockContainerDto>>;
