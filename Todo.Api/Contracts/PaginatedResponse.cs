namespace Todo.Api.Contracts;

/// <summary>
/// Represents a paginated response containing items and pagination metadata.
/// </summary>
/// <typeparam name="T">The type of items in the response.</typeparam>
public record PaginatedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages)
{
    /// <summary>
    /// Indicates whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Indicates whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;
}

