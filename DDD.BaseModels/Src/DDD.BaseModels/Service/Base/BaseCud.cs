using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DDD.BaseModels.Service;

internal class BaseCud<TContext, TEntity>(TContext context, ILogger<IBaseCud<TContext, TEntity>> logger)
    : IBaseCud<TContext, TEntity> where TEntity : BaseEntity where TContext : DbContext
{
    private readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();

    private IRepositoryState _state = new LocalSaveState();

    public async Task<bool> DeleteAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            _dbSet.RemoveRange(entities);
            return await SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in DeleteAsync list");
            return false;
        }
    }

    public async Task<bool> DeleteAsync(TEntity? entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity is not null)
                _dbSet.Remove(entity);
            return await SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in DeleteAsync item");
            return false;
        }
    }

    public async Task<bool> DeleteAsync(Expression<Func<TEntity, bool>> where,
        CancellationToken cancellationToken = default)
        => await DeleteAsync(await _dbSet.Where(where).ToListAsync(cancellationToken), cancellationToken);

    public async Task<bool> DeleteAsync(object id, CancellationToken cancellationToken = default)
        => await DeleteAsync(await _dbSet.FindAsync(id), cancellationToken);

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await context.DisposeAsync();
    }

    public async Task<bool> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbSet.AddAsync(entity, cancellationToken);
            return await SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in InsertAsync item");
            return false;
        }
    }

    public async Task<bool> InsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbSet.AddRangeAsync(entities, cancellationToken);
            return await SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in InsertAsync list");
            return false;
        }
    }

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


    public async Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            context.Entry(entity).State = EntityState.Modified;
            _dbSet.Update(entity);
            return await SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in UpdateAsync item");
            return false;
        }
    }

    public async Task<bool> UpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            _dbSet.UpdateRange(entities);
            return await SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in UpdateAsync list");
            return false;
        }
    }
}