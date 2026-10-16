using FluentValidation;

namespace SnpDepotManagement.Api.Extensions;

public sealed class ValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        void Add(string key, string message)
        {
            if (!errors.TryGetValue(key, out var messages)) errors[key] = messages = [];
            messages.Add(message);
        }
        foreach (var argument in context.Arguments.Where(a => a is not null))
        {
            var type = argument!.GetType();
            if (type.IsEnum)
            {
                if (!Enum.IsDefined(type, argument)) Add(type.Name, "The selected value is invalid.");
                continue;
            }
            if (type.Namespace?.StartsWith("SnpDepotManagement.", StringComparison.Ordinal) != true) continue;
            foreach (var validator in context.HttpContext.RequestServices.GetServices(typeof(IValidator<>).MakeGenericType(type)).OfType<IValidator>())
            {
                var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
                foreach (var error in result.Errors) Add(error.PropertyName, error.ErrorMessage);
            }
            foreach (var property in type.GetProperties().Where(p => p.CanRead && p.GetIndexParameters().Length == 0))
            {
                var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                var value = property.GetValue(argument);
                if (propertyType.IsEnum && value is not null && !Enum.IsDefined(propertyType, value))
                    Add(property.Name, "The selected value is invalid.");
            }
        }
        if (errors.Count > 0)
            return ResultExtensions.Problem(400, "Validation.Failed", "Request validation failed.",
                errors.ToDictionary(e => e.Key, e => e.Value.Distinct().ToArray()));
        return await next(context);
    }
}
