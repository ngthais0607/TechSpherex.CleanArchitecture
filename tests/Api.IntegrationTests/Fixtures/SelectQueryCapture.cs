using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SnpDepotManagement.Api.IntegrationTests.Fixtures;

internal sealed class SelectQueryCapture : DbCommandInterceptor
{
    public List<string> Commands { get; } = [];

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        if (command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            Commands.Add(command.CommandText);
        return ValueTask.FromResult(result);
    }
}
