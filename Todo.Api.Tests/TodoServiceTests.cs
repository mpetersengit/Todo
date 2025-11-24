using Todo.Api.Contracts;
using Todo.Api.Domain;
using Todo.Api.Services;
using Todo.Api.Validation;
using Todo.Api.Persistence;
using Xunit;

namespace Todo.Api.Tests;

public class TodoServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidPayload_ReturnsItem()
    {
        var repo = new InMemoryTodoRepository();
        var service = new TodoService(repo);
        var request = new CreateTodoRequest("  ship feature  ", "  finish writing docs ", "2025-01-01");

        var result = await service.CreateAsync(request);

        Assert.Equal("ship feature", result.Title);
        Assert.Equal("finish writing docs", result.Description);
        Assert.Equal(new DateOnly(2025, 1, 1), result.DueDate);
        Assert.False(result.IsCompleted);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateAsync_WithBlankTitle_ThrowsValidationException()
    {
        var repo = new InMemoryTodoRepository();
        var service = new TodoService(repo);
        var request = new CreateTodoRequest("   ", null, null);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task UpdateAsync_WithoutChanges_ThrowsValidationException()
    {
        var repo = new InMemoryTodoRepository();
        var service = new TodoService(repo);

        var existing = await service.CreateAsync(new CreateTodoRequest("initial", null, null));

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateAsync(existing.Id, new UpdateTodoRequest(null, null, null)));
    }

    [Fact]
    public async Task ListAsync_WithFilters_ReturnsExpectedItems()
    {
        var repo = new InMemoryTodoRepository();
        var service = new TodoService(repo);

        await service.CreateAsync(new CreateTodoRequest("done", null, "2024-01-01"));
        var remaining = await service.CreateAsync(new CreateTodoRequest("remaining", null, "2030-01-01"));
        await service.SetCompletedAsync(remaining.Id, true);

        var overdue = await service.ListAsync(
            isCompleted: false,
            overdue: true,
            dueBefore: null,
            dueAfter: null,
            sortBy: TodoSortField.DueDate,
            sortOrder: SortOrder.Asc,
            page: 1,
            pageSize: 10);

        Assert.Single(overdue.Items);
        Assert.Equal("done", overdue.Items[0].Title);
        Assert.Equal(1, overdue.TotalCount);
        Assert.Equal(1, overdue.TotalPages);
    }

    private sealed class InMemoryTodoRepository : ITodoRepository
    {
        private readonly List<TodoItem> _items = new();

        public Task<TodoItem> AddAsync(TodoItem item, CancellationToken ct = default)
        {
            _items.Add(item);
            return Task.FromResult(item);
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var removed = _items.RemoveAll(x => x.Id == id) > 0;
            return Task.FromResult(removed);
        }

        public Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TodoItem>>(_items.ToList());

        public Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<TodoItem?> UpdateAsync(TodoItem item, CancellationToken ct = default)
        {
            var idx = _items.FindIndex(x => x.Id == item.Id);
            if (idx < 0) return Task.FromResult<TodoItem?>(null);
            _items[idx] = item;
            return Task.FromResult<TodoItem?>(item);
        }
    }
}

