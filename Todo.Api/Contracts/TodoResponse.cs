using Todo.Api.Domain;

namespace Todo.Api.Contracts;

/// <summary>
/// Response model representing a todo item.
/// </summary>
/// <param name="Id">The unique identifier of the todo item.</param>
/// <param name="Title">The title of the todo item.</param>
/// <param name="Description">Optional description of the todo item.</param>
/// <param name="DueDate">Optional due date in YYYY-MM-DD format.</param>
/// <param name="IsCompleted">Indicates whether the todo item has been completed.</param>
/// <param name="CreatedAt">The UTC timestamp when the todo item was created.</param>
public record TodoResponse(
    Guid Id,
    string Title,
    string? Description,
    string? DueDate,
    bool IsCompleted,
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
