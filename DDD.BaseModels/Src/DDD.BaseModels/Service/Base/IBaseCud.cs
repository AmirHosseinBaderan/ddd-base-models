namespace DDD.BaseModels.Service;

public interface IBaseCud<TContext, TEntity> : IAsyncDisposable where TEntity : BaseEntity where TContext : DbContext
{
    Task<bool> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<bool> InsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(object id, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default);

    Task<bool> SaveAsync(CancellationToken cancellationToken = default);

    void SetState(IRepositoryState state);
    
    Task<TResult> UpdateAsync<TResult>(object id, Func<TResult> notFound, Func<TEntity?, Task<TResult>> final,
        Func<TEntity, TEntity> update);

    Task<TResult> UpdateAsync<TResult>(object id, Func<TResult> notFound, Func<TEntity?, TResult> final,
        Func<TEntity, TEntity> update);
    
    Task<TResult> InsertAsync<TResult>(Expression<Func<TEntity, bool>> existExpression,
        Func<TResult> exist, Func<TEntity> create, Func<TEntity?, TResult> final);

    Task<TResult> InsertAsync<TResult>(Expression<Func<TEntity, bool>> existExpression,
        Func<TResult> exist, Func<TEntity> create, Func<TEntity?, Task<TResult>> final);
}