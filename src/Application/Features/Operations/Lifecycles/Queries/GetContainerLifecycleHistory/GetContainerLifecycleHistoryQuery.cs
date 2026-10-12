using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Operations.Lifecycles.Dtos;

namespace SnpDepotManagement.Application.Features.Operations.Lifecycles.Queries.GetContainerLifecycleHistory;

public sealed record GetContainerLifecycleHistoryQuery(Guid ContainerId) : IQuery<List<ContainerLifecycleDto>>;
