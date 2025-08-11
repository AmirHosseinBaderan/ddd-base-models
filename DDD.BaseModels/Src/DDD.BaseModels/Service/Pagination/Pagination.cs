namespace DDD.BaseModels.Service;

public record PaginationResult<TResult>(int PageCount, int ItemsCount, IEnumerable<TResult> Items);

public static class PaginationExtension
{
    public static PaginationResult<ToResult> FromPagination<FromResult, ToResult>(this PaginationResult<FromResult> res, IEnumerable<ToResult> items)
        => new(res.PageCount, res.ItemsCount, items);

    public static PaginationResult<ToResult> FromPagination<FromResult, ToResult>(this PaginationResult<FromResult> res, List<ToResult> items)
        => new(res.PageCount, res.ItemsCount, items);

    public static PaginationResult<TResult> ToPagination<TResult>(this IEnumerable<TResult> items, int itemsCount, int pageSize)
        => new(itemsCount.PageCount(pageSize), itemsCount, items);

    public static int PageCount(this int itemsCount, int pageSize)
        => pageSize <= 0
            ? 1 // Defaulting to 1 to prevent division by zero
            : (int)Math.Ceiling((double)itemsCount / pageSize);
}
