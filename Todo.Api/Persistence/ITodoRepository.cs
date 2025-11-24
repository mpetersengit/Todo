using Todo.Api.Domain;

namespace Todo.Api.Persistence;

public interface ITodoRepository
{
    Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken ct = default);
    Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TodoItem> AddAsync(TodoItem item, CancellationToken ct = default);
    Task<TodoItem?> UpdateAsync(TodoItem item, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}