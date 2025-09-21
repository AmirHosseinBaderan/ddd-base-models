using System.Reflection;
using DDD.BaseModels.Service;
using Microsoft.Extensions.DependencyInjection;

namespace DDD.BaseModels;

public static class DddConfig
{
    public static IServiceCollection AddDDDBaseServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(IBaseCud<,>), typeof(BaseCud<,>));
        services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));

        return services;
    }

    public static void ConfigDDDEvents(this IServiceCollection services,Assembly  assembly)
    {
        // Base
        services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();

        // Config events
        services.AddDomainEventHandlers(assembly);
    }

    static IServiceCollection AddDomainEventHandlers(this IServiceCollection services, Assembly assembly)
    {
        // Find all closed generic implementations of IDomainEventHandler<T>
        var handlers = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces(), (t, i) => new { Handler = t, Interface = i })
            .Where(x =>
                x.Interface.IsGenericType &&
                x.Interface.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>))
            .ToList();

        foreach (var h in handlers)
            services.AddScoped(h.Interface, h.Handler);
        return services;
    }
}