using Microsoft.EntityFrameworkCore;

namespace DDD.BaseModels.Service;

public interface IUnitOfWork<TContext> : IDisposable where TContext : DbContext
{
    Task<bool> CommitAsync();

    void Rollback();

    Task<bool> ExecuteAsync(Func<Task> action, Action<DbContext> onFailed);

    Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action, Func<DbContext, TResult> onFailed);
}