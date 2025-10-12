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
}

public interface IBaseCud<TContext> : IAsyncDisposable where TContext : DbContext
{
    Task<bool> InsertAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : BaseEntity;

    Task<bool> InsertAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        where TEntity : BaseEntity;
    
    Task<bool> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : BaseEntity;

    Task<bool> UpdateAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        where TEntity : BaseEntity;

    Task<bool> DeleteAsync<TEntity>(object id, CancellationToken cancellationToken = default)
        where TEntity : BaseEntity;

    Task<bool> DeleteAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : BaseEntity;

    Task<bool> DeleteAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        where TEntity : BaseEntity;

    Task<bool> DeleteAsync<TEntity>(Expression<Func<TEntity, bool>> where,
        CancellationToken cancellationToken = default)
        where TEntity : BaseEntity;
}