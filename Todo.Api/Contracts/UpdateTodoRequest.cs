namespace Todo.Api.Contracts;

/// <summary>
/// Request model for updating an existing todo item. At least one field must be provided.
/// </summary>
/// <param name="Title">Optional new title for the todo item. Must be between 1 and 200 characters if provided.</param>
/// <param name="Description">Optional new description for the todo item.</param>
/// <param name="DueDate">Optional new due date in YYYY-MM-DD format.</param>
public record UpdateTodoRequest(
    string? Title,
    string? Description,
    string? DueDate);