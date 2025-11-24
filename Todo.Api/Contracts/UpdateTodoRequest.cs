namespace Todo.Api.Contracts;

/// <summary>
/// Request model for updating an existing todo item. At least one field must be provided.
/// </summary>
public record UpdateTodoRequest(
    /// <summary>
    /// Optional new title for the todo item. Must be between 1 and 200 characters if provided.
    /// </summary>
    /// <example>Updated task title</example>
    string? Title,
    /// <summary>
    /// Optional new description for the todo item.
    /// </summary>
    /// <example>Updated description with more details</example>
    string? Description,
    /// <summary>
    /// Optional new due date in YYYY-MM-DD format.
    /// </summary>
    /// <example>2025-06-30</example>
    string? DueDate);