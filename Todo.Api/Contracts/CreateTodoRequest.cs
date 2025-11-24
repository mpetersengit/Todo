namespace Todo.Api.Contracts;

/// <summary>
/// Request model for creating a new todo item.
/// </summary>
public record CreateTodoRequest(
    /// <summary>
    /// The title of the todo item. Must be between 1 and 200 characters.
    /// </summary>
    /// <example>Complete project documentation</example>
    string Title,
    /// <summary>
    /// Optional description providing more details about the task.
    /// </summary>
    /// <example>Write API documentation and update README with examples</example>
    string? Description,
    /// <summary>
    /// Optional due date in YYYY-MM-DD format.
    /// </summary>
    /// <example>2025-12-31</example>
    string? DueDate);