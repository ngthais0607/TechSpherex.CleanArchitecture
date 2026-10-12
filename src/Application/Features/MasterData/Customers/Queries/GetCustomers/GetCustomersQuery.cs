using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.MasterData.Customers.Dtos;

namespace SnpDepotManagement.Application.Features.MasterData.Customers.Queries.GetCustomers;

public sealed record GetCustomersQuery : IQuery<IReadOnlyList<CustomerDto>>;
