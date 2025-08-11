namespace DDD.BaseModels;

public abstract class AggregateRootBase : BaseEntity, IAggregateRoot
{
    public ICollection<IDomainEvent> Events => [.. _events];

    private readonly List<IDomainEvent> _events;

    protected AggregateRootBase()
    {
        _events = [];
    }

    public void ClearEvents() => _events.Clear();

    protected void AddEvent<TDomainEvent>(TDomainEvent @event)
        where TDomainEvent : IDomainEvent => _events.Add(@event);
}
