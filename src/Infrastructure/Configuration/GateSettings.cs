using Microsoft.Extensions.Configuration;
using SnpDepotManagement.Application.Abstractions.Configuration;

namespace SnpDepotManagement.Infrastructure.Configuration;

public sealed class GateSettings : IGateSettings
{
    private readonly IConfiguration _configuration;

    public GateSettings(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool AllowDirectShip => _configuration.GetValue<bool?>("Gate:AllowDirectShip") ?? true;
}
