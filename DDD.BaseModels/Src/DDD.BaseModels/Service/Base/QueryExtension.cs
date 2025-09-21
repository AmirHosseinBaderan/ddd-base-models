using Microsoft.EntityFrameworkCore;

namespace DDD.BaseModels.Service;

public static class QueryExtension
{
    public static IQueryable<T> ToPagination<T>(this IQueryable<T> query, int page, int pageSize)
        => query.Skip(page * pageSize)
            .Take(pageSize);

    public static async Task<PaginationResult<T>> ToPaginationAsync<T>(this IQueryable<T> query, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var count = await query.CountAsync(cancellationToken);
        var result = page == 0 && pageSize == 0
            ? await query.ToListAsync(cancellationToken)
            : await query.Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        return result.ToPagination(count, pageSize);
    }
}