namespace DDD.BaseModels.Service;

public abstract class DtoProfile<T> where T : AggregateRootBase
{
    public abstract void Configure(DtoBuilder<T> builder);
}
