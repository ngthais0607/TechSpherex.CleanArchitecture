using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.MasterData.Customers.Commands.ActivateCustomer;

public sealed record ActivateCustomerCommand(Guid Id) : ICommand;
