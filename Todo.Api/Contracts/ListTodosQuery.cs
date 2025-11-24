using Microsoft.AspNetCore.Mvc;

namespace Todo.Api.Contracts;

/// <summary>
/// Query parameters for listing todos with filtering, sorting, and pagination.
/// </summary>
/// <param name="IsCompleted">Filter by completion status. If null, returns both completed and incomplete items.</param>
/// <param name="Overdue">Filter to show only overdue items (due date in the past and not completed).</param>
/// <param name="DueBefore">Filter items with due date on or before this date (YYYY-MM-DD format).</param>
/// <param name="DueAfter">Filter items with due date on or after this date (YYYY-MM-DD format).</param>
/// <param name="SortBy">Field to sort by. If null, items are returned in their natural order.</param>
/// <param name="Order">Sort order. Defaults to Ascending if not specified.</param>
/// <param name="Page">Page number (1-based). Defaults to 1 if not specified.</param>
/// <param name="PageSize">Number of items per page. Defaults to 10, maximum is 100.</param>
public record ListTodosQuery(
    bool? IsCompleted,
    bool? Overdue,
    DateOnly? DueBefore,
    DateOnly? DueAfter,
    TodoSortField? SortBy,
    SortOrder? Order,
    int? Page,
    int? PageSize)
{
    /// <summary>
    /// Gets the sort direction, defaulting to Ascending if not specified.
    /// </summary>
    public SortOrder SortDirection => Order ?? SortOrder.Asc;

    /// <summary>
    /// Gets the page number, defaulting to 1 if not specified.
    /// </summary>
    public int PageNumber => Page ?? 1;

    /// <summary>
    /// Gets the page size, defaulting to 10 if not specified, with a maximum of 100.
    /// </summary>
    public int PageSizeValue => Math.Min(Math.Max(PageSize ?? 10, 1), 100);
}

