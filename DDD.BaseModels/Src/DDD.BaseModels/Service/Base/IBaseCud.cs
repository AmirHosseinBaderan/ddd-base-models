using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace DDD.BaseModels.Service;

public interface IBaseCud<TContext,TEntity> : IAsyncDisposable where TEntity : BaseEntity where TContext : DbContext
{
    Task<bool> InsertAsync(TEntity entity);

    Task<bool> InsertAsync(IEnumerable<TEntity> entities);

    Task<bool> UpdateAsync(TEntity entity);

    Task<bool> UpdateAsync(IEnumerable<TEntity> entities);

    Task<bool> DeleteAsync(object id);

    Task<bool> DeleteAsync(TEntity entity);

    Task<bool> DeleteAsync(IEnumerable<TEntity> entities);

    Task<bool> DeleteAsync(Expression<Func<TEntity, bool>> where);

    Task<bool> SaveAsync();

    void SetState(IRepositoryState state);
}