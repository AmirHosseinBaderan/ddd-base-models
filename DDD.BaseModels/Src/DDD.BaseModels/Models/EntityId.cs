namespace DDD.BaseModels;

public class EntityId : ValueObject<EntityId>
{
    public Guid Value { get; set; }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static EntityId Create(Guid value) => new() { Value = value };

    public static EntityId? Create(Guid? value) =>
        value is null ? null : new() { Value = value.Value };

    public static EntityId CreateUniqueId() => Create(Guid.NewGuid());

    public override string ToString() => Value.ToString();

    // ✅ Equality between EntityId ↔ EntityId
    public static bool operator ==(EntityId? left, EntityId? right)
        => Equals(left, right);

    public static bool operator !=(EntityId? left, EntityId? right)
        => !(left == right);

    // ✅ Equality between EntityId ↔ Guid
    public static bool operator ==(EntityId? left, Guid right)
        => left?.Value == right;

    public static bool operator !=(EntityId? left, Guid right)
        => !(left == right);

    public static bool operator ==(Guid left, EntityId? right)
        => right?.Value == left;

    public static bool operator !=(Guid left, EntityId? right)
        => !(left == right);

    public override bool Equals(object? obj)
        => obj is EntityId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    // ✅ Explicit cast from EntityId → Guid
    public static explicit operator Guid(EntityId id) => id.Value;

    // ✅ Implicit cast from Guid → EntityId
    public static implicit operator EntityId(Guid value) => Create(value);
}