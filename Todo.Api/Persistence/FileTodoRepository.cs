using Todo.Api.Domain;

namespace Todo.Api.Persistence;

public class FileTodoRepository : ITodoRepository
{
    private readonly DataStore _store;

    public FileTodoRepository(DataStore store) => _store = store;

    public Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken ct = default)
        => _store.ReadAllAsync(ct);

    public Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _store.ReadByIdAsync(id, ct);

    public Task<TodoItem> AddAsync(TodoItem item, CancellationToken ct = default)
        => _store.AddAsync(item, ct);

    public Task<TodoItem?> UpdateAsync(TodoItem item, CancellationToken ct = default)
        => _store.UpdateAsync(item, ct);

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        => _store.RemoveAsync(id, ct);
}
