using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.Yard.Commands.CreateYardBlock;

public sealed record CreateYardBlockCommand(
    string Code,
    string Name,
    bool IsVirtual,
    int Bays = 0,
    int Rows = 0,
    int Tiers = 0) : ICommand<Guid>;
