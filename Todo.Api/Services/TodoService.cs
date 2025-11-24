using Todo.Api.Contracts;
using Todo.Api.Domain;
using Todo.Api.Persistence;
using Todo.Api.Validation;

namespace Todo.Api.Services;

public class TodoService
{
    private readonly ITodoRepository _repo;

    public TodoService(ITodoRepository repo) => _repo = repo;

    public async Task<TodoItem> CreateAsync(CreateTodoRequest req, CancellationToken ct = default)
    {
        Validators.ValidateTitle(req.Title);
        var due = Validators.ParseDate(req.DueDate);

        var item = new TodoItem
        {
            Title = req.Title.Trim(),
            Description = req.Description?.Trim(),
            DueDate = due,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        return await _repo.AddAsync(item, ct);
    }

    public async Task<PaginatedResponse<TodoItem>> ListAsync(
        bool? isCompleted,
        bool? overdue,
        DateOnly? dueBefore,
        DateOnly? dueAfter,
        TodoSortField? sortBy,
        SortOrder sortOrder,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        Validators.EnsureDateRange(dueAfter, dueBefore);
        Validators.ValidatePagination(page, pageSize);

        var items = await _repo.GetAllAsync(ct);
        var query = items.AsEnumerable();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (isCompleted is not null)
        {
            query = query.Where(x => x.IsCompleted == isCompleted.Value);
        }

        if (overdue == true)
        {
            query = query.Where(x =>
                x.DueDate is not null &&
                x.DueDate < today &&
                !x.IsCompleted);
        }

        if (dueBefore is not null)
        {
            query = query.Where(x =>
                x.DueDate is not null &&
                x.DueDate <= dueBefore.Value);
        }

        if (dueAfter is not null)
        {
            query = query.Where(x =>
                x.DueDate is not null &&
                x.DueDate >= dueAfter.Value);
        }

        if (sortBy is not null)
        {
            var descending = sortOrder == SortOrder.Desc;
            query = (sortBy, descending) switch
            {
                (TodoSortField.DueDate, true) => query.OrderByDescending(x => x.DueDate ?? DateOnly.MinValue),
                (TodoSortField.DueDate, false) => query.OrderBy(x => x.DueDate ?? DateOnly.MaxValue),
                (TodoSortField.CreatedAt, true) => query.OrderByDescending(x => x.CreatedAt),
                (TodoSortField.CreatedAt, false) => query.OrderBy(x => x.CreatedAt),
                (TodoSortField.Title, true) => query.OrderByDescending(x => x.Title),
                (TodoSortField.Title, false) => query.OrderBy(x => x.Title),
                _ => query
            };
        }

        var totalCount = query.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var pagedItems = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PaginatedResponse<TodoItem>(pagedItems, page, pageSize, totalCount, totalPages);
    }

    public Task<TodoItem?> GetAsync(Guid id, CancellationToken ct = default) => _repo.GetByIdAsync(id, ct);

    public async Task<TodoItem?> UpdateAsync(Guid id, UpdateTodoRequest req, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct);
        if (existing is null) return null;

        var hasChanges = false;

        if (req.Title is not null)
        {
            Validators.ValidateTitle(req.Title);
            existing.Title = req.Title.Trim();
            hasChanges = true;
        }

        if (req.Description is not null)
        {
            existing.Description = req.Description?.Trim();
            hasChanges = true;
        }

        if (req.DueDate is not null)
        {
            existing.DueDate = Validators.ParseDate(req.DueDate);
            hasChanges = true;
        }

        if (!hasChanges)
        {
            throw new ValidationException("request", "Provide at least one field to update.");
        }

        return await _repo.UpdateAsync(existing, ct);
    }

    public async Task<TodoItem?> SetCompletedAsync(Guid id, bool completed, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct);
        if (existing is null) return null;

        existing.IsCompleted = completed;
        return await _repo.UpdateAsync(existing, ct);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) => _repo.DeleteAsync(id, ct);
}
