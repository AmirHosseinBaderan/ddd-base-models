using Microsoft.Extensions.Logging;

namespace DDD.BaseModels.Service;

internal class BaseCud<TContext, TEntity>(TContext context, ILogger<IBaseCud<TContext, TEntity>> logger)
    : IBaseCud<TContext, TEntity> where TEntity : BaseEntity where TContext : DbContext
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

    public async Task<TResult> InsertAsync<TResult>(Expression<Func<TEntity, bool>> existExpression,
        Func<TResult> exist, Func<TEntity> create, Func<TEntity?, TResult> final)
    {
        TEntity? entity = await _dbSet.FirstOrDefaultAsync(existExpression);
        if (entity is not null)
            return exist();

        TEntity newEntity = create();
        return await InsertAsync(newEntity)
            ? final(newEntity)
            : final(null);
    }

    public async Task<TResult> InsertAsync<TResult>(Expression<Func<TEntity, bool>> existExpression,
        Func<TResult> exist, Func<TEntity> create, Func<TEntity?, Task<TResult>> final)
    {
        TEntity? entity = await _dbSet.FirstOrDefaultAsync(existExpression);
        if (entity is not null)
            return exist();

        TEntity newEntity = create();
        return await InsertAsync(newEntity)
            ? await final(newEntity)
            : await final(null);
    }

    public async Task<TResult> UpdateAsync<TResult>(object id, Func<TResult> notFound,
        Func<TEntity?, Task<TResult>> final,
        Func<TEntity, TEntity> update)
    {
        TEntity? entity = await _dbSet.FindAsync(id);
        if (entity is null)
            return notFound();

        TEntity updateEntity = update(entity);
        return await final(await UpdateAsync(updateEntity)
            ? updateEntity
            : null);
    }

    public async Task<TResult> UpdateAsync<TResult>(object id, Func<TResult> notFound, Func<TEntity?, TResult> final,
        Func<TEntity, TEntity> update)
    {
        TEntity? entity = await _dbSet.FindAsync(id);
        if (entity is null)
            return notFound();

        TEntity updateEntity = update(entity);
        return final(await UpdateAsync(updateEntity)
            ? updateEntity
            : null);
    }
}