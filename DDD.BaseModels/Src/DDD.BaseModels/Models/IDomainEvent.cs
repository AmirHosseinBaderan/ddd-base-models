namespace DDD.BaseModels;

public interface IDomainEvent
{
    public DateTime CreatedOn { get; set; }
}
