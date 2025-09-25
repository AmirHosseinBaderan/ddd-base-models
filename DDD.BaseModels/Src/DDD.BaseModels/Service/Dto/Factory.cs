namespace DDD.BaseModels.Service;

public static class DtoFactory
{
    public static DtoBuilder<T> ToDtoBuilder<T>(this T aggregate) where T : AggregateRootBase
        => new DtoBuilder<T>(aggregate);
}