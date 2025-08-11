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
}