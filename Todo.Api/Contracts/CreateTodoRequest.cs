namespace Todo.Api.Contracts;

/// <summary>
/// Request model for creating a new todo item.
/// </summary>
/// <param name="Title">The title of the todo item. Must be between 1 and 200 characters.</param>
/// <param name="Description">Optional description providing more details about the task.</param>
/// <param name="DueDate">Optional due date in YYYY-MM-DD format.</param>
public record CreateTodoRequest(
    string Title,
    string? Description,
    string? DueDate);