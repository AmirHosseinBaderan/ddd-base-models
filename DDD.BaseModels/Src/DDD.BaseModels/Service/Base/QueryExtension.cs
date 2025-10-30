using Microsoft.EntityFrameworkCore;

namespace DDD.BaseModels.Service;

/// <summary>
    /// Provides useful LINQ query extension methods for conditional filtering,
    /// pagination, and dynamic expression building.
    /// </summary>
    public static class QueryExtension
    {
        /// <summary>
        /// Applies pagination to the query using Skip and Take.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="query">The source query</param>
        /// <param name="page">Zero-based page index</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>Paginated IQueryable</returns>
        public static IQueryable<T> ToPagination<T>(this IQueryable<T> query, int page, int pageSize)
            => query.Skip(page * pageSize)
                    .Take(pageSize);

        /// <summary>
        /// Executes the query asynchronously and returns a paginated result including total count.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="query">The source query</param>
        /// <param name="page">Zero-based page index</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A PaginationResult containing items and total count</returns>
        public static async Task<PaginationResult<T>> ToPaginationAsync<T>(
            this IQueryable<T> query,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var count = await query.CountAsync(cancellationToken);

            var result = (page == 0 && pageSize == 0)
                ? await query.ToListAsync(cancellationToken)
                : await query.Skip(page * pageSize)
                             .Take(pageSize)
                             .ToListAsync(cancellationToken);

            return result.ToPagination(count, pageSize);
        }

        /// <summary>
        /// Conditionally applies a Where clause if the given condition is true.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="query">The source query</param>
        /// <param name="condition">If true, applies the predicate; otherwise, ignored</param>
        /// <param name="predicate">The filter expression</param>
        /// <returns>Modified query if condition is true, otherwise original query</returns>
        public static IQueryable<T> WhereIf<T>(
            this IQueryable<T> query,
            bool condition,
            Expression<Func<T, bool>> predicate)
            => condition ? query.Where(predicate) : query;

        /// <summary>
        /// Conditionally applies an OrderBy or OrderByDescending clause.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TKey">Key type</typeparam>
        /// <param name="query">The source query</param>
        /// <param name="condition">If true, applies ordering; otherwise, ignored</param>
        /// <param name="keySelector">The property to order by</param>
        /// <param name="descending">Whether to order descending</param>
        /// <returns>Ordered query if condition is true, otherwise original query</returns>
        public static IQueryable<T> OrderByIf<T, TKey>(
            this IQueryable<T> query,
            bool condition,
            Expression<Func<T, TKey>> keySelector,
            bool descending = false)
        {
            if (!condition)
                return query;

            return descending
                ? query.OrderByDescending(keySelector)
                : query.OrderBy(keySelector);
        }

        /// <summary>
        /// Filters entities where the specified string property contains the given keyword.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="query">The source query</param>
        /// <param name="propertySelector">Expression selecting a string property</param>
        /// <param name="keyword">Keyword to search for (case-sensitive)</param>
        /// <returns>Filtered query if keyword is provided, otherwise original query</returns>
        public static IQueryable<T> WhereContains<T>(
            this IQueryable<T> query,
            Expression<Func<T, string>> propertySelector,
            string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return query;

            var param = propertySelector.Parameters.First();
            var body = Expression.Call(
                propertySelector.Body,
                typeof(string).GetMethod("Contains", new[] { typeof(string) })!,
                Expression.Constant(keyword)
            );

            var lambda = Expression.Lambda<Func<T, bool>>(body, param);
            return query.Where(lambda);
        }

        /// <summary>
        /// Filters entities where the specified property value is in the provided collection.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TKey">Property type</typeparam>
        /// <param name="query">The source query</param>
        /// <param name="propertySelector">Expression selecting the property</param>
        /// <param name="values">Collection of values to match</param>
        /// <returns>Filtered query if values provided, otherwise original query</returns>
        public static IQueryable<T> WhereIn<T, TKey>(
            this IQueryable<T> query,
            Expression<Func<T, TKey>> propertySelector,
            IEnumerable<TKey>? values)
        {
            if (values == null || !values.Any())
                return query;

            var parameter = propertySelector.Parameters.Single();
            var body = Expression.Call(
                Expression.Constant(values),
                typeof(IEnumerable<TKey>).GetMethod("Contains", new[] { typeof(TKey) })!,
                propertySelector.Body
            );

            var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
            return query.Where(lambda);
        }
    }