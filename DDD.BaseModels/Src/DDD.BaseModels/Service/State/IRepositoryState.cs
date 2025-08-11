using Microsoft.EntityFrameworkCore;

namespace DDD.BaseModels.Service;

public interface IRepositoryState
{
    Task<bool> SaveAsync(DbContext context);
}

public class LocalSaveState : IRepositoryState
{
    public async Task<bool> SaveAsync(DbContext context)
        => await context.SaveChangesAsync() > 0;
}

public class UnitOfWorkState : IRepositoryState
{
    public Task<bool> SaveAsync(DbContext context)
            => Task.FromResult(true);

}