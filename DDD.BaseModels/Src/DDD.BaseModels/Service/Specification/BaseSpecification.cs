namespace DDD.BaseModels.Service;

/// <summary>
/// Base implementation for creating specifications with filtering, sorting, and includes.
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public abstract class BaseSpecification<T> : ISpecification<T> where T : BaseEntity
{
    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public int? Skip { get; private set; }
    public int? Take { get; private set; }
    public bool IsPagingEnabled { get; private set; }

    protected BaseSpecification()
    {
    }

    protected BaseSpecification(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    /// <summary>
    /// Adds an include expression for eager loading.
    /// </summary>
    protected void AddInclude(Expression<Func<T, object>> includeExpression)
        => Includes.Add(includeExpression);

    /// <summary>
    /// Adds an include by string path (useful for nested properties).
    /// </summary>
    protected void AddInclude(string includeString)
        => IncludeStrings.Add(includeString);

    /// <summary>
    /// Adds an ascending order expression.
    /// </summary>
    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
        => OrderBy = orderByExpression;

    /// <summary>
    /// Adds a descending order expression.
    /// </summary>
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
        => OrderByDescending = orderByDescendingExpression;

    /// <summary>
    /// Applies pagination to the query.
    /// </summary>
    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }
}