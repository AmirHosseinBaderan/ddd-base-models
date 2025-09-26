using System.Reflection;

namespace DDD.BaseModels.Service;

public static class DtoProfileRegistry
{
    private static readonly Dictionary<Type, List<object>> _profiles = new();

    public static void RegisterProfile<T>(DtoProfile<T> profile) where T : AggregateRootBase
    {
        var key = typeof(T);
        if (!_profiles.TryGetValue(key, out var list))
        {
            list = new List<object>();
            _profiles[key] = list;
        }

        list.Add(profile);
    }

    public static void RegisterProfilesFromAssembly(Assembly assembly)
    {
        var profileTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.BaseType != null && t.BaseType.IsGenericType &&
                        t.BaseType.GetGenericTypeDefinition() == typeof(DtoProfile<>));

        foreach (var type in profileTypes)
        {
            var instance = Activator.CreateInstance(type)!;
            var targetType = type.BaseType!.GetGenericArguments()[0];

            var registerMethod = typeof(DtoProfileRegistry)
                .GetMethod(nameof(RegisterProfile))!
                .MakeGenericMethod(targetType);

            registerMethod.Invoke(null, [instance]);
        }
    }

    public static IEnumerable<DtoProfile<T>> GetProfilesFor<T>() where T : AggregateRootBase
    {
        if (_profiles.TryGetValue(typeof(T), out var list))
            return list.Cast<DtoProfile<T>>();
        return Enumerable.Empty<DtoProfile<T>>();
    }
}