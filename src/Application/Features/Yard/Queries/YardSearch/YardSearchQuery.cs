using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.Yard.Queries.YardSearch;

public sealed record YardSearchQuery(string ContainerNumber) : IQuery<YardSearchResult>;
