using System.Reflection;
using DddOrderAggregate.Application.Common;
using DddOrderAggregate.Application.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace DddOrderAggregate.Application;

/// <summary>
/// Composition helper for registering Application layer use case handlers and event dispatching.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register domain event dispatcher
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // Register all command and query handlers ending in CommandHandler or QueryHandler
        var handlers = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        (t.Name.EndsWith("CommandHandler", StringComparison.Ordinal) ||
                         t.Name.EndsWith("QueryHandler", StringComparison.Ordinal)));

        foreach (var handler in handlers)
        {
            services.AddScoped(handler);
        }

        // Register all IDomainEventHandler<TEvent> implementations
        var domainEventHandlerType = typeof(IDomainEventHandler<>);
        var eventHandlers = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces(), (t, i) => new { Implementation = t, Interface = i })
            .Where(x => x.Interface.IsGenericType && x.Interface.GetGenericTypeDefinition() == domainEventHandlerType);

        foreach (var handler in eventHandlers)
        {
            services.AddScoped(handler.Interface, handler.Implementation);
        }

        return services;
    }
}
