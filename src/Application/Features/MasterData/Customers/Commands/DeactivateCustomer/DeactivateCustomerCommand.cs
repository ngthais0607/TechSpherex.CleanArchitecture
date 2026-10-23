using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.MasterData.Customers.Commands.DeactivateCustomer;

public sealed record DeactivateCustomerCommand(Guid Id) : ICommand;
