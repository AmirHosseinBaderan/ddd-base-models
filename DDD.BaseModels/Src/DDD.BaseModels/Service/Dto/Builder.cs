using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace DDD.BaseModels.Service;

// -------------------- DtoBuilder --------------------
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

    // Apply a profile manually
    public DtoBuilder<T> ApplyProfile(DtoProfile<T> profile)
    {
        profile.Configure(this);
        return this;
    }

    // Apply profiles from registry (assembly scanned)
    public DtoBuilder<T> ApplyProfilesFromRegistry()
    {
        var profiles = DtoProfileRegistry.GetProfilesFor<T>();
        foreach (var profile in profiles)
        {
            profile.Configure(this);
        }
        return this;
    }

    // Build DTO as dictionary for a single object
    public Dictionary<string, object?> Build()
    {
        return BuildFrom(aggregate);
    }

    // Build DTO for a list of aggregates
    public List<Dictionary<string, object?>> BuildList(IEnumerable<T> items)
    {
        var list = new List<Dictionary<string, object?>>();
        foreach (var item in items)
        {
            list.Add(BuildFrom(item));
        }
        return list;
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

    // Build runtime type for Swagger (list version)
    public Type BuildTypeList(string typeName = "DynamicDtoList")
    {
        var elementType = BuildType(typeName.Replace("List", ""));
        return typeof(List<>).MakeGenericType(elementType);
    }

    // Helper: build dictionary for any instance of T
    private Dictionary<string, object?> BuildFrom(T item)
    {
        // Evaluate conditional ignores
        foreach (var rule in _conditionalIgnores)
        {
            var result = rule(item);
            if (result != null)
                _ignoredProps.Add(result);
        }

        var props = typeof(T).GetProperties()
            .Where(p => p.CanRead
                        && p.Name != nameof(AggregateRootBase.Id)
                        && !_ignoredProps.Contains(p.Name))
            .ToDictionary(p => p.Name, p => p.GetValue(item));

        // Always include EntityId
        props["Id"] = item.Id.Value;

        // Add extra properties
        foreach (var (key, func) in _extraProps)
            props[key] = func(item);

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

// -------------------- Extensions --------------------
public static class TypeBuilderExtensions
{
    public static void DefinePropertyWithBackingField(this TypeBuilder typeBuilder, string propertyName,
        Type propertyType)
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