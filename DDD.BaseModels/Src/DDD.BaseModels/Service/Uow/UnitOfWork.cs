using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DDD.BaseModels.Service;

public class UnitOfWork<TContext>(TContext context, ILogger<IUnitOfWork<TContext>> logger) : IUnitOfWork<TContext> where TContext : DbContext
{
    public async Task<bool> CommitAsync()
    {
        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in SaveAsync");
            return false;
        }
    }

    public void Rollback()
    {
        foreach (var entry in context.ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Modified:
                    entry.State = EntityState.Unchanged;
                    break;
                case EntityState.Added:
                    entry.State = EntityState.Detached;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Unchanged;
                    break;
            }
        }
    }

    public void Dispose()
    {
        context.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task<bool> ExecuteAsync(Func<Task> action, Action<DbContext> onFailed)
    {
        try
        {
            await action();
            await CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError("Exception in execute UOF async {@Exception}",ex);
            onFailed(context);
            return false;
        }
    }

    public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action, Func<DbContext, TResult> onFailed)
    {
        try
        {
            var result = await action();
            return await CommitAsync()
                ? result
                : onFailed(context);
        }
        catch (Exception ex)
        {
            logger.LogError("Exception in execute UOF async {@Exception}",ex);
            return onFailed(context);
        }
    }
}