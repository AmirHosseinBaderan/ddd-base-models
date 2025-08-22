namespace DDD.BaseModels;

public interface IDomainEventHandler<T> where T : IDomainEvent
{
    Task HandleAsync(T domainEvent, CancellationToken ct = default);
}

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
}
