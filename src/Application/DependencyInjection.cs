using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Register RuleEngine
        services.AddSingleton<SnpDepotManagement.Application.Abstractions.Rules.IRuleEngine, SnpDepotManagement.Application.Common.Rules.RuleEngine>();

        // Register default TimeProvider
        services.AddSingleton(TimeProvider.System);

        // Register all FluentValidation validators via assembly scan
        RegisterValidators(services, assembly);

        // Scan and register CQRS Handlers
        RegisterHandlers(services, assembly);

        return services;
    }

    private static void RegisterValidators(IServiceCollection services, Assembly assembly)
    {
        var validatorType = typeof(IValidator<>);
        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition);

        foreach (var type in types)
        {
            var interfaces = type.GetInterfaces();
            foreach (var iface in interfaces)
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == validatorType)
                {
                    services.AddTransient(iface, type);
                }
            }
        }
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerInterfaces = new[]
        {
            typeof(ICommandHandler<>),
            typeof(ICommandHandler<,>),
            typeof(IQueryHandler<,>)
        };

        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition);

        foreach (var type in types)
        {
            var interfaces = type.GetInterfaces();

            foreach (var iface in interfaces)
            {
                if (!iface.IsGenericType)
                {
                    continue;
                }

                var genericDef = iface.GetGenericTypeDefinition();
                if (handlerInterfaces.Contains(genericDef))
                {
                    services.AddTransient(iface, type);
                }
            }
        }
    }
}
