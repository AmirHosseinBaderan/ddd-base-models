namespace DDD.BaseModels.Service;

public class DtoBuilder<T>(T aggregate)
    where T : AggregateRootBase
{
    private readonly HashSet<string> _ignoredProps = new();
    private readonly List<(string Key, Func<T, object?> Value)> _extraProps = new();

    // Ignore property
    public DtoBuilder<T> Ignore(Expression<Func<T, object>> selector)
    {
        var name = GetPropertyName(selector);
        _ignoredProps.Add(name);
        return this;
    }

    // Add computed/custom property
    public DtoBuilder<T> Add(string key, Func<T, object?> valueFactory)
    {
        _extraProps.Add((key, valueFactory));
        return this;
    }

    // Build DTO as dictionary
    public Dictionary<string, object?> Build()
    {
        var props = typeof(T).GetProperties()
            .Where(p => p.CanRead
                        && p.Name != nameof(AggregateRootBase.Id)
                        && !_ignoredProps.Contains(p.Name))
            .ToDictionary(p => p.Name, p => p.GetValue(aggregate));

        // Always include EntityId
        props["Id"] = aggregate.Id.Value;

        // Add extra properties
        foreach (var (key, func) in _extraProps)
            props[key] = func(aggregate);
        return props;
    }

    private static string GetPropertyName(Expression<Func<T, object>> expression)
    {
        if (expression.Body is MemberExpression member)
            return member.Member.Name;

        if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression member2)
            return member2.Member.Name;

        throw new ArgumentException("Invalid expression");
    }
}