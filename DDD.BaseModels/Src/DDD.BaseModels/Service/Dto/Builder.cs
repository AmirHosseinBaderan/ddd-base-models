using System.Reflection;
using System.Reflection.Emit;

namespace DDD.BaseModels.Service;

public class DtoBuilder<T>(T aggregate)
    where T : AggregateRootBase
{
    private readonly HashSet<string> _ignoredProps = new();
    private readonly List<(string Key, Func<T, object?> Value)> _extraProps = new();
    private readonly List<Func<T, string?>> _conditionalIgnores = new();

    // Ignore property directly
    public DtoBuilder<T> Ignore(Expression<Func<T, object>> selector)
    {
        var name = GetPropertyName(selector);
        _ignoredProps.Add(name);
        return this;
    }

    // Ignore property if condition matches
    public DtoBuilder<T> IgnoreWhere(Expression<Func<T, object>> selector, Func<T, bool> condition)
    {
        var name = GetPropertyName(selector);
        _conditionalIgnores.Add(x => condition(x) ? name : null);
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
        // Evaluate conditional ignores
        foreach (var rule in _conditionalIgnores)
        {
            var result = rule(aggregate);
            if (result != null)
                _ignoredProps.Add(result);
        }

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

    // Build runtime type for Swagger
    public Type BuildType(string typeName = "DynamicDto")
    {
        var assemblyName = new AssemblyName("DynamicDtos");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

        var typeBuilder = moduleBuilder.DefineType(
            typeName,
            TypeAttributes.Public | TypeAttributes.Class);

        // Always include Id
        typeBuilder.DefinePropertyWithBackingField("Id", typeof(Guid));

        // Aggregate props
        var props = typeof(T).GetProperties()
            .Where(p => p.CanRead
                        && p.Name != nameof(AggregateRootBase.Id)
                        && !_ignoredProps.Contains(p.Name));

        foreach (var p in props)
        {
            typeBuilder.DefinePropertyWithBackingField(p.Name, p.PropertyType);
        }

        // Extra props (always object type at runtime)
        foreach (var (key, _) in _extraProps)
        {
            typeBuilder.DefinePropertyWithBackingField(key, typeof(object));
        }

        return typeBuilder.CreateType()!;
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

// helper extension for generating properties
public static class TypeBuilderExtensions
{
    public static void DefinePropertyWithBackingField(this TypeBuilder typeBuilder, string propertyName, Type propertyType)
    {
        var fieldBuilder = typeBuilder.DefineField($"_{propertyName}", propertyType, FieldAttributes.Private);

        var propBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);

        // Getter
        var getter = typeBuilder.DefineMethod($"get_{propertyName}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            propertyType, Type.EmptyTypes);

        var getterIL = getter.GetILGenerator();
        getterIL.Emit(OpCodes.Ldarg_0);
        getterIL.Emit(OpCodes.Ldfld, fieldBuilder);
        getterIL.Emit(OpCodes.Ret);

        // Setter
        var setter = typeBuilder.DefineMethod($"set_{propertyName}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            null, new[] { propertyType });

        var setterIL = setter.GetILGenerator();
        setterIL.Emit(OpCodes.Ldarg_0);
        setterIL.Emit(OpCodes.Ldarg_1);
        setterIL.Emit(OpCodes.Stfld, fieldBuilder);
        setterIL.Emit(OpCodes.Ret);

        propBuilder.SetGetMethod(getter);
        propBuilder.SetSetMethod(setter);
    }
}
