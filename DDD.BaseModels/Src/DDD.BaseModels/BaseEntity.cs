namespace DDD.BaseModels;

public class BaseEntity
{
    public EntityId Id { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public DateTime? UpdatedOn { get; set; }
}
