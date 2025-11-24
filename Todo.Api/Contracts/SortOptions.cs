namespace Todo.Api.Contracts;

/// <summary>
/// Fields available for sorting todo items.
/// </summary>
public enum TodoSortField
{
    /// <summary>
    /// Sort by title alphabetically.
    /// </summary>
    Title,
    /// <summary>
    /// Sort by due date.
    /// </summary>
    DueDate,
    /// <summary>
    /// Sort by creation date.
    /// </summary>
    CreatedAt
}

/// <summary>
/// Sort order direction.
/// </summary>
public enum SortOrder
{
    /// <summary>
    /// Ascending order (A-Z, oldest first, etc.).
    /// </summary>
    Asc,
    /// <summary>
    /// Descending order (Z-A, newest first, etc.).
    /// </summary>
    Desc
}

