namespace DDD.BaseModels.Service;

/// <summary>
/// Represents a query specification pattern used to encapsulate query logic.
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface ISpecification<T> where T : BaseEntity
{
    /// <summary>
    /// Filter criteria for the entity.
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// Includes related entities (e.g., navigation properties).
    /// </summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Includes string-based navigation properties.
    /// </summary>
    List<string> IncludeStrings { get; }

    /// <summary>
    /// Order by ascending expression.
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Order by descending expression.
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Number of records to skip.
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Number of records to take.
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Whether to enable paging.
    /// </summary>
    bool IsPagingEnabled { get; }
}