using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.MasterData.Customers.Commands.UpdateCustomer;

public sealed record UpdateCustomerCommand(Guid Id, string Name) : ICommand;
