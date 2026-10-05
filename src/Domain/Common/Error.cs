using System;
using System.Collections.Generic;
using System.Text;

namespace SnpDepotManagement.Domain.Common;

public record Error(string Code, string Description)
{
    public static readonly Error None = new Error(string.Empty, string.Empty);

    public static Error Validation(string code , string Description) => new (code, Description);
    public static Error NotFound(string code, string Description) => new(code, Description);
    public static Error Conflict(string code, string Description) => new(code, Description);

}
