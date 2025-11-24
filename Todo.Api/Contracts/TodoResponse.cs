using Todo.Api.Domain;

namespace Todo.Api.Contracts;

/// <summary>
/// Response model representing a todo item.
/// </summary>
public record TodoResponse(
    /// <summary>
    /// The unique identifier of the todo item.
    /// </summary>
    /// <example>550e8400-e29b-41d4-a716-446655440000</example>
    Guid Id,
    /// <summary>
    /// The title of the todo item.
    /// </summary>
    /// <example>Complete project documentation</example>
    string Title,
    /// <summary>
    /// Optional description of the todo item.
    /// </summary>
    /// <example>Write API documentation and update README</example>
    string? Description,
    /// <summary>
    /// Optional due date in YYYY-MM-DD format.
    /// </summary>
    /// <example>2025-12-31</example>
    string? DueDate,
    /// <summary>
    /// Indicates whether the todo item has been completed.
    /// </summary>
    /// <example>false</example>
    bool IsCompleted,
    /// <summary>
    /// The UTC timestamp when the todo item was created.
    /// </summary>
    /// <example>2025-01-15T10:30:00Z</example>
    DateTime CreatedAt)
{
    /// <summary>
    /// Creates a TodoResponse from a TodoItem domain model.
    /// </summary>
    public static TodoResponse From(TodoItem item) =>
        new(
            item.Id,
            item.Title,
            item.Description,
            item.DueDate?.ToString("yyyy-MM-dd"),
            item.IsCompleted,
            item.CreatedAt);
}
