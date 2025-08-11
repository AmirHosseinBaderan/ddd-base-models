namespace DDD.BaseModels;

public interface IAggregateRoot
{
    ICollection<IDomainEvent> Events { get; }

    void ClearEvents();
}
