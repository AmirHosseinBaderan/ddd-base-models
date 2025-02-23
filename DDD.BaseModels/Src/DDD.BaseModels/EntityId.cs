namespace DDD.BaseModels;

public class EntityId : ValueObject<EntityId>
{
    public Guid Value { get; set; }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static EntityId Create(Guid value) => new()
    {
        Value = value,
    };

    public static EntityId? Create(Guid? value) =>
        value is null
        ? null
            : new()
            {
                Value = (Guid)value,
            };

    public static EntityId CreateUniqueId() => Create(Guid.NewGuid());

    public override string ToString()
    {
        return Value.ToString();
    }
}
