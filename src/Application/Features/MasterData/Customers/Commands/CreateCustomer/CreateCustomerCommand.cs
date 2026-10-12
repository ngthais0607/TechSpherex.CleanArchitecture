using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.MasterData.Customers.Commands.CreateCustomer;

public sealed record CreateCustomerCommand(string TaxCode, string Name) : ICommand<Guid>;
