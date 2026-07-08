using System.Runtime.CompilerServices;
using DDD.BaseModels;
using Microsoft.EntityFrameworkCore;

namespace DDD.BaseModels.Service;

internal sealed class FluentCud<TContext, TEntity>(
    IBaseCud<TContext, TEntity> cud,
    IDomainEventDispatcher dispatcher)
    : IFluentCud<TContext, TEntity>
    where TEntity : BaseEntity
    where TContext : DbContext
{
    public IFluentCudOperation<TContext, TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => new FluentCudOperation<TContext, TEntity>(() => cud.InsertAsync(entity, cancellationToken), dispatcher,
            cancellationToken);

    public IFluentCudOperation<TContext, TEntity> AddAsync(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
        => new FluentCudOperation<TContext, TEntity>(() => cud.InsertAsync(entities, cancellationToken), dispatcher,
            cancellationToken);

    public IFluentCudOperation<TContext, TEntity> UpdateAsync(TEntity entity,
        CancellationToken cancellationToken = default)
        => new FluentCudOperation<TContext, TEntity>(() => cud.UpdateAsync(entity, cancellationToken), dispatcher,
            cancellationToken);

    public IFluentCudOperation<TContext, TEntity> UpdateAsync(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
        => new FluentCudOperation<TContext, TEntity>(() => cud.UpdateAsync(entities, cancellationToken), dispatcher,
            cancellationToken);

    public IFluentCudOperation<TContext, TEntity> DeleteAsync(object id, CancellationToken cancellationToken = default)
        => new FluentCudOperation<TContext, TEntity>(() => cud.DeleteAsync(id, cancellationToken), dispatcher,
            cancellationToken);

    public IFluentCudOperation<TContext, TEntity> DeleteAsync(TEntity entity,
        CancellationToken cancellationToken = default)
        => new FluentCudOperation<TContext, TEntity>(() => cud.DeleteAsync(entity, cancellationToken), dispatcher,
            cancellationToken);

    public IFluentCudOperation<TContext, TEntity> DeleteAsync(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
        => new FluentCudOperation<TContext, TEntity>(() => cud.DeleteAsync(entities, cancellationToken), dispatcher,
            cancellationToken);

    public IFluentCudOperation<TContext, TEntity> DeleteAsync(Expression<Func<TEntity, bool>> where,
        CancellationToken cancellationToken = default)
        => new FluentCudOperation<TContext, TEntity>(() => cud.DeleteAsync(where, cancellationToken), dispatcher,
            cancellationToken);

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await cud.DisposeAsync();
    }

    public Task<bool> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => cud.BeginTransactionAsync(cancellationToken);

    public Task<bool> CommitAsync(CancellationToken cancellationToken = default)
        => cud.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default)
        => cud.RollbackAsync(cancellationToken);
}

internal sealed class FluentCudOperation<TContext, TEntity>(
    Func<Task<bool>> operationFactory,
    IDomainEventDispatcher dispatcher,
    CancellationToken cancellationToken = default)
    : IFluentCudOperation<TContext, TEntity>
    where TEntity : BaseEntity
    where TContext : DbContext
{
    private readonly List<Func<bool, Task>> _dispatchers = [];
    private Task<bool>? _task;

    public IFluentCudOperation<TContext, TEntity> DispatchEvent(bool condition, Func<IDomainEvent> whenTrue,
        Func<IDomainEvent> whenFalse)
    {
        _dispatchers.Add(async success =>
        {
            if (!success) return;
            var domainEvent = condition ? whenTrue() : whenFalse();
            if (domainEvent is not null)
                await dispatcher.DispatchAsync([domainEvent], cancellationToken);
        });
        return this;
    }

    public IFluentCudOperation<TContext, TEntity> DispatchEventIf(bool condition, Func<IDomainEvent> @event)
    {
        _dispatchers.Add(async success =>
        {
            if (!success || !condition) return;
            var domainEvent = @event();
            if (domainEvent is not null)
                await dispatcher.DispatchAsync([domainEvent], cancellationToken);
        });
        return this;
    }

    public IFluentCudOperation<TContext, TEntity> DispatchEvent(Func<IDomainEvent> @event)
    {
        _dispatchers.Add(async success =>
        {
            if (!success) return;
            var domainEvent = @event();
            if (domainEvent is not null)
                await dispatcher.DispatchAsync([domainEvent], cancellationToken);
        });
        return this;
    }

    public IFluentCudOperation<TContext, TEntity> DispatchEvent(Func<bool, IDomainEvent> onResult)
    {
        _dispatchers.Add(async success =>
        {
            var domainEvent = onResult(success);
            if (domainEvent is not null)
                await dispatcher.DispatchAsync([domainEvent], cancellationToken);
        });
        return this;
    }

    public Task<bool> ExecuteAsync() => RunAsync();

    public TaskAwaiter<bool> GetAwaiter() => RunAsync().GetAwaiter();

    private Task<bool> RunAsync()
    {
        if (_task is not null)
            return _task;

        _task = ExecuteCoreAsync();
        return _task;
    }

    private async Task<bool> ExecuteCoreAsync()
    {
        var success = await operationFactory();
        foreach (var dispatch in _dispatchers)
            await dispatch(success);
        return success;
    }
}
