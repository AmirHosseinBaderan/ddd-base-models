using System.Linq.Expressions;
using DDD.BaseModels;
using Microsoft.EntityFrameworkCore;

namespace DDD.BaseModels.Service;

/// <summary>
/// Fluent CUD service that merges the classic <see cref="IBaseCud{TContext,TEntity}"/>
/// operations with domain-event dispatching.
/// Every mutating call returns an <see cref="IFluentCudOperation{TContext,TEntity}"/>
/// which is awaitable and can be chained with <c>DispatchEvent</c> before being awaited.
/// </summary>
/// <example>
/// await _cud.AddAsync(entity)
///     .DispatchEvent(condition, true: () => new CreatedEvent(), false: () => new SkippedEvent());
/// </example>
public interface IFluentCud<TContext, TEntity> : IAsyncDisposable
    where TEntity : BaseEntity
    where TContext : DbContext
{
    IFluentCudOperation<TContext, TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    IFluentCudOperation<TContext, TEntity> AddAsync(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    IFluentCudOperation<TContext, TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    IFluentCudOperation<TContext, TEntity> UpdateAsync(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    IFluentCudOperation<TContext, TEntity> DeleteAsync(object id, CancellationToken cancellationToken = default);

    IFluentCudOperation<TContext, TEntity> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    IFluentCudOperation<TContext, TEntity> DeleteAsync(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    IFluentCudOperation<TContext, TEntity> DeleteAsync(Expression<Func<TEntity, bool>> where,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// The result of a fluent CUD operation. It is awaitable (returns the success of the
/// underlying CUD operation) and supports chaining one or more <c>DispatchEvent</c> calls
/// that are executed after the CUD operation completes.
/// </summary>
public interface IFluentCudOperation<TContext, TEntity>
    where TEntity : BaseEntity
    where TContext : DbContext
{
    /// <summary>Dispatches <paramref name="whenTrue"/> when <paramref name="condition"/> is
    /// <c>true</c>, otherwise <paramref name="whenFalse"/>. Only dispatched when the CUD succeeded.</summary>
    IFluentCudOperation<TContext, TEntity> DispatchEvent(bool condition, Func<IDomainEvent> whenTrue,
        Func<IDomainEvent> whenFalse);

    /// <summary>Dispatches <paramref name="event"/> only when <paramref name="condition"/> is <c>true</c>
    /// and the CUD succeeded.</summary>
    IFluentCudOperation<TContext, TEntity> DispatchEventIf(bool condition, Func<IDomainEvent> @event);

    /// <summary>Dispatches <paramref name="event"/> when the CUD succeeded.</summary>
    IFluentCudOperation<TContext, TEntity> DispatchEvent(Func<IDomainEvent> @event);

    /// <summary>Dispatches an event chosen from the CUD result (always, success or failure).</summary>
    IFluentCudOperation<TContext, TEntity> DispatchEvent(Func<bool, IDomainEvent> onResult);

    /// <summary>Runs the CUD operation and all queued event dispatches, returning the CUD success.</summary>
    Task<bool> ExecuteAsync();
}
