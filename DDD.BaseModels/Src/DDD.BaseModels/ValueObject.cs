namespace DDD.BaseModels;

public abstract class ValueObject<T> where T : ValueObject<T>
{
    public abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object? obj)
        => obj is not null &&
           obj is T valueObject &&
           obj.GetType() == GetType() &&
           GetEqualityComponents().SequenceEqual(valueObject.GetEqualityComponents());

    public static bool operator ==(ValueObject<T>? left, ValueObject<T>? right)
    {
        if (ReferenceEquals(left, right)) return true; // Both are null or same reference
        if (left is null || right is null) return false; // One is null, the other is not
        return left.Equals(right); // Perform actual equality check
    }

    public static bool operator !=(ValueObject<T>? left, ValueObject<T>? right)
            => !(left == right); // Use the updated == operator

    public override int GetHashCode()
        => GetEqualityComponents()
                .Select(x => x?.GetHashCode() ?? 0)
                .Aggregate((x, y) => x ^ y);
}
