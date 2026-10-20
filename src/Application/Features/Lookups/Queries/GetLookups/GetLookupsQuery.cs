using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Lookups.Dtos;

namespace SnpDepotManagement.Application.Features.Lookups.Queries.GetLookups;

public sealed record GetLookupsQuery : IQuery<LookupsDto>;
