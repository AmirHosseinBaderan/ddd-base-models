using Microsoft.Extensions.DependencyInjection;

namespace DDD.BaseModels;

public class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        foreach (var domainEvent in events)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = scopedProvider.GetServices(handlerType);

            foreach (dynamic? handler in handlers)
                if (handler is not null)
                    await handler.HandleAsync((dynamic)domainEvent, ct);
        }
    }
}