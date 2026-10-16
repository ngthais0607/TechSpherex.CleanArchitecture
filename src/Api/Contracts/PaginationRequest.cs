using System.Reflection;

namespace SnpDepotManagement.Api.Contracts;

public sealed record PaginationRequest(int PageIndex = 1, int PageSize = 20)
{
    public static ValueTask<PaginationRequest?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        int Read(string key, int fallback)
        {
            var raw = context.Request.Query[key];
            if (raw.Count == 0) return fallback;
            if (raw.Count != 1 || !int.TryParse(raw.ToString(), out var number))
                throw new BadHttpRequestException("Invalid pagination.");
            return number;
        }
        var index = Read("pageIndex", 1);
        var size = Read("pageSize", 20);
        if (index < 1 || size < 1 || size > 100 || (long)(index - 1) * size > int.MaxValue)
            throw new BadHttpRequestException("Invalid pagination.");
        return ValueTask.FromResult<PaginationRequest?>(new(index, size));
    }
}
