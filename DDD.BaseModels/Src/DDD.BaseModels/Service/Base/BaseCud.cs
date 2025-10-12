using Microsoft.Extensions.Logging;

namespace DDD.BaseModels.Service;

internal class BaseCud<TContext, TEntity>(TContext context, ILogger<IBaseCud<TContext, TEntity>> logger)
    : IBaseCud<TContext>, IBaseCud<TContext, TEntity> where TEntity : BaseEntity where TContext : DbContext
{
    private readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();

    private IRepositoryState _state = new LocalSaveState();

    public Task<bool> DeleteAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        => DeleteAsync(_dbSet, entities, cancellationToken);

    public Task<bool> DeleteAsync(TEntity? entity, CancellationToken cancellationToken = default)
        => DeleteAsync(_dbSet, entity, cancellationToken);

    public Task<bool> DeleteAsync(Expression<Func<TEntity, bool>> where,
        CancellationToken cancellationToken = default)
        => DeleteAsync(_dbSet, where, cancellationToken);

    public Task<bool> DeleteAsync(object id, CancellationToken cancellationToken = default)
        => DeleteAsync(_dbSet, id, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await context.DisposeAsync();
    }

    public Task<bool> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
        => InsertAsync(_dbSet, entity, cancellationToken);

    public Task<bool> InsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        => InsertAsync(_dbSet, entities, cancellationToken);

    public async Task<bool> SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return _state is not LocalSaveState || await _state.SaveAsync(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in SaveAsync");
            return false;
        }
    }

    public void SetState(IRepositoryState state)
        => _state = state;

    public Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        => UpdateAsync(_dbSet, entity, cancellationToken);

    public Task<bool> UpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        => UpdateAsync(_dbSet, entities, cancellationToken);

    public Task<bool> InsertAsync<TEntity1>(TEntity1 entity, CancellationToken cancellationToken = default)
        where TEntity1 : BaseEntity
    {
        var set = context.Set<TEntity1>();
        return InsertAsync(set, entity, cancellationToken);
    }

    public Task<bool> InsertAsync<TEntity1>(IEnumerable<TEntity1> entities,
        CancellationToken cancellationToken = default) where TEntity1 : BaseEntity
    {
        var set = context.Set<TEntity1>();
        return InsertAsync(set, entities, cancellationToken);
    }

    public Task<bool> UpdateAsync<TEntity1>(TEntity1 entity, CancellationToken cancellationToken = default)
        where TEntity1 : BaseEntity
    {
        var set = context.Set<TEntity1>();
        return UpdateAsync(set, entity, cancellationToken);
    }

    public Task<bool> UpdateAsync<TEntity1>(IEnumerable<TEntity1> entities,
        CancellationToken cancellationToken = default) where TEntity1 : BaseEntity
    {
        var set = context.Set<TEntity1>();
        return UpdateAsync(set, entities, cancellationToken);
    }

    public Task<bool> DeleteAsync<TEntity1>(object id, CancellationToken cancellationToken = default)
        where TEntity1 : BaseEntity
    {
        var set = context.Set<TEntity1>();
        return DeleteAsync(set, id, cancellationToken);
    }

    public Task<bool> DeleteAsync<TEntity1>(TEntity1 entity, CancellationToken cancellationToken = default)
        where TEntity1 : BaseEntity
    {
        var set = context.Set<TEntity1>();
        return DeleteAsync(set, entity, cancellationToken);
    }

    public Task<bool> DeleteAsync<TEntity1>(IEnumerable<TEntity1> entities,
        CancellationToken cancellationToken = default) where TEntity1 : BaseEntity
    {
        var set = context.Set<TEntity1>();
        return DeleteAsync(set, entities, cancellationToken);
    }

    public Task<bool> DeleteAsync<TEntity1>(Expression<Func<TEntity1, bool>> where,
        CancellationToken cancellationToken = default) where TEntity1 : BaseEntity
    {
        var set = context.Set<TEntity1>();
        return DeleteAsync(set, where, cancellationToken);
    }

    async Task<bool> InsertAsync<TEntity1>(DbSet<TEntity1> set, TEntity1 entity,
        CancellationToken cancellationToken = default)
        where TEntity1 : BaseEntity
    {
        try
        {
            await set.AddAsync(entity, cancellationToken);
            return await SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in InsertAsync item");
            return false;
        }
    }

    async Task<bool> InsertAsync<TEntity1>(DbSet<TEntity1> set, IEnumerable<TEntity1> entities,
        CancellationToken cancellationToken = default) where TEntity1 : BaseEntity
    {
        try
        {
            await set.AddRangeAsync(entities, cancellationToken);
            return await SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in InsertAsync list");
            return false;
        }
    }

    async Task<bool> UpdateAsync<TEntity1>(DbSet<TEntity1> set, TEntity1 entity,
        CancellationToken cancellationToken = default) where TEntity1 : BaseEntity
    {
        try
        {
            context.Entry(entity).State = EntityState.Modified;
            set.Update(entity);
            return await SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in UpdateAsync item");
            return false;
        }
    }

    async Task<bool> UpdateAsync<TEntity1>(DbSet<TEntity1> set, IEnumerable<TEntity1> entities,
        CancellationToken cancellationToken = default) where TEntity1 : BaseEntity
    {
        try
        {
            set.UpdateRange(entities);
            return await SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in UpdateAsync list");
            return false;
        }
    }

    async Task<bool> DeleteAsync<TEntity1>(DbSet<TEntity1> set, object id,
        CancellationToken cancellationToken = default) where TEntity1 : BaseEntity
        => await DeleteAsync(set, await set.FindAsync(id), cancellationToken);

    async Task<bool> DeleteAsync<TEntity1>(DbSet<TEntity1> set, TEntity1? entity,
        CancellationToken cancellationToken = default) where TEntity1 : BaseEntity
    {
        try
        {
            if (entity is not null)
                set.Remove(entity);
            return await SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in DeleteAsync item");
            return false;
        }
    }

    async Task<bool> DeleteAsync<TEntity1>(DbSet<TEntity1> set, IEnumerable<TEntity1> entities,
        CancellationToken cancellationToken = default) where TEntity1 : BaseEntity
    {
        try
        {
            set.RemoveRange(entities);
            return await SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in DeleteAsync list");
            return false;
        }
    }

    async Task<bool> DeleteAsync<TEntity1>(DbSet<TEntity1> set, Expression<Func<TEntity1, bool>> where,
        CancellationToken cancellationToken = default) where TEntity1 : BaseEntity
        => await DeleteAsync(set, await set.Where(where).ToListAsync(cancellationToken), cancellationToken);
}