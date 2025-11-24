using Microsoft.AspNetCore.Mvc;

namespace Todo.Api.Contracts;

/// <summary>
/// Query parameters for listing todos with filtering, sorting, and pagination.
/// </summary>
public record ListTodosQuery(
    /// <summary>
    /// Filter by completion status. If null, returns both completed and incomplete items.
    /// </summary>
    /// <example>true</example>
    bool? IsCompleted,
    /// <summary>
    /// Filter to show only overdue items (due date in the past and not completed).
    /// </summary>
    /// <example>true</example>
    bool? Overdue,
    /// <summary>
    /// Filter items with due date on or before this date (YYYY-MM-DD format).
    /// </summary>
    /// <example>2025-12-31</example>
    DateOnly? DueBefore,
    /// <summary>
    /// Filter items with due date on or after this date (YYYY-MM-DD format).
    /// </summary>
    /// <example>2025-01-01</example>
    DateOnly? DueAfter,
    /// <summary>
    /// Field to sort by. If null, items are returned in their natural order.
    /// </summary>
    /// <example>DueDate</example>
    TodoSortField? SortBy,
    /// <summary>
    /// Sort order. Defaults to Ascending if not specified.
    /// </summary>
    /// <example>Asc</example>
    SortOrder? Order,
    /// <summary>
    /// Page number (1-based). Defaults to 1 if not specified.
    /// </summary>
    /// <example>1</example>
    int? Page,
    /// <summary>
    /// Number of items per page. Defaults to 10, maximum is 100.
    /// </summary>
    /// <example>10</example>
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

