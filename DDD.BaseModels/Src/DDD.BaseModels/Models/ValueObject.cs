using System.Reflection;

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

public static class ValueObjectExtensions
{
    public static T? ToValueObject<T, TSource>(this TSource? source) where T : ValueObject<T>, new()
    {
        if (source is null)
            return null;

        T target = new();
        BindProperties(source, target);

        return target;
    }

    public static T? ToValueObject<T, TSource>(
        this TSource? source,
        Action<T>? configure)
        where T : ValueObject<T>, new()
    {
        if (source == null)
            return null;

        T target = new();
        BindProperties(source, target);

        configure?.Invoke(target);

        return target;
    }

    private static void BindProperties<TSource, TTarget>(
        TSource source,
        TTarget target)
    {
        var sourceProps = typeof(TSource)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var targetProps = typeof(TTarget)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var targetProp in targetProps)
        {
            if (!targetProp.CanWrite)
                continue;

            var sourceProp = sourceProps
                .FirstOrDefault(p =>
                    p.Name == targetProp.Name &&
                    targetProp.PropertyType.IsAssignableFrom(p.PropertyType));

            if (sourceProp == null)
                continue;

            var value = sourceProp.GetValue(source);
            targetProp.SetValue(target, value);
        }
    }
}