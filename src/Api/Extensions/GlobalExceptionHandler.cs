using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;

namespace SnpDepotManagement.Api.Extensions;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        IResult result;
        switch (exception)
        {
            case DbUpdateConcurrencyException:
                result = ResultExtensions.FromError(Error.Conflict("Conflict.ConcurrentUpdate", "The resource changed. Reload and retry."));
                break;
            case DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } }:
                result = ResultExtensions.FromError(Error.Conflict("Conflict.Duplicate", "The resource already exists."));
                break;
            case BusinessRuleException rule:
                result = ResultExtensions.Problem(422, rule.Error.Code, rule.Error.Description);
                break;
            case BadHttpRequestException badRequest:
                result = ResultExtensions.Problem(badRequest.StatusCode, "Request.Invalid", "The request could not be read.");
                break;
            default:
                logger.LogError(exception, "Unhandled request failure. TraceId {TraceId}", context.TraceIdentifier);
                result = ResultExtensions.Problem(500, "Server.Unexpected", "An unexpected error occurred.");
                break;
        }
        await result.ExecuteAsync(context);
        return true;
    }
}
