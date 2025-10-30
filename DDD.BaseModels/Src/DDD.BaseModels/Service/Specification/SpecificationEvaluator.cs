namespace DDD.BaseModels.Service;

/// <summary>
/// Evaluates a specification and applies it to an IQueryable.
/// </summary>
public static class SpecificationEvaluator
{
    public static IQueryable<T> GetQuery<T>(
        IQueryable<T> inputQuery,
        ISpecification<T> spec)
        where T : BaseEntity
    {
        var query = inputQuery;

        // Filtering
        if (spec.Criteria is not null)
            query = query.Where(spec.Criteria);

        // Includes (lambda)
        query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));

        // Includes (string)
        query = spec.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        // Ordering
        if (spec.OrderBy is not null)
            query = query.OrderBy(spec.OrderBy);
        else if (spec.OrderByDescending is not null)
            query = query.OrderByDescending(spec.OrderByDescending);

        // Paging
        if (spec.IsPagingEnabled)
            query = query.Skip(spec.Skip!.Value).Take(spec.Take!.Value);

        return query;
    }
}